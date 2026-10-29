using System.Numerics;
using Content.Client.CMU14.Lighting.Configuration;
using Content.Shared.CMU14.Lighting;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.CMU14.Lighting;

/// <summary>
/// Experimental sprite-normal prepass and visibility-clipped directional highlight.
/// Restricted to CMUCinematicNormalComponent entities, never writes native light data.
/// </summary>
internal sealed class CinematicNormalRenderer : IDisposable
{
    private const int MaximumSources = 8;
    private const int MaximumSprites = 64;
    private readonly IEntityManager _entities;
    private readonly EntityLookupSystem _lookup;
    private readonly SharedTransformSystem _transforms;
    private readonly IResourceCache _textures;
    private readonly CinematicRenderContext _frame;
    private readonly CinematicOcclusionRenderer _occlusion;
    private readonly CinematicLightingQuality _quality;
    private readonly ShaderInstance _capture;
    private readonly CinematicShaderPool _lights;
    private readonly HashSet<Entity<CMUCinematicNormalComponent>> _visible = new();
    private readonly Dictionary<string, RSI?> _normals = new();
    private readonly Action _drawNormals;
    private readonly Action _drawLights;
    private Texture _normalTexture = Texture.White;
    private bool _hasNormals;

    public CinematicNormalRenderer(IEntityManager entities, IResourceCache textures,
        IPrototypeManager prototypes, CinematicRenderContext frame,
        CinematicOcclusionRenderer occlusion, CinematicLightingQuality quality)
    {
        _entities = entities;
        _textures = textures;
        _frame = frame;
        _occlusion = occlusion;
        _quality = quality;
        _lookup = entities.System<EntityLookupSystem>();
        _transforms = entities.System<SharedTransformSystem>();
        _capture = prototypes.Index<ShaderPrototype>("CMUCinematicNormalCapture").InstanceUnique();
        _lights = new CinematicShaderPool(prototypes.Index<ShaderPrototype>("CMUCinematicNormalLight"), MaximumSources);
        _drawNormals = DrawNormals;
        _drawLights = DrawLights;
    }

    public void Render(IRenderTexture normals, IRenderTexture light)
    {
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
        // A missing normal is not fatal: most content has no generated map yet.
        _normals.Add(source, result);
        return result;
    }

    private void DrawNormals()
    {
        _hasNormals = false;
        _visible.Clear();
        _lookup.GetEntitiesIntersecting(_frame.Map, _frame.Bounds.CalcBoundingBox(), _visible);
        var eyeRotation = _frame.Viewport.Eye!.Rotation;
        var count = 0;
        foreach (var entity in _visible)
        {
            if (count >= MaximumSprites)
                break;
            if (!entity.Comp.Enabled || entity.Comp.Strength <= 0f
                || !_entities.TryGetComponent<TransformComponent>(entity, out var xform)
                || !_entities.TryGetComponent<SpriteComponent>(entity, out var sprite)
                || !Visible(entity, xform) || sprite.AllLayers is not IReadOnlyList<ISpriteLayer> layers)
                continue;

            var (position, rotation) = _transforms.GetWorldPositionRotation(xform);
            var angle = (rotation + eyeRotation).Reduced().FlipPositive();
            var entityMatrix = Matrix3Helpers.CreateTransform(position,
                CinematicLightingGeometry.RenderedRotation(rotation, eyeRotation,
                    sprite.NoRotation, sprite.SnapCardinals));
            var spriteMatrix = sprite.LocalMatrix * entityMatrix;

            for (var i = 0; i < Math.Min(layers.Count, 4); i++)
            {
                if (layers[i] is not SpriteComponent.Layer layer || !layer.Visible || layer.Color.A <= 0.01f
                    || layer.ActualRsi?.TryGetState(layers[i].RsiState, out var state) != true)
                    continue;
                var normalRsi = ResolveNormalRsi(layer.ActualRsi);
                if (normalRsi == null || !normalRsi.TryGetState(layers[i].RsiState, out var normalState))
                    continue;

                var direction = layer.EffectiveDirection(state, angle,
                    sprite.EnableDirectionOverride ? sprite.DirectionOverride : null);
                if (layer.AnimationFrame >= normalState.AnimationFrameCount)
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
                _capture.SetParameter("normal_strength", Math.Clamp(entity.Comp.Strength, 0f, 1f));
                _frame.SetWorldTransform(matrix);
                _frame.Handle.UseShader(_capture);
                var size = texture.Size / (float) EyeManager.PixelsPerMeter;
                _frame.Handle.DrawTextureRectRegion(texture,
                    Box2.CenteredAround(Vector2.Zero, size), Color.White,
                    new UIBox2(0f, 0f, texture.Width, texture.Height));
                _hasNormals = true;
            }
            count++;
        }
        _frame.SetWorldTransform(Matrix3x2.Identity);
    }

    private void DrawLights()
    {
        if (!_hasNormals)
            return;

        for (var index = 0; index < Math.Min(_frame.Sources.Count, MaximumSources); index++)
        {
            var source = _frame.Sources[index];
            var mesh = _occlusion.GetMesh(source, _quality);
            if (!mesh.Valid)
                continue;

            var shader = _lights.Get(index);
            _frame.BindFrame(shader);
            _frame.BindSource(shader, source);
            shader.SetParameter("normal_texture", _normalTexture);
            shader.SetParameter("normal_light_strength", 0.4f);
            _frame.Handle.UseShader(shader);
            _occlusion.DrawMesh(mesh);
        }
    }

    public void Dispose()
    {
        _capture.Dispose();
        _lights.Dispose();
        _normals.Clear();
    }
}
