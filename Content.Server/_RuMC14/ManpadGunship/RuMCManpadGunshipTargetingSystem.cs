using System.Numerics;
using Content.Shared._RuMC14.ManpadGunship;
using Content.Shared.CMU14.Dropship.Integrity;
using Content.Shared.CMU14.Dropship.TacticalLand;
using Content.Shared.CMU14.ZLevels.Core;
using Content.Shared.CMU14.ZLevels.Core.Components;
using Content.Shared.CMU14.ZLevels.Core.EntitySystems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server._RuMC14.ManpadGunship;

/// <summary>Picks the gunship an operator can see.</summary>
public sealed partial class RuMCManpadGunshipTargetingSystem : EntitySystem
{
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private ITileDefinitionManager _tileDefinitions = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private CMUSharedZLevelsSystem _zLevels = default!;

    private EntityQuery<CMUZLevelMapComponent> _levelQuery;
    private EntityQuery<MapGridComponent> _gridQuery;
    private EntityQuery<DropshipIntegrityComponent> _integrityQuery;
    private EntityQuery<DropshipTacticalHoverComponent> _hoverQuery;

    public override void Initialize()
    {
        _levelQuery = GetEntityQuery<CMUZLevelMapComponent>();
        _gridQuery = GetEntityQuery<MapGridComponent>();
        _integrityQuery = GetEntityQuery<DropshipIntegrityComponent>();
        _hoverQuery = GetEntityQuery<DropshipTacticalHoverComponent>();
    }

    public bool TryFindTarget(Entity<RuMCManpadGunshipComponent> launcher, out EntityUid target)
    {
        target = default;

        var bestDistance = float.MaxValue;
        var ships = EntityQueryEnumerator<DropshipTacticalHoverComponent>();
        while (ships.MoveNext(out var ship, out _))
        {
            if (!IsTrackable(launcher, ship, out var distance) || distance >= bestDistance)
                continue;

            bestDistance = distance;
            target = ship;
        }

        return bestDistance < float.MaxValue;
    }

    public bool CanTrack(Entity<RuMCManpadGunshipComponent> launcher, EntityUid ship)
    {
        return IsTrackable(launcher, ship, out _);
    }

    public Vector2 GetHullCenter(EntityUid ship)
    {
        var local = _gridQuery.TryComp(ship, out var grid) ? grid.LocalAABB.Center : Vector2.Zero;
        return Vector2.Transform(local, _transform.GetWorldMatrix(ship));
    }

    private bool IsTrackable(
        Entity<RuMCManpadGunshipComponent> launcher,
        EntityUid ship,
        out float distanceSquared)
    {
        distanceSquared = 0f;

        if (!_hoverQuery.HasComp(ship)
            || !_integrityQuery.TryComp(ship, out var integrity)
            || integrity.Crashing
            || integrity.Wrecked)
        {
            return false;
        }

        var launcherXform = Transform(launcher);
        var shipXform = Transform(ship);
        if (launcherXform.MapUid is not { } launcherMap
            || shipXform.MapUid is not { } shipMap
            || !_levelQuery.TryComp(launcherMap, out var launcherLevel)
            || !_levelQuery.TryComp(shipMap, out var shipLevel)
            || launcherLevel.NetworkUid != shipLevel.NetworkUid
            || shipLevel.Depth <= launcherLevel.Depth)
        {
            return false;
        }

        var origin = _transform.GetWorldPosition(launcherXform);
        distanceSquared = Vector2.DistanceSquared(origin, GetHullCenter(ship));
        if (distanceSquared > launcher.Comp.LockRange * launcher.Comp.LockRange)
            return false;

        return IsOpenToSky(launcherMap, launcherLevel.Depth, shipLevel.Depth, origin);
    }

    private bool IsOpenToSky(EntityUid operatorMap, int operatorDepth, int shipDepth, Vector2 position)
    {
        if (!_zLevels.TryGetZNetwork(operatorMap, out var network))
            return false;

        for (var depth = operatorDepth + 1; depth <= shipDepth; depth++)
        {
            if (!_zLevels.TryGetMapAtDepth(network.Value, depth, out var map)
                || !_gridQuery.TryComp(map, out var grid))
            {
                continue;
            }

            if (!CMUZLevelOpeningCache.IsOpeningTile(map, grid, position, _map, _tileDefinitions))
                return false;
        }

        return true;
    }
}
