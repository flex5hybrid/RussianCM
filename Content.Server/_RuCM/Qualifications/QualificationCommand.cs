using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._RuCM.Qualifications;

[AnyCommand]
public sealed class QualificationCommand : LocalizedCommands
{
    [Dependency] private IEntitySystemManager _systems = default!;
    public override string Command => "qualifications";
    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player == null) { shell.WriteError(Loc.GetString("rucm-qualifications-player-command")); return; }
        _systems.GetEntitySystem<QualificationSystem>().Open(shell.Player, args.Length > 0 && args[0] == "bui");
    }
}
