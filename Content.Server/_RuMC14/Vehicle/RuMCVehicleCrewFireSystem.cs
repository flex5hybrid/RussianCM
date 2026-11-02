using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.Vehicle;
using Content.Shared._RuMC14.Blackfoot;
using Content.Shared._RuMC14.Vehicle;
using Content.Shared.Atmos.Components;

namespace Content.Server._RuMC14.Vehicle;

/// <summary>Sets the crew of a vehicle on fire when it throws them out in a cook-off.</summary>
public sealed partial class RuMCVehicleCrewFireSystem : EntitySystem
{
    [Dependency] private SharedRMCFlammableSystem _flammable = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<FlammableComponent, RuMCVehicleOccupantEjectedEvent>(OnEjected);
    }

    private void OnEjected(Entity<FlammableComponent> ent, ref RuMCVehicleOccupantEjectedEvent args)
    {
        if (!args.CookOff || !TryGetCrewFire(args.Vehicle, out var intensity, out var duration) || duration <= 0)
            return;

        _flammable.Ignite(ent.AsNullable(), intensity, duration, duration);
    }

    /// <summary>The fire a vehicle's crew is thrown out in: a cooked-off tank's or a crashed Blackfoot's.</summary>
    private bool TryGetCrewFire(EntityUid vehicle, out int intensity, out int duration)
    {
        if (TryComp(vehicle, out TankCookOffComponent? tank))
        {
            intensity = tank.CrewFireIntensity;
            duration = tank.CrewFireDuration;
            return true;
        }

        if (TryComp(vehicle, out RuMCBlackfootDestructionComponent? blackfoot))
        {
            intensity = blackfoot.CrewFireIntensity;
            duration = blackfoot.CrewFireDuration;
            return true;
        }

        intensity = 0;
        duration = 0;
        return false;
    }
}
