using System.Numerics;
using Content.Client.CMU14.Lighting.Configuration;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client.CMU14.Lighting;

/// <summary>Extracts emissive pixels from the actual current sprite frame, then builds an HDR glow pyramid.</summary>
internal sealed class CinematicBloomRenderer : IDisposable
{
    private static readonly ProtoId<ShaderPrototype> EmissionShader = "CMUCinematicEmission";
    private static readonly ProtoId<ShaderPrototype> DownsampleShader = "CMUCinematicDownsample";
    private readonly CinematicRenderContext _frame;
    private readonly CinematicOcclusionRenderer _occlusion;
    private readonly CinematicLightingQuality _quality;
    private readonly CinematicShaderPool _seeds;
    private readonly SeedBinding?[] _seedBindings;
    private readonly ShaderInstance _downsample;
    private readonly Action _drawSeeds;
    private readonly Action _drawDownsample;
    private Texture _input = default!;
    private Texture _blockers = default!;

    public CinematicBloomRenderer(IPrototypeManager prototypes, CinematicRenderContext frame,
        CinematicOcclusionRenderer occlusion, CinematicLightingQuality quality)
    {
        _frame = frame;
        _occlusion = occlusion;
        _quality = quality;
        _seeds = new CinematicShaderPool(prototypes.Index(EmissionShader), quality.Sources);
        _seedBindings = new SeedBinding?[quality.Sources];
        _downsample = prototypes.Index(DownsampleShader).InstanceUnique();
        _drawSeeds = DrawSeeds;
        _drawDownsample = DrawDownsample;
    }

    public void Render(IRenderTexture emission, IRenderTexture[] pyramid, Texture blockers)
    {
        _blockers = blockers;
        _frame.Render(emission, _drawSeeds, CinematicRenderContext.RadianceClearColor);
        _input = emission.Texture;
        for (var i = 0; i < pyramid.Length; i++)
        {
            _frame.Render(pyramid[i], _drawDownsample, CinematicRenderContext.ClearColor);
            _input = pyramid[i].Texture;
        }
    }

    private void DrawSeeds()
    {
        var slot = 0;
        foreach (var source in _frame.Sources)
        {
            if (source.Sprite == null && !source.Profile.ProceduralEmission)
                continue;
            var seed = _seeds.Get(slot);
            _frame.BindFrame(seed);
            _frame.BindSource(seed, source);
            var binding = new SeedBinding(source.Profile.EmissionSize, source.Profile.EmissionRegion,
                source.Profile.Threshold, source.Profile.EmissionChroma, source.Profile.Core,
                source.Profile.TextureColor, source.Profile.Bloom);
            if (_seedBindings[slot] != binding)
            {
                _seedBindings[slot] = binding;
                seed.SetParameter("emission_size", binding.Size);
                seed.SetParameter("emission_region", binding.Region);
                seed.SetParameter("emission_threshold", binding.Threshold);
                seed.SetParameter("emission_chroma", binding.Chroma);
                seed.SetParameter("core_strength", binding.Core);
                seed.SetParameter("texture_weight", binding.TextureColor);
                seed.SetParameter("bloom_strength", binding.Bloom);
            }
            slot++;
            var sprite = source.Sprite;
            var useSprite = sprite != null && !source.Profile.ProceduralEmission;
            seed.SetParameter("sprite_emission", useSprite);
            _frame.Handle.UseShader(seed);
            if (useSprite)
                DrawSprite(source, sprite!);
            else
            {
                var mesh = _occlusion.GetMesh(source, _quality);
                _occlusion.DrawMesh(mesh);
            }
        }
    }

    private void DrawSprite(in CinematicLightSource source, SpriteComponent sprite)
    {
        // The public collection is IReadOnlyList-covariant in the pinned engine. Indexing avoids
        // allocating the IEnumerable iterator every frame; no texture readback or sprite clone.
        if (sprite.AllLayers is not IReadOnlyList<ISpriteLayer> layers)
            return;
        var eyeRotation = _frame.Viewport.Eye!.Rotation;
        var angle = (source.EntityRotation + eyeRotation).Reduced().FlipPositive();
        var cardinal = !sprite.NoRotation && sprite.SnapCardinals ? angle.RoundToCardinalAngle() : Angle.Zero;
        var entityMatrix = Matrix3Helpers.CreateTransform(source.EntityPosition,
            sprite.NoRotation ? -eyeRotation : source.EntityRotation - cardinal);
        var spriteMatrix = sprite.LocalMatrix * entityMatrix;
        for (var i = 0; i < Math.Min(layers.Count, 4); i++)
        {
            if (layers[i] is not SpriteComponent.Layer layer || !layer.Visible)
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
            layer.GetLayerDrawMatrix(direction, out var layerMatrix);
            _frame.SetWorldTransform(layerMatrix * spriteMatrix);
            var size = texture.Size / (float) EyeManager.PixelsPerMeter;
            _frame.Handle.DrawTextureRect(texture, Box2.CenteredAround(Vector2.Zero, size), sprite.Color * layer.Color);
        }
        _frame.SetWorldTransform(Matrix3x2.Identity);
    }

    private void DrawDownsample()
    {
        _downsample.SetParameter("source_texture", _input);
        _downsample.SetParameter("blocker_texture", _blockers);
        _downsample.SetParameter("target_size", _frame.TargetSize);
        _downsample.SetParameter("source_pixel", new Vector2(1f / _input.Width, 1f / _input.Height));
        _frame.Handle.UseShader(_downsample);
        _frame.Handle.DrawRect(_frame.Bounds, Color.White);
    }

    public void Dispose()
    {
        _seeds.Dispose();
        _downsample.Dispose();
    }

    private readonly record struct SeedBinding(Vector2 Size, Vector4 Region, float Threshold,
        float Chroma, float Core, float TextureColor, float Bloom);
}
