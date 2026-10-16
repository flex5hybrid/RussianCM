using Content.Server.Administration;
using System.Globalization;
using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.CMU14.AdminAI;

[AdminCommand(AdminFlags.Debug)]
public sealed class AdminAIStatusCommand : IConsoleCommand
{
    [Dependency] private IAdminAIService _service = default!;
    [Dependency] private IAdminManager _admins = default!;
    public string Command => "admin_ai_status";
    public string Description => Loc.GetString("cmu-admin-ai-status-description");
    public string Help => Loc.GetString("cmu-admin-ai-status-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player == null ? !shell.IsLocal : !_admins.HasAdminFlag(shell.Player, AdminFlags.Debug)) return;
        if (args.Length != 0) { shell.WriteError(Help); return; }
        var snapshot = _service.Snapshot();
        shell.WriteLine(Loc.GetString("cmu-admin-ai-status", ("enabled", snapshot.Enabled.ToString()),
            ("state", snapshot.State.ToString()), ("health", snapshot.Health.ToString()), ("reason", snapshot.Reason),
            ("pending", snapshot.Queue?.Pending ?? 0), ("running", snapshot.Queue?.Running == true ? 1 : 0),
            ("accepted", snapshot.Queue?.Accepted ?? 0), ("completed", snapshot.Queue?.Completed ?? 0),
            ("rejected", snapshot.Queue?.Rejected ?? 0)));
        if (snapshot.Load is { } load)
            shell.WriteLine(Loc.GetString("cmu-admin-ai-status-load",
                ("cpu", load.CpuSomeAvg10.ToString("F2", CultureInfo.InvariantCulture)),
                ("memory", load.MemoryFullAvg10.ToString("F2", CultureInfo.InvariantCulture)),
                ("io", load.IoFullAvg10.ToString("F2", CultureInfo.InvariantCulture)),
                ("tick", load.SimulationBudgetRatio.ToString("F3", CultureInfo.InvariantCulture))));
    }
}
