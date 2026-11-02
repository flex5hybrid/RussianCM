using Content.Shared._RMC14.Vehicle;
using Content.Shared.Popups;
using Robust.Shared.Network;

namespace Content.Shared._RuMC14.Blackfoot;

/// <summary>Keeps people out of a Blackfoot that was destroyed, as with a tank after its cook-off.</summary>
public sealed partial class RuMCBlackfootWreckEntrySystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RuMCBlackfootDestructionComponent, VehicleEntryAttemptEvent>(OnEntryAttempt);
    }

    private void OnEntryAttempt(Entity<RuMCBlackfootDestructionComponent> ent, ref VehicleEntryAttemptEvent args)
    {
        if (!TryComp(ent, out HardpointIntegrityComponent? frame) || !frame.DestroyedBeyondRepair)
            return;

        args.Cancelled = true;
        if (_net.IsServer)
            _popup.PopupEntity(Loc.GetString("rumc-blackfoot-wreck-entry-blocked"), ent.Owner, args.User, PopupType.SmallCaution);
    }
}
