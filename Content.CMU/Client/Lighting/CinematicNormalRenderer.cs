using System.Numerics;
using Content.Client.CMU14.Lighting.Configuration;
using Content.Shared.CMU14.Lighting;
using Robust.Shared.Configuration;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.CMU14.Lighting;

/// <summary>
/// Experimental sprite-normal prepass and visibility-clipped directional highlight.
/// Selects authored opt-in sprites or a capped set of visible sprites in mass-test mode.
/// Never modifies native light buffers or gameplay visibility.
/// </summary>
internal sealed class CinematicNormalRenderer : IDisposable
{
    private const int OptInSources = 8;
    private const int MassSources = 16;
    private const int OptInSprites = 64;
    private const int MassSprites = 256;
    private readonly IEntityManager _entities;
    private readonly EntityLookupSystem _lookup;
    private readonly SharedTransformSystem _transforms;
    private readonly IResourceCache _textures;
    private readonly CinematicRenderContext _frame;
    private readonly CinematicOcclusionRenderer _occlusion;
    private readonly CinematicLightingQuality _quality;
    private readonly IConfigurationManager _config;
    private readonly ShaderInstance _capture;
    private readonly CinematicShaderPool _lights;
    private readonly HashSet<Entity<CMUCinematicNormalComponent>> _visible = new();
    private readonly HashSet<Entity<SpriteComponent>> _visibleSprites = new();
    private readonly List<NormalCandidate> _candidates = new(MassSprites);
    private readonly Dictionary<string, RSI?> _normals = new();
    private bool _lastMassMode;
    private readonly Action _drawNormals;
    private readonly Action _drawLights;
    private Texture _normalTexture = Texture.White;
    private bool _hasNormals;
    public int EligibleSprites { get; private set; }
    public int RenderedSprites { get; private set; }
    public int RenderedLayers { get; private set; }

    public CinematicNormalRenderer(IEntityManager entities, IResourceCache textures,
        IPrototypeManager prototypes, CinematicRenderContext frame,
        CinematicOcclusionRenderer occlusion, CinematicLightingQuality quality,
        IConfigurationManager config)
    {
        _entities = entities;
        _textures = textures;
        _frame = frame;
        _occlusion = occlusion;
        _quality = quality;
        _config = config;
        _lookup = entities.System<EntityLookupSystem>();
        _transforms = entities.System<SharedTransformSystem>();
        _capture = prototypes.Index<ShaderPrototype>("CMUCinematicNormalCapture").InstanceUnique();
        _lights = new CinematicShaderPool(prototypes.Index<ShaderPrototype>("CMUCinematicNormalLight"), MassSources);
        _drawNormals = DrawNormals;
        _drawLights = DrawLights;
    }

    public void Render(IRenderTexture normals, IRenderTexture light, bool massMode)
    {
        // A mode switch is also a resource-reload point for previously missing normal
        // RSI assets added during a local test. Negative cache entries are per renderer.
        if (_lastMassMode != massMode)
        {
            _lastMassMode = massMode;
            _normals.Clear();
        }
        _frame.Render(normals, _drawNormals, CinematicRenderContext.ClearColor);
        _normalTexture = normals.Texture;
        // Clear even when no eligible sprite is visible, to prevent a stale frame
        // from lighting the scene after a demo object moves out of view.
        _frame.Render(light, _drawLights, CinematicRenderContext.RadianceClearColor);
    }

    private bool Visible(EntityUid uid, TransformComponent xform)
    {
        if (xform.MapID != _frame.Map)
            return false;

        var current = uid;
        for (var depth = 0; depth < 16; depth++)
        {
            if (_entities.TryGetComponent<MetaDataComponent>(current, out var metadata)
                && metadata.VisibilityMask != 1)
                return false;
            if (_entities.TryGetComponent<SpriteComponent>(current, out var parentSprite)
                && (!parentSprite.Visible || parentSprite.ContainerOccluded || parentSprite.Color.A < 0.01f))
                return false;
            if (!_entities.TryGetComponent<TransformComponent>(current, out var parent)
                || parent.ParentUid == EntityUid.Invalid || parent.ParentUid == xform.GridUid
                || parent.ParentUid == xform.MapUid)
                return true;
            current = parent.ParentUid;
        }
        return false;
    }

    private RSI? ResolveNormalRsi(RSI original)
    {
        var source = original.Path.ToString();
        if (_normals.TryGetValue(source, out var cached))
            return cached;

        RSI? result = null;
        if (source.StartsWith("/Textures/", StringComparison.Ordinal))
        {
            var path = new ResPath("/Textures/_GeneratedNormalMaps/" + source["/Textures/".Length..]);
            if (_textures.TryGetResource<RSIResource>(path, out var normal))
                result = normal.RSI;
        }
        // Most existing RSI assets have no generated normals. Cache misses too:
        // otherwise every visible sprite could hit the filesystem every frame.
        // Changing the mass-mode CVar clears this cache for local asset reloads.
        _normals.Add(source, result);
        return result;
    }

