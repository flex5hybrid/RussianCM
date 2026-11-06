using Content.Client.CMU14.Lighting.Configuration;
using Content.Shared.CCVar;
using Content.Shared.CMU14.Lighting;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Client.CMU14.Lighting;

/// <summary>Masked, occluder-clipped light scattering. Reuses the real light rotation and color.</summary>
internal sealed class CinematicAtmosphereRenderer : IDisposable
{
    private readonly IEntityManager _entities;
    private readonly IConfigurationManager _config;
    private readonly CinematicRenderContext _frame;
    private readonly CinematicOcclusionRenderer _occlusion;
    private readonly CinematicLightingQuality _quality;
    private readonly CinematicShaderPool _shaders;
    private readonly Action _draw;
    public int LastSources { get; private set; }

    public CinematicAtmosphereRenderer(IEntityManager entities, IConfigurationManager config,
        IPrototypeManager prototypes, CinematicRenderContext frame, CinematicOcclusionRenderer occlusion,
        CinematicLightingQuality quality)
    {
        _entities = entities;
        _config = config;
        _frame = frame;
        _occlusion = occlusion;
        _quality = quality;
        _shaders = new CinematicShaderPool(prototypes.Index<ShaderPrototype>("CMUCinematicAtmosphere"), quality.AtmosphericSources);
        _draw = Draw;
    }

    public void Render(IRenderTexture target) => _frame.Render(target, _draw, CinematicRenderContext.RadianceClearColor);

    private void Draw()
    {
        LastSources = 0;
        if (!_frame.Controls.Atmosphere)
            return;
        foreach (var source in _frame.Sources)
        {
            if (source.Profile.Atmosphere <= 0f || LastSources >= _quality.AtmosphericSources)
                continue;
            var density = _config.GetCVar(CCVars.CMUCinematicAtmosphere);
            var roomDensity = _entities.TryGetComponent<CMUCinematicAtmosphereComponent>(source.Grid ?? _frame.MapUid, out var room)
                ? room.Density : 0.12f;
            density = CombineDensity(density, roomDensity);
            if (density <= 0f)
                continue;
            var mesh = _occlusion.GetMesh(source, _quality);
            if (!mesh.Valid)
                continue;
            var shader = _shaders.Get(LastSources++);
            _frame.BindFrame(shader);
            _frame.BindSource(shader, source);
            shader.SetParameter("atmosphere_density", Math.Clamp(density, 0f, 1f));
            shader.SetParameter("atmosphere_strength", Math.Clamp(source.Profile.Atmosphere, 0f, 4f));
            shader.SetParameter("beam_width", Math.Clamp(source.Profile.BeamWidth, 0.2f, 2f));
            shader.SetParameter("beam_falloff", Math.Clamp(source.Profile.BeamFalloff, 0.2f, 5f));
            shader.SetParameter("beam_cone", source.Profile.BeamCone);
            _frame.Handle.UseShader(shader);
            _occlusion.DrawMesh(mesh);
        }
    }

    internal static float CombineDensity(float userDensity, float roomDensity) =>
        Math.Clamp(userDensity, 0f, 1f) * Math.Clamp(roomDensity, 0f, 1f);

    public void Dispose() => _shaders.Dispose();
}
