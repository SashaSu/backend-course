using System.Text;

using var memory = new MemoryStream();
await Console.OpenStandardInput().CopyToAsync(memory);
var data = memory.ToArray();

try
{
    var output = Parse(data);
    Console.Out.Write(output);
    Console.Out.Flush();
}
catch (HttpParseException ex)
{
    Console.Out.Write($"error {ex.Reason}\n");
    Console.Out.Flush();
    Environment.ExitCode = 1;
}

static string Parse(byte[] data)
{
    var sb = new StringBuilder();
    var startLineEnd = Find(data, [13, 10]);
    if (startLineEnd < 0)
        throw new HttpParseException("bad_start_line");
    var firstLf = Array.IndexOf(data, (byte)10);
    if (firstLf != startLineEnd + 1)
        throw new HttpParseException("bad_start_line");
    var firstLine = Encoding.ASCII.GetString(data[..startLineEnd]);
    if (firstLine.StartsWith("HTTP/", StringComparison.Ordinal))
    {
        sb.Append("type response\n");
        ParseResponseLine(firstLine, sb);
    }
    else
    {
        sb.Append("type request\n");
        ParseRequestLine(firstLine, sb);
    }
    var headerEnd = Find(data, [13, 10, 13, 10], startLineEnd);
    if (headerEnd < 0)
        throw new HttpParseException("bad_header");
    var headerBytes = data[(startLineEnd + 2)..(headerEnd + 2)];
    var headers = ParseHeaders(SplitLines(headerBytes));
    foreach (var (name, value) in headers)
        sb.Append($"header {name} {value}\n");
    var bodyStart = headerEnd + 4;
    byte[] body;
    var te = headers.FirstOrDefault(h => h.Name == "transfer-encoding");
    var isChunked = te.Name != null && te.Value.Split(',').Select(s => s.Trim()).LastOrDefault()?.Equals("chunked", StringComparison.OrdinalIgnoreCase) == true;
    if (isChunked)
    {
        body = ReadChunked(data, bodyStart);
    }
    else
    {
        var cl = headers.FirstOrDefault(h => h.Name == "content-length");
        if (cl.Name != null)
        {
            var text = cl.Value;
            if (text.Length == 0 || !text.All(c => c >= '0' && c <= '9'))
                throw new HttpParseException("bad_header");

            if (!long.TryParse(text, out var length))
                throw new HttpParseException("incomplete_body");

            var available = data.Length - bodyStart;
            if (available < length)
                throw new HttpParseException("incomplete_body");

            body = data[bodyStart..(bodyStart + (int)length)];
        }
        else
        {
            body = [];
        }
    }
    sb.Append($"body.length {body.Length}\n");
    if (body.Length > 0 && body.All(b => b >= 0x20 && b <= 0x7E))
        sb.Append($"body.text {Encoding.ASCII.GetString(body)}\n");
    return sb.ToString();
}

static byte[] ReadChunked(byte[] data, int pos)
{
    var result = new List<byte>();
    while (true)
    {
        var lineEnd = Find(data, [13, 10], pos);
        if (lineEnd < 0)
            throw new HttpParseException("bad_chunk");
        var sizeText = Encoding.ASCII.GetString(data[pos..lineEnd]);
        var semi = sizeText.IndexOf(';');
        if (semi >= 0)
            sizeText = sizeText[..semi];
        sizeText = sizeText.Trim(' ', '\t');
        if (sizeText.Length == 0 || !sizeText.All(Uri.IsHexDigit))
            throw new HttpParseException("bad_chunk");
        if (!long.TryParse(sizeText, System.Globalization.NumberStyles.AllowHexSpecifier,
                null, out var size))
            throw new HttpParseException("bad_chunk");
        pos = lineEnd + 2;
        if (size == 0)
            return result.ToArray();
        if (data.Length - pos < size + 2)
            throw new HttpParseException("bad_chunk");
        var end = pos + (int)size;
        if (data[end] != 13 || data[end + 1] != 10)
            throw new HttpParseException("bad_chunk");
        for (var i = pos; i < end; i++)
            result.Add(data[i]);
        pos = end + 2;
    }
}
static bool IsTokenChar(char c) =>
    (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || "!#$%&'*+-.^_`|~".Contains(c);

static List<(string Name, string Value)> ParseHeaders(List<byte[]> lines)
{
    var headers = new List<(string Name, string Value)>();

    foreach (var line in lines)
    {
        var colon = Array.IndexOf(line, (byte)':');
        if (colon <= 0)
            throw new HttpParseException("bad_header");
        var name = Encoding.ASCII.GetString(line[..colon]);
        if (!name.All(IsTokenChar))
            throw new HttpParseException("bad_header");
        var start = colon + 1;
        while (start < line.Length && (line[start] == 32 || line[start] == 9))
            start++;
        var end = line.Length;
        while (end > start && (line[end - 1] == 32 || line[end - 1] == 9))
            end--;
        var value = Encoding.ASCII.GetString(line[start..end]);
        headers.Add((name.ToLowerInvariant(), value));
    }
    return headers;
}

static void ParseRequestLine(string line, StringBuilder sb)
{
    var parts = line.Split(' ');
    if (parts.Length != 3 || parts[0].Length == 0 || parts[1].Length == 0 || parts[2] != "HTTP/1.1")
        throw new HttpParseException("bad_start_line");
    sb.Append($"method {parts[0]}\n");
    sb.Append($"target {parts[1]}\n");
    sb.Append($"version {parts[2]}\n");
}

static void ParseResponseLine(string line, StringBuilder sb)
{
    if (!line.StartsWith("HTTP/1.1 ", StringComparison.Ordinal))
        throw new HttpParseException("bad_start_line");
    var rest = line[9..];
    if (rest.Length < 3 ||
        !rest[..3].All(c => c >= '0' && c <= '9') ||
        (rest.Length > 3 && rest[3] != ' '))
        throw new HttpParseException("bad_start_line");
    var status = rest[..3];
    var reason = rest.Length > 4 ? rest[4..] : "";
    sb.Append("version HTTP/1.1\n");
    sb.Append($"status {status}\n");
    sb.Append($"reason {reason}\n");
}

static List<byte[]> SplitLines(byte[] data)
{
    var result = new List<byte[]>();
    var start = 0;
    while (start < data.Length)
    {
        var end = Find(data, [13, 10], start);
        if (end < 0)
            throw new HttpParseException("bad_header");
        result.Add(data[start..end]);
        start = end + 2;
    }
    return result;
}

static int Find(byte[] data, byte[] pattern, int start = 0)
{
    for (var i = start; i <= data.Length - pattern.Length; i++)
    {
        var match = true;
        for (var j = 0; j < pattern.Length; j++)
        {
            if (data[i + j] != pattern[j])
            {
                match = false;
                break;
            }
        }

        if (match)
            return i;
    }
    return -1;
}

class HttpParseException(string reason) : Exception
{ 
    public string Reason { get; } = reason;
}