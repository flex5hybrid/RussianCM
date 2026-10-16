using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace Content.Tools.AdminAI.Spike;

/// <summary>Deterministic HTTP fixture. It does not load a model or perform inference.</summary>
internal sealed class MockWorker : IDisposable
{
    private readonly Socket _listener;
    private readonly string _path;
    private readonly MockWorkerFault _fault;
    private readonly TaskCompletionSource<bool> _requestReceived = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<bool> _releaseStall = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task RequestReceived => _requestReceived.Task;
    public int RequestCount { get; private set; }
    public void ReleaseStall() => _releaseStall.TrySetResult(true);

    public MockWorker(string path, MockWorkerFault fault = MockWorkerFault.None)
    {
        _path = path;
        _fault = fault;
        _listener = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        try
        {
            // Never unlink an endpoint owned by another worker.
            _listener.Bind(new UnixDomainSocketEndPoint(path));
            _listener.Listen(1);
        }
        catch
        {
            _listener.Dispose();
            throw;
        }
    }

    public async Task RunAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            using var connection = await _listener.AcceptAsync(token);
            await using var stream = new NetworkStream(connection, ownsSocket: false);
            using var requestTimeout = CancellationTokenSource.CreateLinkedTokenSource(token);
            requestTimeout.CancelAfter(TimeSpan.FromSeconds(5));
            try
            {
                await ReplyAsync(stream, requestTimeout.Token);
            }
            catch (IOException) { /* A cancelled/disconnected test client must not kill the mock listener. */ }
            catch (SocketException) { }
            catch (OperationCanceledException) when (!token.IsCancellationRequested) { }
        }
    }

    private async Task ReplyAsync(NetworkStream stream, CancellationToken token)
    {
        var header = new List<byte>();
        var single = new byte[1];
        while (header.Count < 8192)
        {
            var read = await stream.ReadAsync(single, token);
            if (read == 0)
                throw new IOException("Truncated mock request.");
            header.Add(single[0]);
            if (header.Count >= 4 && header[^4] == '\r' && header[^3] == '\n'
                && header[^2] == '\r' && header[^1] == '\n')
                break;
        }
        if (header.Count >= 8192)
            throw new IOException("Mock header limit exceeded.");

        var lines = Encoding.ASCII.GetString(header.ToArray()).Split("\r\n", StringSplitOptions.None);
        var contentLength = 0;
        foreach (var line in lines)
        {
            if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase)
                && (!int.TryParse(line[15..].Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out contentLength)
                    || contentLength is < 0 or > 65536))
                throw new IOException("Invalid mock content length.");
        }
        var input = new byte[contentLength];
        await stream.ReadExactlyAsync(input, token);
        RequestCount++;

        byte[] body;
        var status = "200 OK";
        if (lines[0] == "GET /health HTTP/1.1")
            body = "{\"status\":\"ok\"}"u8.ToArray();
        else if (lines[0] == "POST /v1/chat/completions HTTP/1.1")
        {
            using var request = JsonDocument.Parse(input, new JsonDocumentOptions { MaxDepth = 16 });
            if (request.RootElement.GetProperty("stream").GetBoolean())
                throw new IOException("Streaming is not supported by the mock.");
            _requestReceived.TrySetResult(true);
            if (_fault == MockWorkerFault.Disconnect)
                return;
            if (_fault == MockWorkerFault.StallHeaders)
                await _releaseStall.Task.WaitAsync(token);
            if (_fault is MockWorkerFault.StallBody or MockWorkerFault.TruncatedBody)
            {
                await stream.WriteAsync("HTTP/1.1 200 OK\r\nContent-Length: 100\r\nConnection: close\r\n\r\n{"u8.ToArray(), token);
                if (_fault == MockWorkerFault.StallBody)
                    await _releaseStall.Task.WaitAsync(token);
                return;
            }
            if (_fault == MockWorkerFault.Redirect)
            {
                await stream.WriteAsync("HTTP/1.1 302 Found\r\nLocation: http://external.invalid/\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"u8.ToArray(), token);
                return;
            }
            if (_fault == MockWorkerFault.OversizedBodyWithoutLength)
            {
                await stream.WriteAsync("HTTP/1.1 200 OK\r\nConnection: close\r\n\r\n"u8.ToArray(), token);
                await stream.WriteAsync(new byte[Content.Server.CMU14.AdminAI.AIInferenceOptions.MaxResponseBytes + 1], token);
                return;
            }
            body = JsonSerializer.SerializeToUtf8Bytes(new
            {
                choices = new[]
                {
                    new { finish_reason = "stop", message = new { role = "assistant",
                        content = "[MOCK WORKER: NO MODEL] HTTP/JSON over UDS received." } },
                },
            });
            if (_fault == MockWorkerFault.MalformedJson)
                body = "{"u8.ToArray();
            else if (_fault == MockWorkerFault.EmptyBody)
                body = [];
        }
        else
        {
            status = "404 Not Found";
            body = "{}"u8.ToArray();
        }

        var responseHeader = Encoding.ASCII.GetBytes($"HTTP/1.1 {status}\r\nContent-Type: application/json\r\n" +
            $"Content-Length: {body.Length}\r\nConnection: close\r\n\r\n");
        await stream.WriteAsync(responseHeader, token);
        await stream.WriteAsync(body, token);
    }

    public void Dispose()
    {
        _listener.Dispose();
        if (File.Exists(_path))
            File.Delete(_path);
    }
}

public enum MockWorkerFault
{
    None,
    StallHeaders,
    StallBody,
    Disconnect,
    TruncatedBody,
    MalformedJson,
    EmptyBody,
    OversizedBodyWithoutLength,
    Redirect,
}
