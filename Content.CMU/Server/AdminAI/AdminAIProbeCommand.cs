using System;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.CMU14.AdminAI;

/// <summary>Manual stage A health probe only; no model generation, ahelp traffic or game actions.</summary>
[AdminCommand(AdminFlags.Debug)]
public sealed class AdminAIProbeCommand : IConsoleCommand
{
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private IAdminManager _admins = default!;
    [Dependency] private ITaskManager _tasks = default!;
    private bool _busy;

    public string Command => "admin_ai_probe";
    public string Description => Loc.GetString("cmu-admin-ai-probe-description");
    public string Help => Loc.GetString("cmu-admin-ai-probe-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player;
        if (player == null ? !shell.IsLocal : !_admins.HasAdminFlag(player, AdminFlags.Debug))
            return;
        if (args.Length != 0)
        {
            shell.WriteError(Help);
            return;
        }
        var options = AdminAICVars.Read(_config);
        if (!options.Enabled || !options.IsValid())
        {
            shell.WriteLine(Loc.GetString("cmu-admin-ai-probe-disabled"));
            return;
        }
        if (_busy)
        {
            shell.WriteLine(Loc.GetString("cmu-admin-ai-probe-busy"));
            return;
        }
        _busy = true;
        // Snapshot config on the game thread. All socket/HTTP work runs away from that thread.
        _ = Task.Run(async () =>
        {
            var health = AIWorkerHealth.Unavailable;
            try
            {
                using var client = new UdsAIInferenceClient(options);
                health = await client.CheckHealthAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Diagnostic failures must not escape an unobserved task or expose socket paths.
            }
            var result = health;
            _tasks.RunOnMainThread(() =>
            {
                _busy = false;
                if (shell.Player != player || AdminAICVars.Read(_config) != options)
                    return;
                if (player == null ? !shell.IsLocal : !_admins.HasAdminFlag(player, AdminFlags.Debug))
                    return;
                shell.WriteLine(Loc.GetString(result == AIWorkerHealth.Ready
                    ? "cmu-admin-ai-probe-ready" : "cmu-admin-ai-probe-unavailable"));
            });
        });
    }
}
