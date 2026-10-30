using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RuMC14.BatteryDrain;

public sealed partial class RuMCBatteryDrainSystem : EntitySystem
{
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private PowerCellSystem _powerCell = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RuMCBatteryDrainComponent, RuMCBatteryDrainActiveChangedEvent>(OnActiveChanged);
        SubscribeLocalEvent<RuMCBatteryDrainComponent, RefreshChargeRateEvent>(OnRefreshChargeRate);
        SubscribeLocalEvent<RuMCBatteryDrainComponent, BatteryStateChangedEvent>(OnBatteryStateChanged);
    }

    /// <summary>Whether the item can be turned on. Warns the user if not.</summary>
    public bool CanActivate(Entity<RuMCBatteryDrainComponent> ent, EntityUid? user, bool silent = false)
    {
        if (_battery.GetChargeLevel(ent.Owner) >= ent.Comp.MinActivationCharge)
            return true;

        if (user != null && !silent)
            _popup.PopupClient(Loc.GetString(ent.Comp.NoChargeMessage), user.Value, user.Value, PopupType.SmallCaution);

        return false;
    }

    /// <summary>Whether the item has a battery that is fully drained.</summary>
    public bool IsDepleted(EntityUid ent)
    {
        return HasComp<RuMCBatteryDrainComponent>(ent) && _battery.GetCharge(ent) <= 0f;
    }

    private void OnActiveChanged(Entity<RuMCBatteryDrainComponent> ent, ref RuMCBatteryDrainActiveChangedEvent args)
    {
        if (_timing.ApplyingState || TerminatingOrDeleted(ent))
            return;

        // SetDrawEnabled only refreshes slotted batteries
        _powerCell.SetDrawEnabled(ent.Owner, args.Active);

        if (!args.Active && TryComp(ent, out BatterySelfRechargerComponent? recharger))
            _battery.SetChargeCooldown((ent.Owner, recharger), recharger.AutoRechargePauseTime); // refreshes the rate too
        else
            _battery.RefreshChargeRate(ent.Owner);
    }

    private void OnRefreshChargeRate(Entity<RuMCBatteryDrainComponent> ent, ref RefreshChargeRateEvent args)
    {
        // Self-recharge works only when idle and outside a recharger
        if (TryComp(ent, out BatterySelfRechargerComponent? recharger) &&
            (recharger.NextAutoRecharge is not { } next || _timing.CurTime >= next) &&
            (HasComp<InsideChargerComponent>(ent) || TryComp(ent, out PowerCellDrawComponent? draw) && draw.Enabled))
        {
            args.NewChargeRate -= recharger.AutoRechargeRate;
        }
    }

    private void OnBatteryStateChanged(Entity<RuMCBatteryDrainComponent> ent, ref BatteryStateChangedEvent args)
    {
        if (_net.IsClient ||
            args.NewState != BatteryState.Empty ||
            !TryComp(ent, out PowerCellDrawComponent? draw) ||
            !draw.Enabled)
        {
            return;
        }

        var depleted = new RuMCBatteryDepletedEvent();
        RaiseLocalEvent(ent, ref depleted);
    }
}
