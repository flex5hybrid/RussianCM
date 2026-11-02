using Content.Shared.CMU14.ZLevels.Core;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server._RuMC14.Decals;

/// <summary>
/// Tells whether a decal has a floor to lie on, so blood and scorch from shots across Z-levels don't float over holes.
/// </summary>
public sealed class RuMCDecalFloorSystem : EntitySystem
{
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private ITileDefinitionManager _tileDefinitions = default!;

    public bool HasFloor(EntityCoordinates coordinates)
    {
        return HasFloor(_transform.ToMapCoordinates(coordinates));
    }

    public bool HasFloor(MapCoordinates coordinates)
    {
        return _map.TryFindGridAt(coordinates, out var gridUid, out var grid) &&
               HasFloor(gridUid, grid, _map.WorldToTile(gridUid, grid, coordinates.Position));
    }

    public bool HasFloor(EntityUid gridUid, MapGridComponent grid, Vector2i indices)
    {
        return _map.TryGetTileRef(gridUid, grid, indices, out var tileRef) &&
               !CMUZLevelOpeningCache.IsFallThroughTile(tileRef.Tile, _tileDefinitions);
    }
}
