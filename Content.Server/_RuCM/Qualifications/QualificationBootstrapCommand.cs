using System;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._RuCM.Qualifications;

[AdminCommand(AdminFlags.Host)]
public sealed class QualificationBootstrapCommand : LocalizedCommands
{
    [Dependency] private IEntitySystemManager _systems = default!;
    public override string Command => "qualifications_acl";
    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        // Bootstrap is deliberately available only to the local server console. Active Hosts use Management UI.
        if (shell.Player != null) { shell.WriteError(Loc.GetString("rucm-qualifications-console-only")); return; }
        if (args.Length != 3 || !Guid.TryParse(args[0], out var target) || !bool.TryParse(args[1], out var enabled))
        { shell.WriteError(Help); return; }
        _systems.GetEntitySystem<QualificationSystem>().BootstrapManagement(target, enabled, args[2],
            error => shell.WriteLine(Loc.GetString(error.Length == 0 ? "rucm-qualifications-saved" : "rucm-qualifications-error-" + error)));
    }
}
