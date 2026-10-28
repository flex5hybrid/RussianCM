using Content.Client.CMU14.Lighting.Configuration;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.CMU14.Lighting;

/// <summary>Selective emissive extraction followed by a barrier-aware downsample pyramid.</summary>
internal sealed class CinematicBloomRenderer : IDisposable
{
    private readonly CinematicRenderContext _frame;
    private readonly CinematicOcclusionRenderer _occlusion;
    private readonly CinematicLightingQuality _quality;
    private readonly CinematicShaderPool _seeds;
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
        _seeds = new CinematicShaderPool(prototypes.Index<ShaderPrototype>("CMUCinematicEmission"), quality.Sources);
        _downsample = prototypes.Index<ShaderPrototype>("CMUCinematicDownsample").InstanceUnique();
        _drawSeeds = DrawSeeds;
        _drawDownsample = DrawDownsample;
    }

    public void Render(IRenderTexture[] pyramid, Texture blockers)
    {
        _blockers = blockers;
        _frame.Render(pyramid[0], _drawSeeds, CinematicRenderContext.ClearColor);
        for (var i = 1; i < pyramid.Length; i++)
        {
            _input = pyramid[i - 1].Texture;
            _frame.Render(pyramid[i], _drawDownsample, CinematicRenderContext.ClearColor);
        }
    }

    private void DrawSeeds()
    {
        var slot = 0;
        foreach (var source in _frame.Sources)
        {
            var mesh = _occlusion.GetMesh(source, _quality);
            if (!mesh.Valid)
                continue;
            var seed = _seeds.Get(slot++);
            _frame.BindFrame(seed);
            seed.SetParameter("scene_texture", _frame.Scene);
            _frame.BindSource(seed, source);
            seed.SetParameter("emission_size", source.Profile.EmissionSize);
            seed.SetParameter("emission_threshold", source.Profile.Threshold);
            seed.SetParameter("bloom_strength", source.Profile.Bloom);
            seed.SetParameter("procedural_emission", source.Profile.ProceduralEmission);
            _frame.Handle.UseShader(seed);
            _occlusion.DrawMesh(mesh);
        }
    }

    private void DrawDownsample()
    {
        _downsample.SetParameter("source_texture", _input);
        _downsample.SetParameter("blocker_texture", _blockers);
        _downsample.SetParameter("target_size", _frame.TargetSize);
        _downsample.SetParameter("source_pixel", new System.Numerics.Vector2(1f / _input.Width, 1f / _input.Height));
        _frame.Handle.UseShader(_downsample);
        _frame.Handle.DrawRect(_frame.Bounds, Color.White);
    }

    public void Dispose()
    {
        _seeds.Dispose();
        _downsample.Dispose();
    }
}
