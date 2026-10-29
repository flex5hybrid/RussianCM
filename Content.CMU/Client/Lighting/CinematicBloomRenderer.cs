using System.Numerics;
using Content.Client.CMU14.Lighting.Configuration;
using Content.Shared.CMU14.Lighting;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client.CMU14.Lighting;

/// <summary>Live sprite masks with independent core, inner and outer optical radiance.</summary>
internal sealed class CinematicBloomRenderer : IDisposable
{
    private readonly IPrototypeManager _prototypes;
    private readonly CinematicRenderContext _frame;
    private readonly CinematicOcclusionRenderer _occlusion;
    private readonly CinematicLightingQuality _quality;
    private readonly CinematicShaderPool _seeds;
    private readonly SeedBinding?[] _bindings;
    private readonly ShaderInstance _downsample;
    private readonly Action _drawSeeds;
    private readonly Action _drawDownsample;
    private Texture _input = default!;
    private Texture _blockers = default!;
    private int _seedPass;
    private float _spacing = 1f;

    public CinematicBloomRenderer(IPrototypeManager prototypes, CinematicRenderContext frame,
        CinematicOcclusionRenderer occlusion, CinematicLightingQuality quality)
    {
        _prototypes = prototypes;
        _frame = frame;
        _occlusion = occlusion;
        _quality = quality;
        // Three independent radiance passes, up to four live sprite layers per source.
        _seeds = new CinematicShaderPool(prototypes.Index<ShaderPrototype>("CMUCinematicEmission"), quality.Sources * 12);
        _bindings = new SeedBinding?[quality.Sources * 12];
        _downsample = prototypes.Index<ShaderPrototype>("CMUCinematicDownsample").InstanceUnique();
        _drawSeeds = DrawSeeds;
        _drawDownsample = DrawDownsample;
    }

    public void Render(IRenderTexture emission, IRenderTexture[] pyramid, Texture blockers,
        IRenderTexture? inner = null, IRenderTexture? outer = null, IRenderTexture? receiverMask = null)
    {
        _blockers = blockers;
        _seedPass = 0;
        _frame.Render(emission, _drawSeeds, CinematicRenderContext.RadianceClearColor);
        if (receiverMask != null)
            _occlusion.RenderReceivers(receiverMask, _quality, emission.Texture);
        if (inner != null && outer != null)
        {
            _seedPass = 1;
            _frame.Render(inner, _drawSeeds, CinematicRenderContext.RadianceClearColor);
            _seedPass = 2;
            _frame.Render(outer, _drawSeeds, CinematicRenderContext.RadianceClearColor);
        }
        _input = (inner ?? emission).Texture;
        for (var i = 0; i < pyramid.Length; i++)
        {
            if (i == 1)
                _input = (outer ?? emission).Texture;
            _spacing = 1f;
            _frame.Render(pyramid[i], _drawDownsample, CinematicRenderContext.ClearColor);
            _input = pyramid[i].Texture;
        }
    }

    private void DrawSeeds()
    {
        for (var sourceIndex = 0; sourceIndex < _frame.Sources.Count; sourceIndex++)
        {
            var source = _frame.Sources[sourceIndex];
            if (source.Sprite is { } sprite && !source.Profile.ProceduralEmission)
            {
                var luminous = DrawSprite(source, sprite, sourceIndex);
                if (_seedPass == 0)
                    _frame.Sources[sourceIndex] = source with
                    {
                        LuminousPosition = luminous.Position,
                        LuminousAxis = luminous.Axis,
                        LuminousSpan = luminous.Span,
                    };
            }
            else if (source.Profile.ProceduralEmission)
            {
                var seed = BindSeed(source, sourceIndex * 12 + _seedPass * 4,
                    source.Profile.EmissionRegion, false);
                seed.SetParameter("sprite_emission", false);
                _frame.Handle.UseShader(seed);
                _occlusion.DrawMesh(_occlusion.GetMesh(source, _quality));
            }
        }
    }

    private ShaderInstance BindSeed(in CinematicLightSource source, int slot, Vector4 crop, bool authored)
    {
        var seed = _seeds.Get(slot);
        _frame.BindFrame(seed);
        _frame.BindSource(seed, source);
        var profile = source.Profile;
        var gain = _seedPass == 0 ? 1f : profile.Bloom * (_seedPass == 1 ? profile.InnerGlow : profile.OuterGlow);
        var binding = new SeedBinding(profile.EmissionSize, crop, profile.Threshold,
            profile.EmissionChroma, profile.Core, profile.TextureColor, gain, authored);
        if (_bindings[slot] != binding)
        {
            _bindings[slot] = binding;
            seed.SetParameter("emission_size", binding.Size);
            seed.SetParameter("emission_region", binding.Crop);
            seed.SetParameter("emission_threshold", binding.Threshold);
            seed.SetParameter("emission_chroma", binding.Chroma);
            seed.SetParameter("core_strength", binding.Core);
            seed.SetParameter("texture_weight", binding.TextureColor);
            seed.SetParameter("bloom_strength", binding.Gain);
            seed.SetParameter("authored_mask", binding.Authored);
        }
        return seed;
    }

