using System.Numerics;
using System.Runtime.InteropServices;
using Content.Shared.CMU14.Lighting;
using Content.Shared.Fluids.Components;
using Content.Shared.Maps;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.CMU14.Lighting;

/// <summary>Content material mask, including inherited tile definitions and actual wet-floor puddles.</summary>
internal sealed class CinematicMaterialRenderer
{
    private readonly IEntityManager _entities;
    private readonly SharedMapSystem _map;
    private readonly SharedTransformSystem _transform;
    private readonly EntityLookupSystem _lookup;
    private readonly ITileDefinitionManager _tiles;
    private readonly IPrototypeManager _prototypes;
    private readonly CinematicRenderContext _frame;
    private readonly ShaderInstance _unshaded;
    private readonly Action _draw;
    private readonly Dictionary<int, Color> _tileColors = new();
    private List<Entity<MapGridComponent>> _grids = new();
    private readonly List<WorldRect> _rects = new(2048);
    private readonly HashSet<Entity<CMUCinematicMaterialComponent>> _surfaces = new();
    private readonly HashSet<Entity<PuddleComponent>> _puddles = new();
    private readonly HashSet<(EntityUid Grid, Vector2i Tile)> _wetTiles = new();
    private readonly List<DrawVertexUV2DColor> _wetVertices = new(1024);
    private readonly DrawVertexUV2DColor[] _wetTileVertices = new DrawVertexUV2DColor[16];
    private static readonly float[] WetSteps = [0f, 0.18f, 0.82f, 1f];
    private readonly Texture _white = Texture.White;
    private static readonly Color Ordinary = Encode(0f, 1f, 0f);

    public CinematicMaterialRenderer(IEntityManager entities, IPrototypeManager prototypes,
        ITileDefinitionManager tiles, CinematicRenderContext frame)
    {
        _entities = entities;
        _prototypes = prototypes;
        _tiles = tiles;
        _frame = frame;
        _map = entities.System<SharedMapSystem>();
        _transform = entities.System<SharedTransformSystem>();
        _lookup = entities.System<EntityLookupSystem>();
        _unshaded = prototypes.Index<ShaderPrototype>("unshaded").Instance();
        _draw = Draw;
    }

    public void Render(IRenderTexture target) => _frame.Render(target, _draw, CinematicRenderContext.ClearColor);

