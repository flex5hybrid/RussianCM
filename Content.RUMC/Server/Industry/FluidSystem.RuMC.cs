using Content.Shared.CMU14.Fluids;
using Content.Shared.CMU14.ZLevels.Core.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.CMU14.Fluids;

// RuMC: the liquid pipes going up. A pipe going up joins the pipe in the same place on the level above, as a pipe going down joins
// the one below; a pipe going down over a pipe going up is one joint, not two.
public sealed partial class FluidSystem
{
    // Called after the pipes going down are joined.
    private void RuMCJoinPipesGoingUp()
    {
        var query = EntityQueryEnumerator<FluidPipeComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var pipe, out var xform))
        {
            if (!pipe.Up || pipe.Isolated || !xform.Anchored || xform.MapUid is not { } map)
                continue;

            foreach (var above in RuMCPipesAbove(uid, map))
            {
                if (!_pipeQuery.TryComp(above, out var upper) || upper.Isolated || pipe.Neighbours.Contains(above))
                    continue;

                pipe.Neighbours.Add(above);
                upper.Neighbours.Add(uid);
            }
        }
    }

    // The anchored pipes in the same place on the level over the map. The level above a cave may have a grid of its own on it (the
    // colony's), so the grid is looked for where the pipe is; a dug level is a map that is a grid itself.
    private IEnumerable<EntityUid> RuMCPipesAbove(EntityUid uid, EntityUid map)
    {
        if (!_zLevels.TryMapUp(new Entity<CMUZLevelMapComponent?>(map, null), out var above))
            yield break;

        var levelUid = above.Value.Owner;
        var world = _transform.GetWorldPosition(uid);
        var gridUid = levelUid;
        if (TryComp<MapComponent>(levelUid, out var levelMap)
            && _map.TryFindGridAt(new MapCoordinates(world, levelMap.MapId), out var found, out _))
            gridUid = found;

        if (!TryComp<MapGridComponent>(gridUid, out var grid))
            yield break;

        var tile = _map.WorldToTile(gridUid, grid, world);
        var anchored = _map.GetAnchoredEntitiesEnumerator(gridUid, grid, tile);
        while (anchored.MoveNext(out var other))
        {
            if (_pipeQuery.HasComp(other.Value))
                yield return other.Value;
        }
    }
}
