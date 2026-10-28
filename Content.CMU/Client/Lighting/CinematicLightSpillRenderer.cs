using Content.Client.CMU14.Lighting.Configuration;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.CMU14.Lighting;

/// <summary>Local secondary illumination and directional material highlights share one bounded pass.</summary>
internal sealed class CinematicLightSpillRenderer : IDisposable
{
    private readonly CinematicRenderContext _frame;
    private readonly CinematicOcclusionRenderer _occlusion;
    private readonly CinematicLightingQuality _quality;
    private readonly CinematicShaderPool _shaders;
    private readonly Action _draw;
    private Texture _materials = default!;

    public CinematicLightSpillRenderer(IPrototypeManager prototypes, CinematicRenderContext frame,
        CinematicOcclusionRenderer occlusion, CinematicLightingQuality quality)
    {
        _frame = frame;
        _occlusion = occlusion;
        _quality = quality;
        _shaders = new CinematicShaderPool(prototypes.Index<ShaderPrototype>("CMUCinematicSpill"), quality.Sources);
        _draw = Draw;
    }

    public void Render(IRenderTexture target, Texture materials)
    {
        _materials = materials;
        _frame.Render(target, _draw, CinematicRenderContext.ClearColor);
    }

    private void Draw()
    {
        var slot = 0;
        foreach (var source in _frame.Sources)
        {
            var mesh = _occlusion.GetMesh(source, _quality);
            if (!mesh.Valid)
                continue;
            var shader = _shaders.Get(slot++);
            _frame.BindFrame(shader);
            shader.SetParameter("material_texture", _materials);
            _frame.BindSource(shader, source);
            shader.SetParameter("spill_strength", Math.Clamp(source.Profile.Spill, 0f, 0.5f));
            _frame.Handle.UseShader(shader);
            _occlusion.DrawMesh(mesh);
        }
    }

    public void Dispose() => _shaders.Dispose();
}