    private void Draw()
    {
        _frame.Handle.UseShader(_unshaded);
        _grids.Clear();
        _map.FindGridsIntersecting(_frame.Map, _frame.Bounds, ref _grids);
        foreach (var grid in _grids)
        {
            _frame.SetWorldTransform(_transform.GetWorldMatrix(grid.Owner));
            _rects.Clear();
            var tiles = _map.GetTilesIntersecting(grid.Owner, grid, _frame.Bounds);
            while (tiles.MoveNext(out var tile))
            {
                var definition = (ContentTileDefinition) _tiles[tile.Tile.TypeId];
                if (tile.Tile.IsEmpty || definition.CMUFallThrough)
                    continue;
                if (!_tileColors.TryGetValue(tile.Tile.TypeId, out var color))
                {
                    color = FindMaterial(definition);
                    _tileColors.Add(tile.Tile.TypeId, color);
                }
                _rects.Add(new WorldRect(_lookup.GetLocalBounds(tile, grid.Comp.TileSize), color));
            }
            _frame.Handle.DrawRectsUnmodulated(CollectionsMarshal.AsSpan(_rects));
        }

        _surfaces.Clear();
        _lookup.GetEntitiesIntersecting(_frame.Map, _frame.Bounds.CalcBoundingBox(), _surfaces);
        foreach (var entity in _surfaces)
        {
            if (!_prototypes.TryIndex(entity.Comp.Material, out var material))
                continue;
            DrawSurface(entity, entity.Comp.Size, entity.Comp.Offset,
                Encode(material.Reflectivity, material.Roughness, material.Stretch));
        }

        _puddles.Clear();
        _wetTiles.Clear();
        // Include neighbours just outside the camera, so the screen edge never becomes a dry seam.
        _lookup.GetEntitiesIntersecting(_frame.Map, _frame.Bounds.CalcBoundingBox().Enlarged(1f), _puddles);
        foreach (var entity in _puddles)
        {
            if (!_entities.TryGetComponent<TransformComponent>(entity, out var xform)
                || xform.GridUid is not { } grid || !VisibleSurface(entity, xform)
                || !_transform.TryGetGridTilePosition((entity.Owner, xform), out var tile))
                continue;
            _wetTiles.Add((grid, tile));
        }
        var wet = _prototypes.Index<CMUCinematicMaterialPrototype>("CMUWetFloor");
        var data = new Color(wet.Reflectivity, wet.Roughness, wet.Stretch, 1f);
        foreach (var grid in _grids)
        {
            _wetVertices.Clear();
            foreach (var key in _wetTiles)
            {
                if (key.Grid == grid.Owner)
                    AddWetTile(grid.Owner, key.Tile, grid.Comp.TileSize, data);
            }
            if (_wetVertices.Count == 0)
                continue;
            _frame.SetWorldTransform(_transform.GetWorldMatrix(grid.Owner));
            _frame.Handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, _white,
                CollectionsMarshal.AsSpan(_wetVertices));
        }
    }

    private void AddWetTile(EntityUid grid, Vector2i tile, float tileSize, Color data)
    {
        var left = _wetTiles.Contains((grid, tile + new Vector2i(-1, 0)));
        var right = _wetTiles.Contains((grid, tile + new Vector2i(1, 0)));
        var bottom = _wetTiles.Contains((grid, tile + new Vector2i(0, -1)));
        var top = _wetTiles.Contains((grid, tile + new Vector2i(0, 1)));
        // A flat plateau joins actual neighbouring wet cells. Only the exposed pool perimeter
        // fades out; separate circular footprints would turn a reflected strip into bright dots.
        var steps = WetSteps;
        var vertices = _wetTileVertices;
        for (var y = 0; y < 4; y++)
        for (var x = 0; x < 4; x++)
        {
            var alpha = x == 0 && !left || x == 3 && !right || y == 0 && !bottom || y == 3 && !top ? 0f : 1f;
            vertices[y * 4 + x] = new DrawVertexUV2DColor(
                (new Vector2(tile.X, tile.Y) + new Vector2(steps[x], steps[y])) * tileSize,
                data.WithAlpha(alpha));
        }
        for (var y = 0; y < 3; y++)
        for (var x = 0; x < 3; x++)
        {
            var a = y * 4 + x;
            _wetVertices.Add(vertices[a]);
            _wetVertices.Add(vertices[a + 1]);
            _wetVertices.Add(vertices[a + 5]);
            _wetVertices.Add(vertices[a]);
            _wetVertices.Add(vertices[a + 5]);
            _wetVertices.Add(vertices[a + 4]);
        }
    }

    internal bool VisibleSurface(EntityUid uid, TransformComponent xform)
    {
        if (xform.MapID != _frame.Map)
            return false;
        var current = uid;
        for (var depth = 0; depth < 16; depth++)
        {
            if (_entities.TryGetComponent<MetaDataComponent>(current, out var metadata) && metadata.VisibilityMask != 1
                || _entities.TryGetComponent<SpriteComponent>(current, out var sprite)
                && (!sprite.Visible || sprite.ContainerOccluded || sprite.Color.A <= 0.01f))
                return false;
            if (!_entities.TryGetComponent<TransformComponent>(current, out var parent)
                || parent.ParentUid == EntityUid.Invalid || parent.ParentUid == xform.GridUid
                || parent.ParentUid == xform.MapUid)
                return true;
            current = parent.ParentUid;
        }
        return false;
    }

    private void DrawSurface(EntityUid uid, Vector2 size, Vector2 offset, Color color)
    {
        if (!_entities.TryGetComponent<TransformComponent>(uid, out var xform) || !VisibleSurface(uid, xform))
            return;
        _frame.SetWorldTransform(_transform.GetWorldMatrix(xform));
        _frame.Handle.DrawRect(Box2.CenteredAround(offset, size), color);
    }

    private Color FindMaterial(ContentTileDefinition tile)
    {
        CMUCinematicMaterialPrototype? best = null;
        foreach (var material in _prototypes.EnumeratePrototypes<CMUCinematicMaterialPrototype>())
        {
            if ((best == null || material.Priority > best.Priority) && Matches(tile, material, 0))
                best = material;
        }
        return best == null ? Ordinary : Encode(best.Reflectivity, best.Roughness, best.Stretch);
    }

    private bool Matches(ContentTileDefinition tile, CMUCinematicMaterialPrototype material, int depth)
    {
        if (material.Tiles.Contains(tile.ID))
            return true;
        if (depth >= 16 || tile.Parents == null)
            return false;
        foreach (var parent in tile.Parents)
        {
            if (material.Tiles.Contains(parent)
                || _prototypes.TryIndex<ContentTileDefinition>(parent, out var parentTile)
                && Matches(parentTile, material, depth + 1))
                return true;
        }
        return false;
    }

    private static Color Encode(float reflectivity, float roughness, float stretch)
    {
        // Draw primitives convert colors from sRGB; undo that conversion for this linear data texture.
        return Color.ToSrgb(new Color(Math.Clamp(reflectivity, 0f, 1f),
            Math.Clamp(roughness, 0f, 1f), Math.Clamp(stretch, 0f, 1f), 1f));
    }
}
