using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace Content.Server.CMU14.AdminAI;

/// <summary>Local UDS transport. No DNS, TCP fallback, redirects, tools or game actions.</summary>
public sealed class UdsAIInferenceClient : IAIInferenceClient, IDisposable
{
    private readonly HttpClient _client;
    private readonly AIInferenceOptions _options;

    public UdsAIInferenceClient(AIInferenceOptions options) : this(options.SocketPath, options) { }

    internal UdsAIInferenceClient(string socketPath, AIInferenceOptions options)
    {
        if (!options.IsValid())
            throw new ArgumentException("Invalid inference limits.", nameof(options));
        var endpoint = new UnixDomainSocketEndPoint(socketPath);
        var handler = new SocketsHttpHandler
        {
            UseProxy = false,
            AllowAutoRedirect = false,
            UseCookies = false,
            MaxConnectionsPerServer = 1,
            MaxResponseHeadersLength = 8,
            ConnectCallback = async (_, token) =>
            {
                var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                try
                {
                    await socket.ConnectAsync(endpoint, token).ConfigureAwait(false);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            },
        };
        _options = options;
        // URI provides HTTP framing only; ConnectCallback never uses DNS or TCP.
        _client = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost/"),
            Timeout = Timeout.InfiniteTimeSpan,
            DefaultRequestVersion = HttpVersion.Version11,
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact,
        };
    }

    public async Task<AIWorkerHealth> CheckHealthAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_options.Enabled)
            return AIWorkerHealth.Disabled;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "health");
            using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token)
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return AIWorkerHealth.Unavailable;
            var body = await ReadBoundedAsync(response, timeout.Token).ConfigureAwait(false);
            if (body == null)
                return AIWorkerHealth.Unavailable;
            using var document = JsonDocument.Parse(body, new JsonDocumentOptions { MaxDepth = 16 });
            return document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("status", out var status)
                && status.ValueKind == JsonValueKind.String && status.GetString() == "ok"
                ? AIWorkerHealth.Ready : AIWorkerHealth.Unavailable;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return AIWorkerHealth.Unavailable;
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or JsonException)
        {
            return AIWorkerHealth.Unavailable;
        }
    }

    public async Task<AIInferenceResult> InferAsync(AIInferenceRequest request, CancellationToken cancellationToken)
    {
        AIInferenceResult Failure(AIInferenceStatus status) => new(request.CorrelationId, status);
        if (cancellationToken.IsCancellationRequested)
            return Failure(AIInferenceStatus.Cancelled);
        if (!_options.Enabled)
            return Failure(AIInferenceStatus.Disabled);
        if (!_options.Accepts(request))
            return Failure(AIInferenceStatus.InvalidRequest);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));
        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions");
            message.Content = new ByteArrayContent(AIInferenceProtocol.SerializeRequest(request, _options));
            message.Content.Headers.ContentType = new("application/json");
            using var response = await _client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, timeout.Token)
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return Failure(AIInferenceStatus.Unavailable);
            var body = await ReadBoundedAsync(response, timeout.Token).ConfigureAwait(false);
            return body == null ? Failure(AIInferenceStatus.InvalidResponse)
                : AIInferenceProtocol.ParseResponse(request.CorrelationId, body, _options);
        }
        catch (OperationCanceledException)
        {
            return Failure(cancellationToken.IsCancellationRequested ? AIInferenceStatus.Cancelled : AIInferenceStatus.TimedOut);
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException)
        {
            return Failure(AIInferenceStatus.Unavailable);
        }
    }

    private static async Task<byte[]?> ReadBoundedAsync(HttpResponseMessage response, CancellationToken token)
    {
        if (response.Content.Headers.ContentLength > AIInferenceOptions.MaxResponseBytes)
            return null;
        await using var stream = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
        // One extra byte detects oversized responses even when Content-Length is absent.
        var buffer = new byte[AIInferenceOptions.MaxResponseBytes + 1];
        var count = 0;
        while (count < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(count), token).ConfigureAwait(false);
            if (read == 0)
                return buffer[..count];
            count += read;
        }
        return null;
    }

    public void Dispose() => _client.Dispose();
}