    private readonly record struct NormalCandidate(
        EntityUid Uid, SpriteComponent Sprite, TransformComponent Xform,
        float Strength, Vector2 Position, float DistanceSquared, bool Explicit);

    private void AddCandidate(EntityUid uid, SpriteComponent sprite,
        float strength, bool explicitOptIn, Vector2 eyePosition)
    {
        if (strength <= 0f || !sprite.Visible || sprite.ContainerOccluded || sprite.Color.A <= 0.01f
            || !_entities.TryGetComponent<TransformComponent>(uid, out var xform)
            || xform.MapID != _frame.Map
            || sprite.AllLayers is not IReadOnlyList<ISpriteLayer> layers)
            return;

        // Most sprites are unmodified upstream assets. Resolve at most four visible
        // RSI layers first, and never include entities without an available normal RSI.
        var hasNormals = false;
        for (var layerIndex = 0; layerIndex < Math.Min(layers.Count, 4); layerIndex++)
        {
            if (layers[layerIndex] is not SpriteComponent.Layer layer
                || !layer.Visible || layer.Color.A <= 0.01f || layer.ActualRsi == null)
                continue;
            if (ResolveNormalRsi(layer.ActualRsi) is { } mapped
                && mapped.TryGetState(layers[layerIndex].RsiState, out _))
            {
                hasNormals = true;
                break;
            }
        }
        if (!hasNormals || !Visible(uid, xform))
            return;

        var (position, _) = _transforms.GetWorldPositionRotation(xform);
        _candidates.Add(new NormalCandidate(uid, sprite, xform, strength, position,
            Vector2.DistanceSquared(position, eyePosition), explicitOptIn));
    }

    private void GatherCandidates()
    {
        _candidates.Clear();
        var eyePosition = _frame.Viewport.Eye!.Position.Position + _frame.Viewport.Eye.Offset;
        var bounds = _frame.Bounds.CalcBoundingBox();
        if (_lastMassMode)
        {
            // Spatially query SpriteComponent, not every entity on the map.
            _visibleSprites.Clear();
            _lookup.GetEntitiesIntersecting(_frame.Map, bounds, _visibleSprites);
            foreach (var entity in _visibleSprites)
            {
                var optIn = _entities.TryGetComponent<CMUCinematicNormalComponent>(
                    entity.Owner, out var marker);
                if (optIn && !marker!.Enabled)
                    continue;
                AddCandidate(entity.Owner, entity.Comp,
                    optIn ? Math.Clamp(marker!.Strength, 0f, 1f) : 0.65f,
                    optIn, eyePosition);
            }
        }
        else
        {
            _visible.Clear();
            _lookup.GetEntitiesIntersecting(_frame.Map, bounds, _visible);
            foreach (var entity in _visible)
            {
                if (!entity.Comp.Enabled
                    || !_entities.TryGetComponent<SpriteComponent>(entity.Owner, out var sprite))
                    continue;
                AddCandidate(entity.Owner, sprite, Math.Clamp(entity.Comp.Strength, 0f, 1f),
                    true, eyePosition);
            }
        }

        // Fixed-cost per-frame budget. Explicitly opted-in test objects win ties;
        // nearby objects are chosen deterministically instead of HashSet iteration order.
        _candidates.Sort(static (a, b) =>
        {
            var cmp = b.Explicit.CompareTo(a.Explicit);
            if (cmp != 0)
                return cmp;
            cmp = a.DistanceSquared.CompareTo(b.DistanceSquared);
            return cmp != 0 ? cmp : a.Uid.Id.CompareTo(b.Uid.Id);
        });
        var budget = _lastMassMode ? MassSprites : OptInSprites;
        if (_candidates.Count > budget)
            _candidates.RemoveRange(budget, _candidates.Count - budget);
        EligibleSprites = _candidates.Count;

        // Approximate native sprite sorting; the engine additionally sorts by
        // screen-space bounding-box top, which cannot be reconstructed reliably
        // for every special renderer here. Keep this explicitly experimental.
        _candidates.Sort(static (a, b) =>
        {
            var cmp = a.Sprite.DrawDepth.CompareTo(b.Sprite.DrawDepth);
            if (cmp != 0)
                return cmp;
            cmp = a.Sprite.RenderOrder.CompareTo(b.Sprite.RenderOrder);
            if (cmp != 0)
                return cmp;
            cmp = b.Position.Y.CompareTo(a.Position.Y);
            return cmp != 0 ? cmp : a.Uid.Id.CompareTo(b.Uid.Id);
        });
    }

