using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

var server = args[0];
var port = int.Parse(args[1]);
var cache = new Dictionary<(string Name, string Type), CacheEntry>();

while (Console.ReadLine() is { } line)
{
    if (string.IsNullOrWhiteSpace(line))
        continue;
    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    var name = parts[0];
    var type = parts[1];
    Console.WriteLine($"query {name} {type}");
    var key = (name.ToLowerInvariant(), type);
    if (cache.TryGetValue(key, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
    {
        Console.WriteLine("status NOERROR");
        foreach (var answer in cached.Answers)
            Console.WriteLine($"answer {answer.Type} {answer.Value} {answer.Ttl}");
        Console.WriteLine("end");
        continue;
    }
    cache.Remove(key);
    var id = (ushort)Random.Shared.Next(0, 65536);
    var query = BuildQuery(id, name, type);
    using var client = new UdpClient();
    await client.SendAsync(query, query.Length, server, port);
    byte[] response = [];
    var stopwatch = Stopwatch.StartNew();
    var received = false;
    while (stopwatch.Elapsed < TimeSpan.FromSeconds(5))
    {
        var remaining = TimeSpan.FromSeconds(5) - stopwatch.Elapsed;
        var receiveTask = client.ReceiveAsync();
        var timeoutTask = Task.Delay(remaining);
        var completed = await Task.WhenAny(receiveTask, timeoutTask);
        if (completed == timeoutTask)
            break;
        var result = await receiveTask;
        if (result.Buffer.Length < 12)
            continue;
        var responseId = ReadUInt16(result.Buffer, 0);
        if (responseId != id)
            continue;
        response = result.Buffer;
        received = true;
        break;
    }
    if (!received)
    {
        Console.WriteLine("status TIMEOUT");
        Console.WriteLine("end");
        Environment.ExitCode = 1;
        return;
    }
    var flags = ReadUInt16(response, 2);
    var answerCount = ReadUInt16(response, 6);
    var status = GetStatus(flags);
    Console.WriteLine($"status {status}");
    var offset = 12;
    var question = ReadName(response, offset);
    offset = question.NextOffset + 4;
    var answers = new List<DnsAnswer>();
    uint minTtl = uint.MaxValue;
    var hasZeroTtl = false;
    for (var i = 0; i < answerCount; i++)
    {
        var answerName = ReadName(response, offset);
        offset = answerName.NextOffset;
        var answerType = ReadUInt16(response, offset);
        var answerClass = ReadUInt16(response, offset + 2);
        var ttl = ReadUInt32(response, offset + 4);
        var rdLength = ReadUInt16(response, offset + 8);
        offset += 10;
        var rdataOffset = offset;
        offset += rdLength;
        if (answerClass != 1)
            continue;
        if (ttl < minTtl)
            minTtl = ttl;
        if (ttl == 0)
            hasZeroTtl = true;
        var answer = ParseAnswer(response, answerType, ttl, rdataOffset, rdLength);
        if (answer != null)
        {
            answers.Add(answer);
            Console.WriteLine($"answer {answer.Type} {answer.Value} {answer.Ttl}");
        }
    }
    Console.WriteLine("end");
    if (status == "NOERROR" && answers.Count > 0 && !hasZeroTtl)
    {
        cache[key] = new CacheEntry(answers, DateTime.UtcNow.AddSeconds(minTtl));
    }
}

static byte[] BuildQuery(ushort id, string name, string type)
{
    using var stream = new MemoryStream();
    WriteUInt16(stream, id);
    WriteUInt16(stream, 0x0100);
    WriteUInt16(stream, 1);
    WriteUInt16(stream, 0);
    WriteUInt16(stream, 0);
    WriteUInt16(stream, 0);
    stream.Write(EncodeName(name));
    WriteUInt16(stream, GetTypeCode(type));
    WriteUInt16(stream, 1);
    return stream.ToArray();
}

static byte[] EncodeName(string name)
{
    using var stream = new MemoryStream();
    foreach (var label in name.TrimEnd('.').Split('.'))
    {
        var bytes = Encoding.ASCII.GetBytes(label);
        stream.WriteByte((byte)bytes.Length);
        stream.Write(bytes);
    }
    stream.WriteByte(0);
    return stream.ToArray();
}

static ushort GetTypeCode(string type)
{
    return type switch
    {
        "A" => 1,
        "NS" => 2,
        "CNAME" => 5,
        "MX" => 15,
        "TXT" => 16,
        "AAAA" => 28,
        _ => throw new ArgumentException($"Unsupported type: {type}")
    };
}

static void WriteUInt16(Stream stream, ushort value)
{
    stream.WriteByte((byte)(value >> 8));
    stream.WriteByte((byte)value);
}

static ushort ReadUInt16(byte[] data, int offset)
{
    return (ushort)((data[offset] << 8) | data[offset + 1]);
}

static uint ReadUInt32(byte[] data, int offset)
{
    return ((uint)data[offset] << 24) | ((uint)data[offset + 1] << 16) | ((uint)data[offset + 2] << 8) | data[offset + 3];
}

static (string Name, int NextOffset) ReadName(byte[] data, int offset)
{
    var labels = new List<string>();
    var current = offset;
    var nextOffset = offset;
    var jumped = false;
    var jumps = 0;
    while (true)
    {
        if (jumps++ > 128)
            throw new InvalidDataException("DNS name compression loop");
        if (current >= data.Length)
            throw new InvalidDataException("Invalid DNS name");
        var length = data[current];
        if (length == 0)
        {
            if (!jumped)
                nextOffset = current + 1;
            break;
        }
        if ((length & 0xC0) == 0xC0)
        {
            if (current + 1 >= data.Length)
                throw new InvalidDataException("Invalid DNS pointer");
            if (!jumped)
                nextOffset = current + 2;
            var pointer = ((length & 0x3F) << 8) | data[current + 1];
            current = pointer;
            jumped = true;
            continue;
        }
        if ((length & 0xC0) != 0)
            throw new InvalidDataException("Invalid DNS label");
        current++;
        if (current + length > data.Length)
            throw new InvalidDataException("Invalid DNS name");
        labels.Add(Encoding.ASCII.GetString(data, current, length));
        current += length;
    }
    return (string.Join(".", labels) + ".", nextOffset);
}

static DnsAnswer? ParseAnswer(byte[] data, ushort type, uint ttl, int offset, int length)
{
    return type switch
    {
        1 when length == 4 => new DnsAnswer("A", new IPAddress(data.Skip(offset).Take(4).ToArray()).ToString(), ttl),
        28 when length == 16 => new DnsAnswer("AAAA", new IPAddress(data.Skip(offset).Take(16).ToArray()).ToString(), ttl),
        5 => new DnsAnswer("CNAME", ReadName(data, offset).Name, ttl),
        2 => new DnsAnswer("NS", ReadName(data, offset).Name, ttl),
        15 when length >= 3 => ParseMx(data, offset, ttl),
        16 => ParseTxt(data, offset, length, ttl),
        _ => null
    };
}

static DnsAnswer ParseMx(byte[] data, int offset, uint ttl)
{
    var priority = ReadUInt16(data, offset);
    var exchange = ReadName(data, offset + 2).Name;
    return new DnsAnswer("MX", $"{priority} {exchange}", ttl);
}

static DnsAnswer ParseTxt(byte[] data, int offset, int length, uint ttl)
{
    var end = offset + length;
    var current = offset;
    var builder = new StringBuilder();
    while (current < end)
    {
        var stringLength = data[current++];
        if (current + stringLength > end)
            throw new InvalidDataException("Invalid TXT record");
        builder.Append(Encoding.UTF8.GetString(data, current, stringLength));
        current += stringLength;
    }
    return new DnsAnswer("TXT", builder.ToString(), ttl);
}

static string GetStatus(ushort flags)
{
    return (flags & 0x000F) switch
    {
        0 => "NOERROR",
        1 => "FORMERR",
        2 => "SERVFAIL",
        3 => "NXDOMAIN",
        5 => "REFUSED",
        var code => $"RCODE{code}"
    };
}

record DnsAnswer(string Type, string Value, uint Ttl);
record CacheEntry(List<DnsAnswer> Answers, DateTime ExpiresAt);

