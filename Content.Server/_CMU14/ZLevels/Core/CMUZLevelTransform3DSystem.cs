using Content.Shared._CMU14.ZLevels.Core;
using Content.Shared._CMU14.ZLevels.Core.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server._CMU14.ZLevels.Core;

/// <summary>
/// Projects CMU's existing map-per-floor z-level network into the transitional Robust Transform3D hierarchy.
/// The legacy maps remain authoritative for gameplay while each map root receives a physical world Z.
/// </summary>
public sealed class CMUZLevelTransform3DSystem : EntitySystem
{
    [Dependency] private SharedTransform3DSystem _transform3D = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CMUZLevelMapComponent, ComponentStartup>(OnMapStartup);
        SubscribeLocalEvent<CMUZLevelNetworkUpdatedEvent>(OnNetworkUpdated);
    }

    private void OnMapStartup(Entity<CMUZLevelMapComponent> entity, ref ComponentStartup args)
    {
        ApplyDepth(entity.Owner, entity.Comp.Depth);
    }

    private void OnNetworkUpdated(ref CMUZLevelNetworkUpdatedEvent args)
    {
        foreach (var (depth, mapUid) in args.Network.Comp.ZLevels)
        {
            if (mapUid is not { } map || TerminatingOrDeleted(map))
                continue;

            ApplyDepth(map, depth);
        }
    }

    private void ApplyDepth(EntityUid mapUid, int depth)
    {
        _transform3D.SetWorldZ(
            mapUid,
            depth * CMUWorld3DSpatialConstants.ZLevelSpacing);
    }
}
