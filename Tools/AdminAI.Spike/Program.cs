using System.Net.Sockets;
using Content.Server.CMU14.AdminAI;
using Content.Tools.AdminAI.Spike;

if (args.Length != 2 || args[0] is not ("worker" or "probe" or "selftest" or "model-smoke"))
{
    Console.Error.WriteLine("Usage: AdminAI.Spike <worker|probe|selftest|model-smoke> <absolute .sock path, <=107 UTF-8 bytes>");
    return 1;
}

var path = args[1];
if (!Path.IsPathFullyQualified(path) || !path.EndsWith(".sock", StringComparison.Ordinal)
    || System.Text.Encoding.UTF8.GetByteCount(path) > 107)
{
    Console.Error.WriteLine("Invalid local socket path. Choose a short writable path; existing sockets are never deleted.");
    return 1;
}

try
{
    using var lifetime = new CancellationTokenSource(TimeSpan.FromMinutes(5));
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; lifetime.Cancel(); };
    if (args[0] == "worker")
    {
        using var worker = new MockWorker(path);
        Console.WriteLine("MOCK WORKER ready (no model, no game integration).");
        await worker.RunAsync(lifetime.Token);
        return 0;
    }

    MockWorker? fixture = args[0] == "selftest" ? new MockWorker(path) : null;
    using (fixture)
    {
        var workerTask = fixture?.RunAsync(lifetime.Token);
        try
        {
            var modelSmoke = args[0] == "model-smoke";
            var options = modelSmoke
                ? new AIInferenceOptions(Enabled: true, SocketPath: path, TimeoutSeconds: 45)
                : new AIInferenceOptions(Enabled: true, TimeoutSeconds: 5);
            // Real model acceptance uses the production endpoint validator and timeout.
            using var client = modelSmoke ? new UdsAIInferenceClient(options) : new UdsAIInferenceClient(path, options);
            var health = await client.CheckHealthAsync(lifetime.Token);
            Console.WriteLine($"UDS health: {health}");
            if (health != AIWorkerHealth.Ready)
                return 2;
            var input = modelSmoke
                ? "Синтетический тест, реальных игроков нет. Игрок пишет: «Меня случайно задел союзник, что делать?» " +
                  "Напиши одно короткое нейтральное предложение для черновика администратора: попроси время и место события. " +
                  "Не выноси решений о виновности или наказаниях."
                : "This is a synthetic IPC probe; no player data.";
            var timer = System.Diagnostics.Stopwatch.StartNew();
            var result = await client.InferAsync(new(Guid.NewGuid(), input), lifetime.Token);
            Console.WriteLine($"UDS inference protocol: {result.Status}");
            if (modelSmoke)
            {
                Console.WriteLine($"Synthetic model request elapsed: {timer.Elapsed.TotalSeconds:F2}s");
                // Only this fixed synthetic request may expose a sample, never real player data.
                if (result.Status == AIInferenceStatus.Success)
                    Console.WriteLine($"UNTRUSTED SYNTHETIC DRAFT: {result.UntrustedText}");
            }
            // Ordinary mock/probe modes never print prompts or responses.
            return result.Status == AIInferenceStatus.Success ? 0 : 2;
        }
        finally
        {
            lifetime.Cancel();
            if (workerTask != null)
            {
                try { await workerTask; }
                catch (OperationCanceledException) { }
            }
        }
    }
}
catch (SocketException exception)
{
    Console.Error.WriteLine($"IPC_BLOCKED: SocketError={exception.SocketErrorCode}, native={exception.NativeErrorCode}: {exception.Message}");
    return 2;
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine("IPC probe cancelled or lifetime expired.");
    return 2;
}
catch (Exception exception) when (exception is IOException or ArgumentException or PlatformNotSupportedException)
{
    Console.Error.WriteLine($"IPC_BLOCKED: {exception.GetType().Name}: {exception.Message}");
    return 2;
}
