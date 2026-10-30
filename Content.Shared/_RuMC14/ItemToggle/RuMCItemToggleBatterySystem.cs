using Content.Shared._RuMC14.BatteryDrain;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;

namespace Content.Shared._RuMC14.ItemToggle;

public sealed partial class RuMCItemToggleBatterySystem : EntitySystem
{
    [Dependency] private RuMCBatteryDrainSystem _drain = default!;
    [Dependency] private ItemToggleSystem _toggle = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RuMCBatteryDrainComponent, ItemToggleActivateAttemptEvent>(OnActivateAttempt);
        SubscribeLocalEvent<ItemToggleComponent, RuMCBatteryDepletedEvent>(OnDepleted);
    }

    private void OnActivateAttempt(Entity<RuMCBatteryDrainComponent> ent, ref ItemToggleActivateAttemptEvent args)
    {
        if (!_drain.CanActivate(ent, args.User, args.Silent))
            args.Cancelled = true;
    }

    private void OnDepleted(Entity<ItemToggleComponent> ent, ref RuMCBatteryDepletedEvent args)
    {
        _toggle.TryDeactivate(ent.Owner);
    }
}
