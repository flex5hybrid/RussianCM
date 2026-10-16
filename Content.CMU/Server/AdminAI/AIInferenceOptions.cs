using System;
using System.Text;

namespace Content.Server.CMU14.AdminAI;

/// <summary>Immutable per-request snapshot. Invalid configuration must fail closed, not be clamped.</summary>
public sealed record AIInferenceOptions(
    bool Enabled = false,
    string SocketPath = "/run/rucmu-ai/llama.sock",
    int TimeoutSeconds = 45,
    int MaxInputChars = 4096,
    int MaxOutputTokens = 256,
    int MaxOutputChars = 4096)
{
    public const int MaxResponseBytes = 32768;

    public bool IsValid()
    {
        const string directory = "/run/rucmu-ai/";
        // Linux deployment contract; a URL, relative path or Windows path is not a production endpoint.
        // AF_UNIX paths must fit in sockaddr_un including the terminating NUL.
        if (string.IsNullOrWhiteSpace(SocketPath) || !SocketPath.StartsWith(directory, StringComparison.Ordinal))
            return false;
        foreach (var character in SocketPath.AsSpan(directory.Length))
        {
            if (character is not (>= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '.' or '-' or '_'))
                return false;
        }
        return SocketPath.Length > directory.Length + ".sock".Length
            && SocketPath.EndsWith(".sock", StringComparison.Ordinal)
            && !SocketPath.Contains("..", StringComparison.Ordinal)
            && !SocketPath.Contains('\0')
            && !SocketPath.Contains('\\')
            && Encoding.UTF8.GetByteCount(SocketPath) <= 107
            && TimeoutSeconds is >= 1 and <= 60
            && MaxInputChars is >= 1 and <= 16384
            && MaxOutputTokens is >= 1 and <= 512
            && MaxOutputChars is >= 1 and <= 8192;
    }

    public bool Accepts(AIInferenceRequest request)
    {
        return IsValid()
            && request.CorrelationId != Guid.Empty
            && request.IsInputValid(MaxInputChars)
            && (request.OutputTokenLimit == null || request.OutputTokenLimit is >= 1 and <= 512);
    }
}
