using Content.Client.CMU14.Lighting.Configuration;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client.CMU14.Lighting;

/// <summary>Independent irradiance and material-reflection fields, composed underneath entity sprites.</summary>
internal sealed class CinematicLightSpillRenderer : IDisposable
{
    private static readonly ProtoId<ShaderPrototype> SpillShader = "CMUCinematicSpill";
    private readonly CinematicRenderContext _frame;
    private readonly CinematicOcclusionRenderer _occlusion;
    private readonly CinematicLightingQuality _quality;
    private readonly CinematicShaderPool _shaders;
    private readonly NativeBinding?[] _nativeBindings;
    private readonly AuthoredBinding?[] _authoredBindings;
    private readonly Action _draw;
    private Texture _materials = default!;
    private bool _reflections;
    private bool _nativeContribution;
    private bool _bindShared;

    public CinematicLightSpillRenderer(IPrototypeManager prototypes, CinematicRenderContext frame,
        CinematicOcclusionRenderer occlusion, CinematicLightingQuality quality)
    {
        _frame = frame;
        _occlusion = occlusion;
        _quality = quality;
        _shaders = new CinematicShaderPool(prototypes.Index(SpillShader), quality.Sources);
        _nativeBindings = new NativeBinding?[quality.Sources];
        _authoredBindings = new AuthoredBinding?[quality.Sources];
        _draw = Draw;
    }

    public void RenderIrradiance(IRenderTexture irradiance)
    {
        _bindShared = true;
        _nativeContribution = false;
        _materials = _frame.Scene;
        _reflections = false;
        _frame.Render(irradiance, _draw, CinematicRenderContext.RadianceClearColor);
    }

    public void RenderReflections(IRenderTexture reflections, Texture materials)
    {
        _nativeContribution = false;
        _materials = materials;
        _reflections = true;
        _frame.Render(reflections, _draw, CinematicRenderContext.RadianceClearColor);
    }

    public void RenderNativeContribution(IRenderTexture target)
    {
        _nativeContribution = true;
        _reflections = false;
        _materials = _frame.Scene;
        _frame.Render(target, _draw, CinematicRenderContext.RadianceClearColor);
    }

    private void Draw()
    {
        // Keep each source in the same shader slot in every pass, including sources skipped
        // by the native-light model. Shared frame/source uniforms only need uploading once.
        for (var slot = 0; slot < _frame.Sources.Count; slot++)
        {
            var source = _frame.Sources[slot];
            if (_nativeContribution && (source.NativeEnergy <= 0f || source.Profile.ProceduralEmission))
                continue;
            var mesh = _occlusion.GetMesh(source, _quality);
            if (!mesh.Valid)
                continue;
            var shader = _shaders.Get(slot);
            if (_bindShared)
            {
                _frame.BindFrame(shader);
                _frame.BindSource(shader, source);
                // Reset texture references after resize, even for branches not sampling them.
                shader.SetParameter("material_texture", _materials);
                shader.SetParameter("scene_texture", _frame.Scene);
                // SetParameter boxes scalar/vector values in the public shader API. Keep stable
                // radiometry in bounded slot caches, while positions, masks and animation update.
                var native = new NativeBinding(source.NativeColor, source.NativeRadius, source.NativeEnergy,
                    source.NativeFalloff, source.NativeCurveFactor);
                if (_nativeBindings[slot] != native)
                {
                    _nativeBindings[slot] = native;
                    var nativeColor = Color.FromSrgb(native.Color);
                    shader.SetParameter("native_color", new Vector3(nativeColor.R, nativeColor.G, nativeColor.B));
                    shader.SetParameter("native_radius", native.Radius);
                    shader.SetParameter("native_energy", native.Energy);
                    shader.SetParameter("native_falloff", native.Falloff);
                    shader.SetParameter("native_curve", native.Curve);
                }
                var authored = new AuthoredBinding(Math.Clamp(source.Profile.Spill, 0f, 8f) * 0.28f,
                    Math.Clamp(source.Profile.Reflection, 0f, 8f), source.Profile.EmissionSize.X);
                if (_authoredBindings[slot] != authored)
                {
                    _authoredBindings[slot] = authored;
                    shader.SetParameter("spill_strength", authored.Spill);
                    shader.SetParameter("reflection_strength", authored.Reflection);
                    shader.SetParameter("emitter_span", authored.Span);
                }
                var direction = mesh.SurfaceDirection.LengthSquared() > 0f
                    ? mesh.SurfaceDirection : -Vector2.Normalize(_frame.WorldY);
                shader.SetParameter("reflection_direction", direction);
            }
            shader.SetParameter("native_pass", _nativeContribution);
            shader.SetParameter("reflection_pass", _reflections);
            if (_reflections)
            {
                shader.SetParameter("material_texture", _materials);
                shader.SetParameter("scene_texture", _frame.Scene);
            }
            _frame.Handle.UseShader(shader);
            _occlusion.DrawMesh(mesh);
        }
        _bindShared = false;
    }

    public void Dispose() => _shaders.Dispose();

    private readonly record struct NativeBinding(Color Color, float Radius, float Energy, float Falloff, float Curve);
    private readonly record struct AuthoredBinding(float Spill, float Reflection, float Span);
}
