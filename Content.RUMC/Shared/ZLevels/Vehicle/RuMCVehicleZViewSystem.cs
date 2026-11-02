using Content.Shared._RuMC14.Vehicle;
using Content.Shared.CMU14.ZLevels.Core.Components;
using Content.Shared.CMU14.ZLevels.Core.EntitySystems;
using Content.Shared.Vehicle.Components;

namespace Content.Shared._RuMC14.ZLevels.Vehicle;

/// <summary>
/// Crew looking out of a vehicle see Z-levels through the hull's viewer, not their own.
/// </summary>
public sealed class RuMCVehicleZViewSystem : EntitySystem
{
    [Dependency] private CMUSharedZLevelsSystem _zLevels = default!;

    private EntityQuery<EyeComponent> _eyeQuery;
    private EntityQuery<VehicleComponent> _vehicleQuery;
    private EntityQuery<CMUZLevelViewerComponent> _viewerQuery;

    public override void Initialize()
    {
        base.Initialize();

        _eyeQuery = GetEntityQuery<EyeComponent>();
        _vehicleQuery = GetEntityQuery<VehicleComponent>();
        _viewerQuery = GetEntityQuery<CMUZLevelViewerComponent>();

        SubscribeLocalEvent<CMUZLevelViewerComponent, RuMCVehicleOccupantEjectedEvent>(OnOccupantEjected);
    }

    // thrown out of a burning hull, nobody should be left aiming at the floor above
    private void OnOccupantEjected(Entity<CMUZLevelViewerComponent> ent, ref RuMCVehicleOccupantEjectedEvent args)
    {
        _zLevels.TryDisableLookUp(ent);
    }

    /// <summary>
    /// The entity whose <see cref="CMUZLevelViewerComponent"/> and map define what <paramref name="user"/> sees:
    /// the vehicle while their eye is on its hull, otherwise the user.
    /// </summary>
    public EntityUid GetViewSource(EntityUid user)
    {
        return _eyeQuery.TryComp(user, out var eye) &&
               eye.Target is { } target &&
               _vehicleQuery.HasComp(target) &&
               _viewerQuery.HasComp(target)
            ? target
            : user;
    }
}
