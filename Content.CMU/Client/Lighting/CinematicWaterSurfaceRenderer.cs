using System.Numerics;
using Content.Shared.CMU14.Lighting;
using Content.Shared._RMC14.Water;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client.CMU14.Lighting;

/// <summary>Actual floor-water sprite coverage and its late, below-actor material response.</summary>
internal sealed class CinematicWaterSurfaceRenderer : IDisposable
{
    private const int MaximumLayers = 4096;
    private readonly IEntityManager _entities;
    private readonly SharedTransformSystem _transform;
    private readonly EntityLookupSystem _lookup;
    private readonly CinematicRenderContext _frame;
    private readonly CinematicMaterialRenderer _materials;
    private readonly HashSet<Entity<RMCWaterComponent>> _water = new();
    private readonly List<Layer> _layers = new(256);
    private readonly HashSet<EntityUid> _receivers = new();
    private readonly ShaderInstance _mask;
    private readonly ShaderInstance _reflection;
    private float _maskOpacity = -1f;
    private float _reflectionOpacity = -1f;

    public CinematicWaterSurfaceRenderer(IEntityManager entities, IPrototypeManager prototypes,
        CinematicRenderContext frame, CinematicMaterialRenderer materials)
    {
        _entities = entities;
        _transform = entities.System<SharedTransformSystem>();
        _lookup = entities.System<EntityLookupSystem>();
        _frame = frame;
        _materials = materials;
        _mask = prototypes.Index<ShaderPrototype>("CMUCinematicWaterMaterial").InstanceUnique();
        _reflection = prototypes.Index<ShaderPrototype>("CMUCinematicWaterReflection").InstanceUnique();
        var wet = prototypes.Index<CMUCinematicMaterialPrototype>("CMUWetFloor");
        var data = new Vector3(Math.Clamp(wet.Reflectivity * wet.SpecularStrength / 4f, 0f, 1f),
            wet.Roughness, wet.Stretch);
        _mask.SetParameter("water_material_data", data);
    }

    internal int LayerCount => _layers.Count;
    internal bool ContainsReceiver(EntityUid uid) => _receivers.Contains(uid);

    // Called inside the existing material target, after opaque tiles and puddles.
    public void DrawMask()
    {
        _water.Clear();
        _layers.Clear();
        _receivers.Clear();
        _lookup.GetEntitiesIntersecting(_frame.Map, _frame.Bounds.CalcBoundingBox(), _water);
        var eyeRotation = _frame.Viewport.Eye!.Rotation;
        foreach (var entity in _water)
        {
            if (!_entities.TryGetComponent<TransformComponent>(entity, out var xform)
                || !_materials.VisibleSurface(entity, xform)
                || !_entities.TryGetComponent<SpriteComponent>(entity, out var sprite)
                || sprite.DrawDepth != (int) DrawDepth.BelowFloor
                || sprite.AllLayers is not IReadOnlyList<ISpriteLayer> layers)
                continue;
            var rotation = _transform.GetWorldRotation(xform);
            var angle = (rotation + eyeRotation).Reduced().FlipPositive();
            var entityMatrix = Matrix3Helpers.CreateTransform(_transform.GetWorldPosition(xform),
                CinematicLightingGeometry.RenderedRotation(rotation, eyeRotation, sprite.NoRotation, sprite.SnapCardinals));
            var spriteMatrix = sprite.LocalMatrix * entityMatrix;
            var firstLayer = _layers.Count;
            for (var i = 0; i < Math.Min(layers.Count, 4) && _layers.Count < MaximumLayers; i++)
            {
                if (layers[i] is not SpriteComponent.Layer layer || !layer.Visible)
                    continue;
                var opacity = sprite.Color.A * layer.Color.A;
                if (opacity <= 0.01f)
                    continue;
                var direction = layer.EffectiveDirection(angle);
                Texture? texture = layer.Texture;
                if (layer.ActualRsi?.TryGetState(layers[i].RsiState, out var state) == true)
                {
                    direction = layer.EffectiveDirection(state, angle,
                        sprite.EnableDirectionOverride ? sprite.DirectionOverride : null);
                    texture = state.GetFrame(direction, layers[i].AnimationFrame);
                }
                if (texture == null)
                    continue;
                layer.GetLayerDrawMatrix(direction, out var local);
                _layers.Add(new Layer(texture, Box2.CenteredAround(Vector2.Zero, texture.Size / (float) EyeManager.PixelsPerMeter),
                    local * spriteMatrix, opacity));
            }
            if (_layers.Count != firstLayer)
                _receivers.Add(entity.Owner);
            if (_layers.Count == MaximumLayers)
                break;
        }
        _frame.Handle.UseShader(_mask);
        foreach (var layer in _layers)
        {
            _frame.SetWorldTransform(layer.Matrix);
            if (_maskOpacity != layer.Opacity)
            {
                _maskOpacity = layer.Opacity;
                _mask.SetParameter("receiver_opacity", layer.Opacity);
            }
            Draw(_frame.Handle, layer);
        }
        _frame.SetWorldTransform(Matrix3x2.Identity);
    }

    public void DrawReflections(in OverlayDrawArgs args, Texture reflections, Texture blockers, float exposure)
    {
        if (_layers.Count == 0)
            return;
        _frame.BindFrame(_reflection);
        _reflection.SetParameter("target_size", (Vector2) args.Viewport.Size);
        BindResponse(_reflection, reflections, blockers, exposure);
        _reflection.SetParameter("native_light_texture", args.Viewport.LightRenderTarget.Texture);
        args.WorldHandle.UseShader(_reflection);
        foreach (var layer in _layers)
        {
            args.WorldHandle.SetTransform(layer.Matrix);
            if (_reflectionOpacity != layer.Opacity)
            {
                _reflectionOpacity = layer.Opacity;
                _reflection.SetParameter("receiver_opacity", layer.Opacity);
            }
            Draw(args.WorldHandle, layer);
        }
        args.WorldHandle.SetTransform(Matrix3x2.Identity);
        args.WorldHandle.UseShader(null);
    }

    public void BindResponse(ShaderInstance shader, Texture reflections, Texture blockers, float exposure)
    {
        shader.SetParameter("water_reflection_texture", reflections);
        shader.SetParameter("blocker_texture", blockers);
        shader.SetParameter("local_exposure", exposure);
    }

    private static void Draw(DrawingHandleWorld handle, in Layer layer) =>
        handle.DrawTextureRectRegion(layer.Texture, layer.Bounds, Color.White,
            new UIBox2(0, 0, layer.Texture.Width, layer.Texture.Height));

    public void Dispose()
    {
        _mask.Dispose();
        _reflection.Dispose();
    }

    private readonly record struct Layer(Texture Texture, Box2 Bounds, Matrix3x2 Matrix, float Opacity);
}
