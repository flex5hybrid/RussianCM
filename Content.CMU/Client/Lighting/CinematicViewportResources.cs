using Content.Client.CMU14.Lighting.Configuration;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Configuration;
using Robust.Shared.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client.CMU14.Lighting;

/// <summary>Viewport-local HDR fields. All three overlays share one owner and one occlusion cache.</summary>
internal sealed class CinematicViewportResources : IDisposable
{
    private static readonly ProtoId<ShaderPrototype> CompositeShader = "CMUCinematicComposite";
    private static readonly ProtoId<ShaderPrototype> SurfaceShader = "CMUCinematicSurfaceComposite";
    private static readonly ProtoId<ShaderPrototype> LightShader = "CMUCinematicLightComposite";
    private readonly IClyde _clyde;
    private readonly CinematicLightingQuality _quality;
    private readonly bool _normalsEnabled;
    private Vector2i _size;
    private Vector2i _nativeLightSize;
    private MapId _map;
    public readonly CinematicRenderContext Frame;
    public readonly CinematicOcclusionRenderer Occlusion;
    public readonly CinematicBloomRenderer Bloom;
    public readonly CinematicMaterialRenderer? Material;
    public readonly CinematicWaterSurfaceRenderer? Water;
    public readonly CinematicContactRenderer? Contact;
    public readonly CinematicLightSpillRenderer? Spill;
    public readonly CinematicAtmosphereRenderer? Atmosphere;
    public readonly CinematicNormalRenderer? NormalRenderer;
    public readonly ShaderInstance Composite;
    public readonly ShaderInstance? SurfaceComposite;
    public readonly ShaderInstance? LightComposite;
    public readonly ShaderInstance? LightCopy;
    public IRenderTexture Blockers = default!;
    public IRenderTexture Emission = default!;
    public IRenderTexture OpticalSeeds = default!;
    public IRenderTexture[] Pyramid = Array.Empty<IRenderTexture>();
    public IRenderTexture? Materials;
    public IRenderTexture? Irradiance;
    public IRenderTexture? Reflections;
    public IRenderTexture? AtmosphereTarget;
    public IRenderTexture? NativeLightCopy;
    public IRenderTexture? Normals;
    public IRenderTexture? NormalLight;
    public bool SurfacePrepared;
    public long TargetBytes { get; private set; }
    public int TargetCount => Pyramid.Length + (Blockers == null ? 0 : 1) + (Emission == null ? 0 : 1)
        + (OpticalSeeds == null ? 0 : 1) + (Materials == null ? 0 : 5) + (Normals == null ? 0 : 2);

    public CinematicViewportResources(IClyde clyde, IEntityManager entities, IPrototypeManager prototypes,
        IResourceCache textures, ITileDefinitionManager tiles, IConfigurationManager config,
        CinematicLightingQuality quality, bool normalsEnabled)
    {
        _clyde = clyde;
        _quality = quality;
        _normalsEnabled = normalsEnabled && quality.SurfaceEffects;
        Frame = new CinematicRenderContext(prototypes, textures);
        Occlusion = new CinematicOcclusionRenderer(entities, prototypes, Frame);
        Bloom = new CinematicBloomRenderer(prototypes, Frame, Occlusion, quality);
        Composite = prototypes.Index(CompositeShader).InstanceUnique();
        if (quality.SurfaceEffects)
        {
            Material = new CinematicMaterialRenderer(entities, prototypes, tiles, Frame);
            Water = new CinematicWaterSurfaceRenderer(entities, prototypes, Frame, Material);
            Material.Water = Water;
            Contact = new CinematicContactRenderer(entities, prototypes, Frame, Material);
            Spill = new CinematicLightSpillRenderer(prototypes, Frame, Occlusion, quality);
            Atmosphere = new CinematicAtmosphereRenderer(entities, config, prototypes, Frame, Occlusion, quality);
            SurfaceComposite = prototypes.Index(SurfaceShader).InstanceUnique();
            LightComposite = prototypes.Index(LightShader).InstanceUnique();
            LightCopy = prototypes.Index<ShaderPrototype>("CMUCinematicLightCopy").InstanceUnique();
            if (_normalsEnabled)
                NormalRenderer = new CinematicNormalRenderer(entities, textures, prototypes, Frame, Occlusion, quality, config);
        }
    }

