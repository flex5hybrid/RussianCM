using System;
using System.Threading.Tasks;

namespace Content.Server.Corvax.TTS;

/// <summary>
/// Contains failures across the entire asynchronous speech request, including delivery.
/// Event handlers must not let an async-void exception terminate the server process.
/// </summary>
public static class CMUTTSRequestRunner
{
    public static async Task Run(Func<Task> request, Action<Exception> onError)
    {
        try
        {
            await request();
        }
        catch (OperationCanceledException)
        {
            // Shutdown and round changes may cancel a pending request.
        }
        catch (Exception e)
        {
            onError(e);
        }
    }
}
