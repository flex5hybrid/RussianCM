using Content.Shared._RMC14.Armor.ThermalCloak;
using Content.Shared._RuMC14.BatteryDrain;

namespace Content.Shared._RuMC14.ThermalCloak;

public sealed partial class RuMCThermalCloakBatterySystem : EntitySystem
{
    [Dependency] private RuMCBatteryDrainSystem _drain = default!;
    [Dependency] private ThermalCloakSystem _cloak = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RuMCBatteryDrainComponent, ThermalCloakTurnInvisibleActionEvent>(OnCloakAction,
            before: [typeof(ThermalCloakSystem)]);
        SubscribeLocalEvent<ThermalCloakComponent, RuMCBatteryDepletedEvent>(OnDepleted);
    }

    private void OnCloakAction(Entity<RuMCBatteryDrainComponent> ent, ref ThermalCloakTurnInvisibleActionEvent args)
    {
        if (args.Handled ||
            !TryComp(ent, out ThermalCloakComponent? cloak) ||
            cloak.Enabled ||
            _drain.CanActivate(ent, args.Performer))
        {
            return;
        }

        args.Handled = true;
    }

    private void OnDepleted(Entity<ThermalCloakComponent> ent, ref RuMCBatteryDepletedEvent args)
    {
        if (Transform(ent).ParentUid is { Valid: true } user)
            _cloak.SetInvisibility(ent, user, false, true);
    }
}
