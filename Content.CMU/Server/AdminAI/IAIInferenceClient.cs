using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Content.Server.CMU14.AdminAI;

/// <summary>
/// Local inference boundary. No game actions, sessions, database handles or tools are exposed here.
/// All successful output is untrusted data, never a command or a player-facing message.
/// </summary>
public interface IAIInferenceClient
{
    Task<AIWorkerHealth> CheckHealthAsync(CancellationToken cancellationToken);
    Task<AIInferenceResult> InferAsync(AIInferenceRequest request, CancellationToken cancellationToken);
}

public sealed record AIInferenceTurn(bool FromAdmin, string Text);
public sealed record AIInferenceRequest(Guid CorrelationId, string Input, int? OutputTokenLimit = null)
{
    public ImmutableArray<AIInferenceTurn> Dialogue { get; init; } = ImmutableArray<AIInferenceTurn>.Empty;
    public long InputChars => (Input?.Length ?? 0) + (Dialogue.IsDefault ? 0 : Dialogue.Sum(turn => (long)(turn?.Text?.Length ?? 0)));
    public bool IsInputValid(int maxChars) => !string.IsNullOrWhiteSpace(Input) && !Dialogue.IsDefault
        && Dialogue.Length <= 16 && Dialogue.All(turn => turn != null && !string.IsNullOrWhiteSpace(turn.Text) && turn.Text.Length <= 1024)
        && InputChars <= maxChars;
}

public enum AIInferenceStatus : byte
{
    Disabled,
    Unavailable,
    TimedOut,
    Cancelled,
    InvalidResponse,
    Success,
    QueueFull,
    Duplicate,
    Expired,
    Paused,
    InvalidRequest,
}

public enum AIWorkerHealth : byte
{
    Disabled,
    Unavailable,
    Ready,
}

public sealed record AIInferenceResult(Guid CorrelationId, AIInferenceStatus Status, string? UntrustedText = null);
