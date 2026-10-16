using System.Threading;
using System.Threading.Tasks;

namespace Content.Server.CMU14.AdminAI;

/// <summary>
/// Explicitly unavailable implementation while the stage A IPC gate is blocked.
/// It is not registered with the game and does not simulate successful inference.
/// </summary>
public sealed class UnavailableAIInferenceClient(AIInferenceOptions options) : IAIInferenceClient
{
    public Task<AIWorkerHealth> CheckHealthAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(options.Enabled ? AIWorkerHealth.Unavailable : AIWorkerHealth.Disabled);
    }

    public Task<AIInferenceResult> InferAsync(AIInferenceRequest request, CancellationToken cancellationToken)
    {
        var status = cancellationToken.IsCancellationRequested
            ? AIInferenceStatus.Cancelled
            : options.Enabled ? AIInferenceStatus.Unavailable : AIInferenceStatus.Disabled;
        return Task.FromResult(new AIInferenceResult(request.CorrelationId, status));
    }
}
