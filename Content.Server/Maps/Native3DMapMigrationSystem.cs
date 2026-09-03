using System.Collections.Generic;
using System.Numerics;
using Content.Server.Maps.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Physics3D;

namespace Content.Server.Maps;

/// <summary>
/// Imports existing station maps into MapGrid3D at map initialization. The legacy tile payload is read once as
/// source content; after import all subsequent edits are mirrored into the volumetric grid until map files are
/// rewritten natively by the content migration tool.
/// </summary>
public sealed class Native3DMapMigrationSystem : EntitySystem
{
    [Dependency] private SharedMapSystem _maps = default!;
    [Dependency] private SharedMapGrid3DSystem _grids3D = default!;
    [Dependency] private SharedTransform3DSystem _transforms3D = default!;
    [Dependency] private SharedPhysicsSystem _physics2D = default!;
    [Dependency] private Native3DEntityMigrationSystem _entities3D = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MapGridComponent, MapInitEvent>(OnGridMapInit);
        SubscribeLocalEvent<TileChangedEvent>(OnTileChanged);
    }

    private void OnGridMapInit(Entity<MapGridComponent> entity, ref MapInitEvent args)
    {
        _transforms3D.SetAuthoritative(entity.Owner, true);
        EnsureComp<LegacyPhysics3DBridgeComponent>(entity.Owner);
        if (TryComp(entity.Owner, out PhysicsComponent? legacyBody))
            _physics2D.SetCanCollide(entity.Owner, false, body: legacyBody);

        if (TryComp(entity.Owner, out Native3DMigratedGridComponent? marker) &&
            marker.Version == Native3DMigratedGridComponent.CurrentVersion &&
            HasComp<MapGrid3DComponent>(entity.Owner))
            return;

        var grid3D = EnsureComp<MapGrid3DComponent>(entity.Owner);
        grid3D.CellSize = entity.Comp.TileSize;
        var edits = new List<(Vector3i Indices, Voxel3D Voxel)>();
        var tiles = _maps.GetAllTilesEnumerator(entity.Owner, entity.Comp, ignoreEmpty: true);
        while (tiles.MoveNext(out var tile))
        {
            edits.Add((
                new Vector3i(tile.GridIndices.X, tile.GridIndices.Y, -1),
                ConvertTile(tile.Tile)));
        }

        _grids3D.SetVoxels((entity.Owner, grid3D), edits);
        marker = EnsureComp<Native3DMigratedGridComponent>(entity.Owner);
        marker.Version = Native3DMigratedGridComponent.CurrentVersion;
        _entities3D.PromoteGrid(entity.Owner);
    }

    private void OnTileChanged(ref TileChangedEvent args)
    {
        if (!TryComp(args.Entity.Owner, out MapGrid3DComponent? grid3D))
            return;

        var edits = new List<(Vector3i Indices, Voxel3D Voxel)>(args.Changes.Length);
        foreach (var change in args.Changes)
        {
            edits.Add((
                new Vector3i(change.GridIndices.X, change.GridIndices.Y, -1),
                ConvertTile(change.NewTile)));
        }
        _grids3D.SetVoxels((args.Entity.Owner, grid3D), edits);
    }

    private static Voxel3D ConvertTile(Tile tile)
    {
        return tile.IsEmpty
            ? Voxel3D.Empty
            : new Voxel3D(
                tile.TypeId,
                VoxelFlags3D.DefaultStructure,
                tile.Variant,
                tile.RotationMirroring);
    }
}