    public void EnsureSize(Vector2i size, MapId map, Vector2i? nativeLightSize = null)
    {
        if (_map != map)
        {
            Occlusion.Clear();
            SurfacePrepared = false;
            _map = map;
        }
        var requestedLightSize = nativeLightSize ?? new Vector2i((size.X + 1) / 2, (size.Y + 1) / 2);
        if (_size == size && (!_quality.SurfaceEffects || _nativeLightSize == requestedLightSize))
            return;
        DisposeTargets();
        _size = size;
        _nativeLightSize = requestedLightSize;
        Blockers = Create(size, "blockers", RenderTargetColorFormat.Rgba8);
        // Intrinsic RSI texels remain crisp with nearest sampling; a 2x/3x viewport does
        // not require a full-size HDR copy of each original pixel-art luminous element.
        Emission = Create(new Vector2i((size.X + 1) / 2, (size.Y + 1) / 2), "emission", filter: false);
        var reduced = new Vector2i(Math.Max(1, (size.X + _quality.ResolutionDivisor - 1) / _quality.ResolutionDivisor),
            Math.Max(1, (size.Y + _quality.ResolutionDivisor - 1) / _quality.ResolutionDivisor));
        // Two independent radiance fields, one target transition and one clear.
        OpticalSeeds = Create(new Vector2i(reduced.X * 2, reduced.Y), "optical-seeds");
        if (_quality.SurfaceEffects)
        {
            Materials = Create(reduced, "materials", RenderTargetColorFormat.Rgba8);
            Irradiance = Create(reduced, "irradiance");
            Reflections = Create(reduced, "reflections");
            AtmosphereTarget = Create(reduced, "atmosphere");
            NativeLightCopy = Create(_nativeLightSize, "native-light-copy");
            if (_normalsEnabled)
            {
                Normals = Create(size, "normals", RenderTargetColorFormat.Rgba8, filter: false);
                NormalLight = Create(size, "normal-light");
            }
        }
        Pyramid = new IRenderTexture[_quality.BloomLevels];
        for (var i = 0; i < Pyramid.Length; i++)
        {
            Pyramid[i] = Create(reduced, $"bloom-{i}");
            reduced = new Vector2i(Math.Max(1, (reduced.X + 1) / 2), Math.Max(1, (reduced.Y + 1) / 2));
        }
    }

    private IRenderTexture Create(Vector2i size, string label, RenderTargetColorFormat format = RenderTargetColorFormat.R11FG11FB10F,
        bool filter = true)
    {
        TargetBytes += (long) size.X * size.Y * (format == RenderTargetColorFormat.Rgba16F ? 8 : 4);
        return _clyde.CreateRenderTarget(size, new RenderTargetFormatParameters(format, hasDepthStencil: false),
            new TextureSampleParameters { Filter = filter }, $"cmu-cinematic-{label}");
    }

    private void DisposeTargets()
    {
        Blockers?.Dispose();
        Blockers = default!;
        Emission?.Dispose();
        Emission = default!;
        OpticalSeeds?.Dispose();
        OpticalSeeds = default!;
        Materials?.Dispose();
        Materials = null;
        Irradiance?.Dispose();
        Irradiance = null;
        Reflections?.Dispose();
        Reflections = null;
        AtmosphereTarget?.Dispose();
        AtmosphereTarget = null;
        NativeLightCopy?.Dispose();
        NativeLightCopy = null;
        Normals?.Dispose();
        Normals = null;
        NormalLight?.Dispose();
        NormalLight = null;
        foreach (var target in Pyramid)
            target?.Dispose();
        Pyramid = Array.Empty<IRenderTexture>();
        TargetBytes = 0;
        SurfacePrepared = false;
    }

    public void Dispose()
    {
        DisposeTargets();
        Bloom.Dispose();
        Contact?.Dispose();
        Water?.Dispose();
        Spill?.Dispose();
        Atmosphere?.Dispose();
        Composite.Dispose();
        SurfaceComposite?.Dispose();
        LightComposite?.Dispose();
        LightCopy?.Dispose();
        NormalRenderer?.Dispose();
        Occlusion.Dispose();
    }
}
