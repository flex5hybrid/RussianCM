using System.Diagnostics;
using System.Numerics;
using Content.Client.Graphics;
using Content.Client.Light.EntitySystems;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = Robust.Shared.Maths.Color;

namespace Content.Client.CMU14.Lighting;

/// <summary>Runs after sprites have been lit, before the native hard FOV and vision/HUD overlays.</summary>
public sealed partial class CinematicLightingOverlay : Overlay
{
    [Dependency] private IClyde _clyde = default!;
    [Dependency] private ILightManager _lights = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private IResourceCache _textures = default!;
    [Dependency] private ITileDefinitionManager _tiles = default!;
    [Dependency] private IConfigurationManager _config = default!;
    private readonly IEntityManager _entities;
    private readonly CinematicLightingSystem _system;
    private readonly GridStencilSystem _stencils;
    private readonly OverlayResourceCache<CinematicViewportResources> _resources = new();
    private readonly Func<IClydeViewport, CinematicViewportResources> _factory;
    private CinematicViewportResources? _lastResources;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;
    public override bool RequestScreenTexture => true;

    public CinematicLightingOverlay(IEntityManager entities, CinematicLightingSystem system)
    {
        IoCManager.InjectDependencies(this);
        _entities = entities;
        _system = system;
        _stencils = entities.System<GridStencilSystem>();
        _factory = CreateResources;
        ZIndex = -100;
    }

    private CinematicViewportResources CreateResources(IClydeViewport viewport)
    {
        return new CinematicViewportResources(_clyde, _entities, _prototypes, _textures, _tiles, _config, _system.Quality);
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_system.IsEligible(args.Viewport, _lights)
            || !_entities.TryGetComponent<MapComponent>(args.MapUid, out var map) || !map.LightingEnabled)
        {
            _lastResources = null;
            return false;
        }
        var resources = _resources.GetForViewport(args.Viewport, _factory);
        _system.CollectSources(args.MapId, args.WorldAABB, args.Viewport.Eye!, resources.Frame.Sources);
        return resources.Frame.Sources.Count > 0;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;
        var start = Stopwatch.GetTimestamp();
        var resources = _resources.GetForViewport(args.Viewport, _factory);
        var frame = resources.Frame;
        _lastResources = resources;
        frame.Begin(args, ScreenTexture);
        resources.EnsureSize(args.Viewport.Size, args.MapId);
        if (!resources.Occlusion.Update(args.MapId, args.WorldAABB.Enlarged(7f)))
            return;

        resources.Occlusion.BeginMeshes();
        resources.Occlusion.RenderBlockers(resources.Blockers);
        resources.Bloom.Render(resources.Pyramid, resources.Blockers.Texture);
        if (resources.Materials != null && resources.Effects != null)
        {
            resources.Material!.Render(resources.Materials);
            resources.Spill!.Render(resources.Effects, resources.Materials.Texture);
            resources.Atmosphere!.Render(resources.Effects);
        }
        resources.Occlusion.EndMeshes();

        var composite = resources.Composite;
        // Frame uniforms were set for an offscreen pass; the final pass uses the actual viewport size.
        frame.BindFrame(composite);
        composite.SetParameter("target_size", (Vector2) args.Viewport.Size);
        composite.SetParameter("scene_texture", ScreenTexture);
        composite.SetParameter("light_texture", args.Viewport.LightRenderTarget.Texture);
        composite.SetParameter("blocker_texture", resources.Blockers.Texture);
        composite.SetParameter("surface_texture", _stencils.GetNonSpaceStencil(args).Texture);
        composite.SetParameter("effects_texture", (resources.Effects ?? resources.Pyramid[0]).Texture);
        composite.SetParameter("surface_effects", resources.Effects != null);
        composite.SetParameter("bloom_small", resources.Pyramid[1].Texture);
        composite.SetParameter("bloom_medium", resources.Pyramid[Math.Min(2, resources.Pyramid.Length - 1)].Texture);
        composite.SetParameter("bloom_large", resources.Pyramid[^1].Texture);
        args.WorldHandle.SetTransform(Matrix3x2.Identity);
        args.WorldHandle.UseShader(composite);
        args.WorldHandle.DrawRect(args.WorldBounds, Color.White);
        args.WorldHandle.UseShader(null);

        _system.LastFrame = new CinematicLightingStatistics(_system.Quality.Level, frame.Sources.Count,
            resources.Atmosphere?.LastSources ?? 0, resources.Occlusion.EdgeCount, resources.Occlusion.MeshRebuilds,
            resources.TargetCount, resources.TargetBytes, Stopwatch.GetElapsedTime(start).TotalMilliseconds);
    }

    protected override void DisposeBehavior()
    {
        _lastResources = null;
        _resources.Dispose();
        ScreenTexture = null;
        base.DisposeBehavior();
    }

    internal void CaptureDebug(Action<string, Image<Rgba32>> capture)
    {
        if (_lastResources is not { } resources)
            return;
        Copy("blockers", resources.Blockers);
        if (resources.Materials != null)
            Copy("materials", resources.Materials);
        if (resources.Effects != null)
            Copy("effects", resources.Effects);
        for (var i = 0; i < resources.Pyramid.Length; i++)
            Copy($"bloom-{i}", resources.Pyramid[i]);
        return;

        void Copy(string name, IRenderTexture target) =>
            target.CopyPixelsToMemory<Rgba32>(img => capture(name, img));
    }
}
