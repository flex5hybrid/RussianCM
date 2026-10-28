using Content.Client.CMU14.Lighting.Configuration;
using System.Numerics;
using Robust.Client.GameObjects;
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
    private readonly SpriteSystem _sprites;
    private readonly Action _draw;
    private Texture _materials = default!;
    private Texture _emission = default!;
    private bool _reflections;
    private bool _nativeContribution;
    private bool _bindShared;

    public CinematicLightSpillRenderer(IEntityManager entities, IPrototypeManager prototypes, CinematicRenderContext frame,
        CinematicOcclusionRenderer occlusion, CinematicLightingQuality quality)
    {
        _frame = frame;
        _occlusion = occlusion;
        _quality = quality;
        _sprites = entities.System<SpriteSystem>();
        _shaders = new CinematicShaderPool(prototypes.Index(SpillShader), quality.Sources);
        _draw = Draw;
    }

    public void RenderIrradiance(IRenderTexture irradiance)
    {
        _bindShared = true;
        _nativeContribution = false;
        _materials = _frame.Scene;
        _emission = _frame.Scene;
        _reflections = false;
        _frame.Render(irradiance, _draw, CinematicRenderContext.RadianceClearColor);
    }

    public void RenderReflections(IRenderTexture reflections, Texture materials, Texture emission)
    {
        _nativeContribution = false;
        _materials = materials;
        _emission = emission;
        _reflections = true;
        _frame.Render(reflections, _draw, CinematicRenderContext.RadianceClearColor);
    }

    public void RenderNativeContribution(IRenderTexture target)
    {
        _nativeContribution = true;
        _reflections = false;
        _materials = _emission = _frame.Scene;
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
                shader.SetParameter("emission_texture", _emission);
                var nativeColor = Color.FromSrgb(source.NativeColor);
                shader.SetParameter("native_color", new Vector3(nativeColor.R, nativeColor.G, nativeColor.B));
                shader.SetParameter("native_radius", source.NativeRadius);
                shader.SetParameter("native_energy", source.NativeEnergy);
                shader.SetParameter("native_falloff", source.NativeFalloff);
                shader.SetParameter("native_curve", source.NativeCurveFactor);
                shader.SetParameter("spill_strength", Math.Clamp(source.Profile.Spill, 0f, 8f) * 0.28f);
                shader.SetParameter("reflection_strength", Math.Clamp(source.Profile.Reflection, 0f, 8f));
                shader.SetParameter("emitter_span", source.Profile.EmissionSize.X);
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
                shader.SetParameter("emission_texture", _emission);
                var useSprite = source.Sprite != null && !source.Profile.ProceduralEmission;
                shader.SetParameter("sprite_reflection", useSprite);
                var bounds = useSprite
                    ? _sprites.CalculateBounds((source.Uid, source.Sprite!), source.EntityPosition,
                        source.EntityRotation, _frame.Viewport.Eye!.Rotation).CalcBoundingBox()
                    : Box2.CenteredAround(source.EmissionPosition, source.Profile.EmissionSize * 2f);
                shader.SetParameter("emission_bounds", new Vector4(bounds.Left, bounds.Bottom, bounds.Right, bounds.Top));
            }
            _frame.Handle.UseShader(shader);
            _occlusion.DrawMesh(mesh);
        }
        _bindShared = false;
    }

    public void Dispose() => _shaders.Dispose();
}
