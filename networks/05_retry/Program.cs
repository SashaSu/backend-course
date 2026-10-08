using System.Net;
using System.Net.Http.Headers;

var url = args[0];
var method = "GET";
var maxAttempts = 5;
string? idempotencyKey = null;

for (var i = 1; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--method":
            method = args[++i];
            break;
        case "--max-attempts":
            maxAttempts = int.Parse(args[++i]);
            break;
        case "--idempotency-key":
            idempotencyKey = args[++i];
            break;
    }
}

var canRetryMethod = method != "POST" || idempotencyKey != null;
var success = false;
var attempts = 0;

for (var attempt = 1; attempt <= maxAttempts; attempt++)
{
    attempts = attempt;
    HttpResponseMessage? response = null;

    try
    {
        using var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            UseProxy = false
        };
        using var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(5)
        };
        client.DefaultRequestHeaders.ConnectionClose = true;

        using var request = new HttpRequestMessage(new HttpMethod(method), url);

        if (idempotencyKey != null)
            request.Headers.Add("Idempotency-Key", idempotencyKey);

        response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead);

        await response.Content.ReadAsByteArrayAsync();

        Console.WriteLine($"attempt {attempt} status {(int)response.StatusCode}");

        if ((int)response.StatusCode is >= 200 and <= 399)
        {
            success = true;
            break;
        }

        if (!ShouldRetry((int)response.StatusCode) || !canRetryMethod || attempt == maxAttempts)
            break;

        var delay = GetDelay(attempt + 1, response.Headers.RetryAfter);
        Console.WriteLine($"sleep_ms {delay}");
        await Task.Delay(delay);
    }
    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
    {
        Console.WriteLine($"attempt {attempt} error {GetErrorText(ex)}");

        if (!canRetryMethod || attempt == maxAttempts)
            break;

        var delay = GetDelay(attempt + 1, null);
        Console.WriteLine($"sleep_ms {delay}");
        await Task.Delay(delay);
    }
    finally
    {
        response?.Dispose();
    }
}

Console.WriteLine($"result {(success ? "success" : "failure")} attempts {attempts}");
Environment.ExitCode = success ? 0 : 1;

static bool ShouldRetry(int statusCode)
{
    return statusCode is 429 or 500 or 502 or 503 or 504;
}

static int GetDelay(int attempt, RetryConditionHeaderValue? retryAfter)
{
    if (retryAfter?.Delta is { } delta)
        return Math.Max(0, (int)delta.TotalMilliseconds);

    var interval = Math.Min(2000, 200 * Math.Pow(2, attempt - 1));
    return Random.Shared.Next(0, (int)interval + 1);
}

static string GetErrorText(Exception ex)
{
    return ex is TaskCanceledException
        ? "timeout"
        : ex.Message;
}

