using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Spawners;

namespace Content.Server.Imperial.Medieval.Magic.SpawnEntityOnNullSpaceTeleport;



public sealed partial class SpawnEntityOnNullSpaceTeleportSystem : EntitySystem
{
    [Dependency] private readonly TransformSystem _transformSystem = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpawnEntityOnNullSpaceTeleportComponent, MoveEvent>(OnRemove);
    }

    private void OnRemove(EntityUid uid, SpawnEntityOnNullSpaceTeleportComponent component, MoveEvent args)
    {
        if (args.NewPosition.IsValid(EntityManager)) return;
        if (string.IsNullOrEmpty(component.Spawn)) return;

        Spawn(component.Spawn, _transformSystem.ToMapCoordinates(args.OldPosition));
    }
}
