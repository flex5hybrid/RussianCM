using Content.Shared._RMC14.NightVision;
using Content.Shared._RuMC14.BatteryDrain;
using Content.Shared.Toggleable;

namespace Content.Shared._RuMC14.NightVision;

public sealed partial class RuMCNightVisionBatterySystem : EntitySystem
{
    [Dependency] private RuMCBatteryDrainSystem _drain = default!;
    [Dependency] private SharedNightVisionSystem _nightVision = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RuMCBatteryDrainComponent, ToggleActionEvent>(OnToggleAction,
            before: [typeof(SharedNightVisionSystem)]);
        SubscribeLocalEvent<NightVisionItemComponent, RuMCBatteryDepletedEvent>(OnDepleted);
    }

    private void OnToggleAction(Entity<RuMCBatteryDrainComponent> ent, ref ToggleActionEvent args)
    {
        if (args.Handled ||
            !TryComp(ent, out NightVisionItemComponent? item) ||
            item.User != null ||
            _drain.CanActivate(ent, args.Performer))
        {
            return;
        }

        args.Handled = true;
    }

    private void OnDepleted(Entity<NightVisionItemComponent> ent, ref RuMCBatteryDepletedEvent args)
    {
        if (ent.Comp.User is { } user)
            _nightVision.DisableNightVisionItem(ent, user);
    }
}
