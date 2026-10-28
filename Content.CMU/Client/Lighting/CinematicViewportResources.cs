using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Configuration;
using Robust.Shared.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Content.Client.CMU14.Lighting.Configuration;

namespace Content.Client.CMU14.Lighting;

/// <summary>One owner per viewport. All GPU objects are disposed on resize, preset change, or viewport purge.</summary>
internal sealed class CinematicViewportResources : IDisposable
{
    private readonly IClyde _clyde;
    private readonly CinematicLightingQuality _quality;
    private Vector2i _size;
    private MapId _map;
    public readonly CinematicRenderContext Frame;
    public readonly CinematicOcclusionRenderer Occlusion;
    public readonly CinematicBloomRenderer Bloom;
    public readonly CinematicMaterialRenderer? Material;
    public readonly CinematicLightSpillRenderer? Spill;
    public readonly CinematicAtmosphereRenderer? Atmosphere;
    public readonly ShaderInstance Composite;
    public IRenderTexture Blockers = default!;
    public IRenderTexture[] Pyramid = Array.Empty<IRenderTexture>();
    public IRenderTexture? Materials;
    public IRenderTexture? Effects;
    public long TargetBytes { get; private set; }
    public int TargetCount => Pyramid.Length + (Blockers == null ? 0 : 1) + (Effects == null ? 0 : 2);

    public CinematicViewportResources(IClyde clyde, IEntityManager entities, IPrototypeManager prototypes,
        IResourceCache textures, ITileDefinitionManager tiles, IConfigurationManager config,
        CinematicLightingQuality quality)
    {
        _clyde = clyde;
        _quality = quality;
        Frame = new CinematicRenderContext(prototypes, textures);
        Occlusion = new CinematicOcclusionRenderer(entities, prototypes, Frame);
        Bloom = new CinematicBloomRenderer(prototypes, Frame, Occlusion, quality);
        Composite = prototypes.Index<ShaderPrototype>("CMUCinematicComposite").InstanceUnique();
        if (quality.SurfaceEffects)
        {
            Material = new CinematicMaterialRenderer(entities, prototypes, tiles, Frame);
            Spill = new CinematicLightSpillRenderer(prototypes, Frame, Occlusion, quality);
            Atmosphere = new CinematicAtmosphereRenderer(entities, config, prototypes, Frame, Occlusion, quality);
        }
    }

    public void EnsureSize(Vector2i size, MapId map)
    {
        if (_map != map)
        {
            Occlusion.Clear();
            _map = map;
        }
        if (_size == size)
            return;
        DisposeTargets();
        _size = size;
        Blockers = Create(size, "blockers", RenderTargetColorFormat.Rgba8);
        var reduced = new Vector2i(Math.Max(1, (size.X + _quality.ResolutionDivisor - 1) / _quality.ResolutionDivisor),
            Math.Max(1, (size.Y + _quality.ResolutionDivisor - 1) / _quality.ResolutionDivisor));
        if (_quality.SurfaceEffects)
        {
            Materials = Create(reduced, "materials", RenderTargetColorFormat.Rgba8);
            Effects = Create(reduced, "effects");
        }
        Pyramid = new IRenderTexture[_quality.BloomLevels];
        for (var i = 0; i < Pyramid.Length; i++)
        {
            Pyramid[i] = Create(reduced, $"bloom-{i}");
            reduced = new Vector2i(Math.Max(1, (reduced.X + 1) / 2), Math.Max(1, (reduced.Y + 1) / 2));
        }
    }

    private IRenderTexture Create(Vector2i size, string label, RenderTargetColorFormat format = RenderTargetColorFormat.Rgba8Srgb)
    {
        TargetBytes += (long) size.X * size.Y * 4;
        return _clyde.CreateRenderTarget(size,
            new RenderTargetFormatParameters(format, hasDepthStencil: false),
            new TextureSampleParameters { Filter = true }, $"cmu-cinematic-{label}");
    }

    private void DisposeTargets()
    {
        Blockers?.Dispose();
        Blockers = default!;
        Materials?.Dispose();
        Materials = null;
        Effects?.Dispose();
        Effects = null;
        foreach (var target in Pyramid)
            target?.Dispose();
        Pyramid = Array.Empty<IRenderTexture>();
        TargetBytes = 0;
    }

    public void Dispose()
    {
        DisposeTargets();
        Bloom.Dispose();
        Spill?.Dispose();
        Atmosphere?.Dispose();
        Composite.Dispose();
        Occlusion.Clear();
    }
}