    private void DrawNormals()
    {
        _hasNormals = false;
        RenderedSprites = 0;
        RenderedLayers = 0;
        GatherCandidates();
        var eyeRotation = _frame.Viewport.Eye!.Rotation;
        foreach (var entity in _candidates)
        {
            var sprite = entity.Sprite;
            var renderedAny = false;
            var (_, rotation) = _transforms.GetWorldPositionRotation(entity.Xform);
            var angle = (rotation + eyeRotation).Reduced().FlipPositive();
            var entityMatrix = Matrix3Helpers.CreateTransform(entity.Position,
                CinematicLightingGeometry.RenderedRotation(rotation, eyeRotation,
                    sprite.NoRotation, sprite.SnapCardinals));
            var spriteMatrix = sprite.LocalMatrix * entityMatrix;

            if (sprite.AllLayers is not IReadOnlyList<ISpriteLayer> layers)
                continue;
            for (var i = 0; i < Math.Min(layers.Count, 4); i++)
            {
                if (layers[i] is not SpriteComponent.Layer layer || !layer.Visible || layer.Color.A <= 0.01f
                    || layer.ActualRsi == null)
                    continue;
                if (!layer.ActualRsi.TryGetState(layers[i].RsiState, out var state))
                    continue;
                var normalRsi = ResolveNormalRsi(layer.ActualRsi);
                if (normalRsi == null || !normalRsi.TryGetState(layers[i].RsiState, out var normalState))
                    continue;

                var direction = layer.EffectiveDirection(state, angle,
                    sprite.EnableDirectionOverride ? sprite.DirectionOverride : null);
                if (layer.AnimationFrame >= normalState.DelayCount)
                    continue;
                var texture = normalState.GetFrame(direction, layer.AnimationFrame);
                layer.GetLayerDrawMatrix(direction, out var layerMatrix);
                var matrix = layerMatrix * spriteMatrix;
                var x = Vector2.TransformNormal(Vector2.UnitX, matrix);
                var y = Vector2.TransformNormal(Vector2.UnitY, matrix);
                if (x.LengthSquared() < 0.0001f || y.LengthSquared() < 0.0001f)
                    continue;
                _capture.SetParameter("normal_basis_x", Vector2.Normalize(x));
                _capture.SetParameter("normal_basis_y", Vector2.Normalize(y));
                _capture.SetParameter("normal_strength", entity.Strength);
                _frame.SetWorldTransform(matrix);
                _frame.Handle.UseShader(_capture);
                var size = texture.Size / (float) EyeManager.PixelsPerMeter;
                _frame.Handle.DrawTextureRectRegion(texture,
                    Box2.CenteredAround(Vector2.Zero, size), Color.White,
                    new UIBox2(0f, 0f, texture.Width, texture.Height));
                _hasNormals = true;
                RenderedLayers++;
                renderedAny = true;
            }
            if (renderedAny)
                RenderedSprites++;
        }
        _frame.SetWorldTransform(Matrix3x2.Identity);
    }

    private void DrawLights()
    {
        if (!_hasNormals)
            return;

        // Read CVars once per light pass; these take effect without client recompilation.
        // Clamp test inputs so extreme values cannot destabilize the HDR contribution.
        var gain = ClampFinite(_config.GetCVar(CMUCinematicNormalCVars.Gain), 0f, 3f, 1f);
        var height = ClampFinite(_config.GetCVar(CMUCinematicNormalCVars.Height), 0.05f, 3f, 0.45f);
        var contrast = ClampFinite(_config.GetCVar(CMUCinematicNormalCVars.Contrast), 0f, 6f, 3f);
        var flatness = ClampFinite(_config.GetCVar(CMUCinematicNormalCVars.Flatness), 0f, 1f, 0.7f);
        var maxBoost = ClampFinite(_config.GetCVar(CMUCinematicNormalCVars.MaxBoost), 0f, 2f, 1f);
        if (gain <= 0f || contrast <= 0f || maxBoost <= 0f)
            return;

        for (var index = 0; index < Math.Min(_frame.Sources.Count, _lastMassMode ? MassSources : OptInSources); index++)
        {
            var source = _frame.Sources[index];
            var mesh = _occlusion.GetMesh(source, _quality);
            if (!mesh.Valid)
                continue;

            var shader = _lights.Get(index);
            _frame.BindFrame(shader);
            _frame.BindSource(shader, source);
            shader.SetParameter("normal_texture", _normalTexture);
            shader.SetParameter("normal_light_strength", gain);
            shader.SetParameter("normal_light_height", height);
            shader.SetParameter("normal_light_contrast", contrast);
            shader.SetParameter("normal_light_flatness", flatness);
            shader.SetParameter("normal_light_max_boost", maxBoost);
            _frame.Handle.UseShader(shader);
            _occlusion.DrawMesh(mesh);
        }
    }

    private static float ClampFinite(float value, float min, float max, float fallback) =>
        float.IsFinite(value) ? Math.Clamp(value, min, max) : fallback;

    public void Dispose()
    {
        _capture.Dispose();
        _lights.Dispose();
        _normals.Clear();
    }
}
