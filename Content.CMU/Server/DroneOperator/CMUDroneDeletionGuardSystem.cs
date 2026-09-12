using Content.Shared.CMU14.DroneOperator;
using Content.Shared.Mobs.Systems;

namespace Content.Server.CMU14.DroneOperator;

/// <summary>
/// Prevents the drone termination handler from producing gameplay loot when an
/// entity is being removed for lifecycle cleanup rather than because it died.
/// </summary>
public sealed class CMUDroneDeletionGuardSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CMUDroneDeletionGuardComponent, EntityTerminatingEvent>(
            OnDroneTerminating,
            before: [typeof(CMUDroneOperatorSystem)]);
    }

    private void OnDroneTerminating(Entity<CMUDroneDeletionGuardComponent> ent, ref EntityTerminatingEvent args)
    {
        if (!_mobState.IsDead(ent.Owner) &&
            TryComp<CMUDroneAndroidComponent>(ent.Owner, out var drone))
        {
            drone.RuinedCoreSpawned = true;
        }
    }
}
