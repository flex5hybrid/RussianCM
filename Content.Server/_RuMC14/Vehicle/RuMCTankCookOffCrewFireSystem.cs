using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.Vehicle;
using Content.Shared._RuMC14.Vehicle;
using Content.Shared.Atmos.Components;

namespace Content.Server._RuMC14.Vehicle;

public sealed partial class RuMCTankCookOffCrewFireSystem : EntitySystem
{
    [Dependency] private SharedRMCFlammableSystem _flammable = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<FlammableComponent, RuMCVehicleOccupantEjectedEvent>(OnEjected);
    }

    private void OnEjected(Entity<FlammableComponent> ent, ref RuMCVehicleOccupantEjectedEvent args)
    {
        if (!args.CookOff || !TryComp(args.Vehicle, out TankCookOffComponent? tank) || tank.CrewFireDuration <= 0)
            return;

        _flammable.Ignite(ent.AsNullable(), tank.CrewFireIntensity, tank.CrewFireDuration, tank.CrewFireDuration);
    }
}
