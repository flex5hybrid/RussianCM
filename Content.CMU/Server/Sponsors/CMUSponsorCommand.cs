using System.Linq;
using Content.Server._RMC14.LinkAccount;
using Content.Server.Administration;
using Content.Server.Database;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.CMU14.Sponsors;

[AdminCommand(AdminFlags.Host)]
public sealed class CMUSponsorCommand : IConsoleCommand
{
    [Dependency] private IServerDbManager _db = default!;
    [Dependency] private IEntityManager _entities = default!;
    [Dependency] private LinkAccountManager _link = default!;
    [Dependency] private IPlayerLocator _players = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private IComponentFactory _components = default!;

    public string Command => "cmusponsor";
    public string Description => "Review and approve sponsor cosmetics.";
    public string Help => "cmusponsor review <player> | approve <player> figurine|lobby <exact-reviewed-text> | custom <player> <prototype-or-clear>";

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 2)
        {
            shell.WriteLine(Help);
            return;
        }
        try
        {
            var player = await _players.LookupIdByNameOrIdAsync(args[1]);
            if (player == null)
            {
                shell.WriteError("Player not found.");
                return;
            }
            var id = player.UserId;
            var preferences = await _db.GetCMUSponsorPreferences(id.UserId);
            var patron = await _db.GetPatron(id.UserId, default);
            var settings = LinkAccountManager.DeserializeSponsorSettings(preferences?.Settings);

            switch (args[0])
            {
                case "review" when args.Length == 2:
                    shell.WriteLine($"{player.Username}: {patron?.Tier.Name ?? "no subscription"}");
                    shell.WriteLine($"Figurine draft: {settings.FigurineDescription}");
                    shell.WriteLine($"Approved figurine text: {preferences?.ApprovedFigurineDescription ?? ""}");
                    shell.WriteLine($"Lobby draft: {patron?.LobbyMessage?.Message ?? ""}; approved={patron?.LobbyMessage?.Approved == true}");
                    shell.WriteLine($"Custom item: {preferences?.CustomItem ?? ""}");
                    shell.WriteLine("Custom orders require manual verification of the agreed paid-subscription history before assignment.");
                    return;

                case "approve" when args.Length == 4 && args[2] == "figurine":
                    if (patron?.Tier.Figurines != true || preferences == null || settings.FigurineDescription != args[3]
                        || !await _db.ApproveCMUSponsorFigurine(id.UserId, preferences.Settings, args[3]))
                    {
                        shell.WriteError("The current eligible draft does not match the reviewed text. Run 'cmusponsor review' again.");
                        return;
                    }
                    break;

                case "approve" when args.Length == 4 && args[2] == "lobby":
                    if (patron?.Tier.LobbyMessage != true || !await _db.ApproveCMUSponsorLobby(id.UserId, args[3]))
                    {
                        shell.WriteError("The current eligible draft does not match the reviewed text. Run 'cmusponsor review' again.");
                        return;
                    }
                    break;

                case "custom" when args.Length == 3:
                    var prototypeId = args[2] == "clear" ? "" : args[2];
                    if (prototypeId.Length > 0 && (patron?.Tier.Priority != 1
                        || !_prototypes.TryIndex<EntityPrototype>(prototypeId, out var prototype)
                        || prototype.Abstract
                        || !prototype.TryComp(out CMUSponsorCustomItemComponent? cosmetic, _components)
                        || cosmetic.Owner != id.UserId))
                    {
                        shell.WriteError("Only an owner-marked, approved cosmetic prototype can be assigned to the highest tier.");
                        return;
                    }
                    await _db.SetCMUSponsorCustomItem(id.UserId, prototypeId);
                    break;

                default:
                    shell.WriteLine(Help);
                    return;
            }
            await _link.RefreshPatron(id);
            _entities.System<CMUSponsorSystem>().ReloadApprovals();
            shell.WriteLine("Sponsor reward updated.");
        }
        catch (Exception e)
        {
            shell.WriteError($"Could not update sponsor reward: {e.Message}");
        }
    }
}
