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
    private Texture _emission = default!;
    private bool _reflections;
    private float _nativeWeight;
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

    public void RenderIrradiance(IRenderTexture irradiance, float exposure = 1f)
    {
        _bindShared = true;
        _nativeWeight = .75f * Math.Clamp(exposure, 0f, 1f) / Math.Max(exposure, .001f);
        _materials = Texture.White;
        _reflections = false;
        _frame.Render(irradiance, _draw, CinematicRenderContext.RadianceClearColor);
    }

    public void RenderReflections(IRenderTexture reflections, Texture materials, Texture? emission = null)
    {
        _emission = emission ?? _frame.Scene;
        _materials = materials;
        _reflections = true;
        _frame.Render(reflections, _draw, CinematicRenderContext.RadianceClearColor);
    }

    private void Draw()
    {
        // Keep each source in the same shader slot in every pass, including sources skipped
        // by the native-light model. Shared frame/source uniforms only need uploading once.
        for (var slot = 0; slot < _frame.Sources.Count; slot++)
        {
            var source = _frame.Sources[slot];
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
                shader.SetParameter("scene_texture", Texture.White);
                // SetParameter boxes scalar/vector values in the public shader API. Keep stable
                // radiometry in bounded slot caches, while positions, masks and animation update.
                var native = new NativeBinding(source.NativeColor, source.NativeRadius, source.NativeEnergy,
                    source.NativeFalloff, source.NativeCurveFactor, source.Profile.ProceduralEmission);
                if (_nativeBindings[slot] != native)
                {
                    _nativeBindings[slot] = native;
                    var nativeColor = Color.FromSrgb(native.Color);
                    shader.SetParameter("native_color", new Vector3(nativeColor.R, nativeColor.G, nativeColor.B));
                    shader.SetParameter("native_radius", native.Radius);
                    var peak = Math.Max(nativeColor.R, Math.Max(nativeColor.G, nativeColor.B));
                    var saturation = (peak - Math.Min(nativeColor.R, Math.Min(nativeColor.G, nativeColor.B))) / Math.Max(peak, .001f);
                    var neutral = Math.Clamp((saturation - .10f) / .55f, 0f, 1f);
                    neutral = 1f - neutral * neutral * (3f - 2f * neutral);
                    shader.SetParameter("native_energy", native.Procedural ? 0f : native.Energy * neutral);
                    shader.SetParameter("native_falloff", native.Falloff);
                    shader.SetParameter("native_curve", native.Curve);
                }
                var controls = _frame.Controls;
                var authored = new AuthoredBinding(Math.Clamp(source.Profile.Spill, 0f, 8f),
                    Math.Clamp(source.Profile.Reflection, 0f, 8f), source.Profile.EmissionSize.X,
                    source.Profile.EmissionSize, (int) source.Profile.Shape, source.Profile.WallMounted,
                    source.Profile.Falloff, source.Profile.Indirect, _nativeWeight,
                    controls.Direct && controls.View != CinematicDebugView.Indirect,
                    controls.Indirect && controls.View != CinematicDebugView.Direct);
                if (_authoredBindings[slot] != authored)
                {
                    _authoredBindings[slot] = authored;
                    shader.SetParameter("spill_strength", authored.Spill);
                    shader.SetParameter("reflection_strength", authored.Reflection);
                    shader.SetParameter("emitter_span", authored.Span);
                    shader.SetParameter("emitter_size", authored.Size);
                    shader.SetParameter("source_shape", authored.Shape);
                    shader.SetParameter("wall_mounted", authored.WallMounted);
                    shader.SetParameter("area_samples", _quality.Level >= 3 ? 5 : 3);
                    shader.SetParameter("light_falloff", Math.Clamp(authored.Falloff, 0.5f, 12f));
                    shader.SetParameter("indirect_strength", Math.Clamp(authored.Indirect, 0f, 2f));
                    shader.SetParameter("native_replacement_weight", authored.NativeWeight);
                    shader.SetParameter("direct_enabled", authored.Direct);
                    shader.SetParameter("indirect_enabled", authored.IndirectEnabled);
                }
                var direction = mesh.SurfaceDirection.LengthSquared() > 0f
                    ? mesh.SurfaceDirection : -Vector2.Normalize(_frame.WorldY);
                if (source.Profile.Shape == Content.Shared.CMU14.Lighting.CMUCinematicSourceShape.Cone && source.Mask == null)
                    direction = source.Rotation.RotateVec(Vector2.UnitY);
                shader.SetParameter("reflection_direction", direction);
            }
            shader.SetParameter("reflection_pass", _reflections);
            if (_reflections)
            {
                shader.SetParameter("material_texture", _materials);
                shader.SetParameter("scene_texture", _frame.Scene);
                shader.SetParameter("emission_texture", _emission);
                shader.SetParameter("reflection_enabled", _frame.Controls.Reflections);
            }
            _frame.Handle.UseShader(shader);
            _occlusion.DrawMesh(mesh);
        }
        _bindShared = false;
    }

    public void Dispose() => _shaders.Dispose();

    private readonly record struct NativeBinding(Color Color, float Radius, float Energy, float Falloff, float Curve, bool Procedural);
    private readonly record struct AuthoredBinding(float Spill, float Reflection, float Span, Vector2 Size,
        int Shape, bool WallMounted, float Falloff, float Indirect, float NativeWeight, bool Direct, bool IndirectEnabled);
}
