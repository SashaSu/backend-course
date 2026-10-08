var input = Console.In.ReadToEnd();
var hex = new string(input.Where(Uri.IsHexDigit).ToArray());
var data = Convert.FromHexString(hex);

Console.WriteLine($"eth.dst {Mac(data, 0)}");
Console.WriteLine($"eth.src {Mac(data, 6)}");

ushort ethertype = ReadUInt16(data, 12);
Console.WriteLine($"eth.ethertype 0x{ethertype:x4}");

if (ethertype == 0x0800)
{
    int ip = 14;

    int version = data[ip] >> 4;
    int ihlBytes = (data[ip] & 0x0f) * 4;
    ushort totalLength = ReadUInt16(data, ip + 2);
    ushort id = ReadUInt16(data, ip + 4);
    ushort flagsAndOffset = ReadUInt16(data, ip + 6);
    int ttl = data[ip + 8];
    int protocol = data[ip + 9];

    Console.WriteLine($"ip.version {version}");
    Console.WriteLine($"ip.ihl_bytes {ihlBytes}");
    Console.WriteLine($"ip.total_length {totalLength}");
    Console.WriteLine($"ip.id 0x{id:x4}");
    Console.WriteLine($"ip.ttl {ttl}");
    Console.WriteLine($"ip.protocol {protocol}");

    int flags = flagsAndOffset >> 13;
    int fragmentOffset = (flagsAndOffset & 0x1fff) * 8;

    string flagsText = flags switch
    {
        0b010 => "DF",
        0b001 => "MF",
        0b011 => "DF,MF",
        _ => "none"
    };

    Console.WriteLine($"ip.flags {flagsText}");
    Console.WriteLine($"ip.frag_offset {fragmentOffset}");

    Console.WriteLine($"ip.src {IpAddress(data, ip + 12)}");
    Console.WriteLine($"ip.dst {IpAddress(data, ip + 16)}");
    Console.WriteLine($"ip.checksum_valid {ChecksumValid(data, ip, ihlBytes).ToString().ToLower()}");

    int transport = ip + ihlBytes;

    if (protocol == 6)
    {
        ushort srcPort = ReadUInt16(data, transport);
        ushort dstPort = ReadUInt16(data, transport + 2);
        uint seq = ReadUInt32(data, transport + 4);
        uint ack = ReadUInt32(data, transport + 8);

        int dataOffsetBytes = (data[transport + 12] >> 4) * 4;
        ushort tcpFlags = ReadUInt16(data, transport + 12);
        ushort window = ReadUInt16(data, transport + 14);

        Console.WriteLine($"tcp.src_port {srcPort}");
        Console.WriteLine($"tcp.dst_port {dstPort}");
        Console.WriteLine($"tcp.seq {seq}");
        Console.WriteLine($"tcp.ack {ack}");
        Console.WriteLine($"tcp.data_offset_bytes {dataOffsetBytes}");
        Console.WriteLine($"tcp.flags {TcpFlags(tcpFlags)}");
        Console.WriteLine($"tcp.window {window}");

        int payloadStart = transport + dataOffsetBytes;
        int payloadLength = ip + totalLength - payloadStart;

        Console.WriteLine($"payload.length {payloadLength}");
    }
    else if (protocol == 17)
    {
        ushort srcPort = ReadUInt16(data, transport);
        ushort dstPort = ReadUInt16(data, transport + 2);
        ushort udpLength = ReadUInt16(data, transport + 4);

        Console.WriteLine($"udp.src_port {srcPort}");
        Console.WriteLine($"udp.dst_port {dstPort}");
        Console.WriteLine($"udp.length {udpLength}");

        int payloadLength = udpLength - 8;

        Console.WriteLine($"payload.length {payloadLength}");
    }
    else
    {
        int payloadStart = transport;
        int payloadLength = ip + totalLength - payloadStart;

        Console.WriteLine($"payload.length {payloadLength}");
    }
}

static ushort ReadUInt16(byte[] data, int offset)
{
    return (ushort)((data[offset] << 8) | data[offset + 1]);
}

static uint ReadUInt32(byte[] data, int offset)
{
    return ((uint)data[offset] << 24)
         | ((uint)data[offset + 1] << 16)
         | ((uint)data[offset + 2] << 8)
         | data[offset + 3];
}

static string Mac(byte[] data, int offset)
{
    return string.Join(":", data.Skip(offset).Take(6).Select(x => x.ToString("x2")));
}

static string IpAddress(byte[] data, int offset)
{
    return string.Join(".", data.Skip(offset).Take(4));
}

static bool ChecksumValid(byte[] data, int offset, int length)
{
    uint sum = 0;

    for (int i = 0; i < length; i += 2)
    {
        ushort word = ReadUInt16(data, offset + i);

        if (i == 10)
            word = 0;

        sum += word;
        sum = (sum & 0xffff) + (sum >> 16);
    }

    sum = (sum & 0xffff) + (sum >> 16);

    ushort checksum = ReadUInt16(data, offset + 10);
    uint result = sum + checksum;
    result = (result & 0xffff) + (result >> 16);

    return result == 0xffff;
}

static string TcpFlags(ushort flags)
{
    string[] names = ["FIN", "SYN", "RST", "PSH", "ACK", "URG"];
    var result = new List<string>();

    for (int i = 0; i < names.Length; i++)
    {
        if ((flags & (1 << i)) != 0)
            result.Add(names[i]);
    }

    return result.Count == 0 ? "none" : string.Join(",", result);
}