    private LuminousShape DrawSprite(in CinematicLightSource source, SpriteComponent sprite, int sourceIndex)
    {
        if (sprite.AllLayers is not IReadOnlyList<ISpriteLayer> layers)
            return new(source.EmissionPosition, Vector2.UnitX, 0.1f);
        var luminous = new LuminousShape(source.EmissionPosition, Vector2.UnitX, 0.1f);
        var located = false;
        var eyeRotation = _frame.Viewport.Eye!.Rotation;
        var angle = (source.EntityRotation + eyeRotation).Reduced().FlipPositive();
        var entityMatrix = Matrix3Helpers.CreateTransform(source.EntityPosition,
            CinematicLightingGeometry.RenderedRotation(source.EntityRotation, eyeRotation,
                sprite.NoRotation, sprite.SnapCardinals));
        var spriteMatrix = sprite.LocalMatrix * entityMatrix;
        CMUCinematicEmissionMaskPrototype? mask = null;
        if (source.Profile.EmissionMask is { } maskId)
            _prototypes.TryIndex(maskId, out mask);
        for (var i = 0; i < Math.Min(layers.Count, 4); i++)
        {
            if (layers[i] is not SpriteComponent.Layer layer || !layer.Visible || layer.Color.A <= 0.01f)
                continue;
            Texture? texture = layer.Texture;
            var direction = layer.EffectiveDirection(angle);
            if (layer.ActualRsi?.TryGetState(layers[i].RsiState, out var state) == true)
            {
                direction = layer.EffectiveDirection(state, angle,
                    sprite.EnableDirectionOverride ? sprite.DirectionOverride : null);
                texture = state.GetFrame(direction, layers[i].AnimationFrame);
            }
            if (texture == null)
                continue;
            var authored = mask != null && texture.Size == mask.FrameSize
                && (mask.Rsi.Length == 0 || layer.ActualRsi?.Path.ToString() == mask.Rsi);
            var region = authored ? mask!.Directions.GetValueOrDefault(direction, mask.Region)
                : source.Profile.EmissionRegion;
            // Restrict rasterization to the luminous region while preserving its live texture UVs.
            var seed = BindSeed(source, sourceIndex * 12 + _seedPass * 4 + i, region, authored);
            seed.SetParameter("sprite_emission", true);
            layer.GetLayerDrawMatrix(direction, out var layerMatrix);
            _frame.SetWorldTransform(layerMatrix * spriteMatrix);
            _frame.Handle.UseShader(seed);
            var size = texture.Size / (float) EyeManager.PixelsPerMeter;
            var min = new Vector2(region.X, region.Y);
            var max = new Vector2(region.Z, region.W);
            var quad = new Box2((min - new Vector2(0.5f)) * size, (max - new Vector2(0.5f)) * size);
            var sub = new UIBox2(region.X * texture.Width, (1f - region.W) * texture.Height,
                region.Z * texture.Width, (1f - region.Y) * texture.Height);
            if (!located)
            {
                var matrix = layerMatrix * spriteMatrix;
                var localAxis = quad.Width >= quad.Height ? Vector2.UnitX : Vector2.UnitY;
                var axis = Vector2.TransformNormal(localAxis, matrix);
                luminous = new LuminousShape(Vector2.Transform(quad.Center, matrix), Vector2.Normalize(axis),
                    Math.Max(quad.Width, quad.Height) * axis.Length() * 0.5f);
                located = true;
            }
            _frame.Handle.DrawTextureRectRegion(texture, quad, sprite.Color * layer.Color, sub);
        }
        _frame.SetWorldTransform(Matrix3x2.Identity);
        return luminous;
    }

    private void DrawDownsample()
    {
        _downsample.SetParameter("source_texture", _input);
        _downsample.SetParameter("blocker_texture", _blockers);
        _downsample.SetParameter("target_size", _frame.TargetSize);
        _downsample.SetParameter("source_pixel", new Vector2(_spacing / _input.Width, _spacing / _input.Height));
        _frame.Handle.UseShader(_downsample);
        _frame.Handle.DrawRect(_frame.Bounds, Color.White);
    }

    public void Dispose()
    {
        _seeds.Dispose();
        _downsample.Dispose();
    }

    private readonly record struct SeedBinding(Vector2 Size, Vector4 Crop, float Threshold,
        float Chroma, float Core, float TextureColor, float Gain, bool Authored);
    private readonly record struct LuminousShape(Vector2 Position, Vector2 Axis, float Span);
}
