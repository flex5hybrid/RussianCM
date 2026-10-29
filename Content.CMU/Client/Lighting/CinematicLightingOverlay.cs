using System.Diagnostics;
using System.Numerics;
using Content.Client.Graphics;
using Content.Client.Light.EntitySystems;
using Content.Shared.CCVar;
using Robust.Client.GameObjects;
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

/// <summary>Owns the terrain-lighting and final lens passes, on either side of native entity rendering.</summary>
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
    internal IReadOnlyList<CinematicLightSource> Sources => _lastResources?.Frame.Sources
        ?? (IReadOnlyList<CinematicLightSource>) Array.Empty<CinematicLightSource>();
    internal CinematicRenderContext? CaptureFrame => _lastResources?.Frame;
    private double _surfaceCpu;
    private double _waterCpu;
    private double _lightingCpu;
    private double _prepareCpu;
    private readonly Action _drawLight;
    private readonly Action _copyLight;
    private CinematicViewportResources _lightResources = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;
    public override bool RequestScreenTexture => true;

    public CinematicLightingOverlay(IEntityManager entities, CinematicLightingSystem system)
    {
        IoCManager.InjectDependencies(this);
        _entities = entities;
        _system = system;
        _stencils = entities.System<GridStencilSystem>();
        _factory = CreateResources;
        _drawLight = DrawLightBuffer;
        _copyLight = CopyLightBuffer;
        ZIndex = -100;
    }

    private CinematicViewportResources CreateResources(IClydeViewport viewport) =>
        new(_clyde, _entities, _prototypes, _textures, _tiles, _config,
            _system.Quality, _system.NormalsEnabled);

    internal bool Prepare(in OverlayDrawArgs args, bool gatherSources = true)
    {
        if (!_system.IsEligible(args.Viewport, _lights)
            || !_entities.TryGetComponent<MapComponent>(args.MapUid, out var map) || !map.LightingEnabled)
        {
            _lastResources = null;
            return false;
        }
        var resources = _resources.GetForViewport(args.Viewport, _factory);
        if (gatherSources || !resources.SurfacePrepared)
        {
            var start = Stopwatch.GetTimestamp();
            _system.CollectSources(args.MapId, args.WorldAABB, args.Viewport.Eye!, resources.Frame.Sources);
            _prepareCpu = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        }
        return resources.Frame.Sources.Count > 0;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args) => Prepare(args, !_system.Quality.SurfaceEffects);

    private CinematicViewportResources Begin(in OverlayDrawArgs args, Texture scene)
    {
        var resources = _resources.GetForViewport(args.Viewport, _factory);
        _lastResources = resources;
        resources.Frame.Begin(args, scene);
        resources.Frame.Controls = _system.Controls;
        resources.EnsureSize(args.Viewport.Size, args.MapId, args.Viewport.LightRenderTarget.Size);
        return resources;
    }

    private bool BeginGeometry(CinematicViewportResources resources, in OverlayDrawArgs args)
    {
        if (!resources.Occlusion.Update(args.MapId, args.WorldAABB.Enlarged(7f)))
            return false;
        resources.Occlusion.BeginMeshes();
        resources.Occlusion.RenderBlockers(resources.Blockers);
        return true;
    }

    internal void DrawIrradiance(in OverlayDrawArgs args)
    {
        var start = Stopwatch.GetTimestamp();
        var resources = Begin(args, args.Viewport.RenderTarget.Texture);
        _waterCpu = 0;
        resources.SurfacePrepared = false;
        if (!BeginGeometry(resources, args))
            return;
        resources.Bloom.Render(resources.Emission, resources.OpticalSeeds, resources.Pyramid,
            resources.Blockers.Texture, resources.Blockers);
        resources.Spill!.RenderIrradiance(resources.Irradiance!, Exposure);
        _lightResources = resources;
        resources.Frame.Render(resources.NativeLightCopy!, _copyLight);
        resources.Frame.Render(args.Viewport.LightRenderTarget, _drawLight);
        resources.SurfacePrepared = true;
        _lightingCpu = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
    }

    private void DrawLightBuffer()
    {
        var frame = _lightResources.Frame;
        var shader = _lightResources.LightComposite!;
        frame.BindFrame(shader);
        shader.SetParameter("irradiance_texture", _lightResources.Irradiance!.Texture);
        shader.SetParameter("native_light_texture", _lightResources.NativeLightCopy!.Texture);
        shader.SetParameter("blocker_texture", _lightResources.Blockers.Texture);
        shader.SetParameter("local_exposure", Exposure);
        shader.SetParameter("native_only", _system.Controls.View == CinematicDebugView.Native);
        frame.Handle.UseShader(shader);
        frame.Handle.DrawRect(frame.Bounds, Color.White);
    }

    private void CopyLightBuffer()
    {
        var frame = _lightResources.Frame;
        var shader = _lightResources.LightCopy!;
        shader.SetParameter("native_copy_texture", frame.Viewport.LightRenderTarget.Texture);
        shader.SetParameter("target_size", frame.TargetSize);
        frame.Handle.UseShader(shader);
        frame.Handle.DrawRect(frame.Bounds, Color.White);
    }

    internal void DrawSurfaces(in OverlayDrawArgs args, Texture scene)
    {
        var start = Stopwatch.GetTimestamp();
        var resources = Begin(args, scene);
        if (!resources.SurfacePrepared)
            return;
        var frame = resources.Frame;
        resources.Material!.Render(resources.Materials!);
        resources.Contact!.Render(resources.Blockers);
        resources.Spill!.RenderReflections(resources.Reflections!, resources.Materials!.Texture, resources.Emission.Texture);
        var shader = resources.SurfaceComposite!;
        frame.BindFrame(shader);
        shader.SetParameter("target_size", (Vector2) args.Viewport.Size);
        shader.SetParameter("scene_texture", scene);
        shader.SetParameter("material_texture", resources.Materials.Texture);
        shader.SetParameter("reflection_texture", resources.Reflections!.Texture);
        shader.SetParameter("local_exposure", Exposure);
        args.WorldHandle.SetTransform(Matrix3x2.Identity);
        args.WorldHandle.UseShader(shader);
        args.WorldHandle.DrawRect(args.WorldBounds, Color.White);
        args.WorldHandle.UseShader(null);
        resources.SurfacePrepared = true;
        _surfaceCpu = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
    }

    internal void DrawWaterSurfaces(in OverlayDrawArgs args)
    {
        if (!_system.Controls.Reflections || _system.Controls.View != CinematicDebugView.Final)
            return;
        var resources = _resources.GetForViewport(args.Viewport, _factory);
        if (!resources.SurfacePrepared || resources.Water == null)
            return;
        var start = Stopwatch.GetTimestamp();
        resources.Water.DrawReflections(args, resources.Reflections!.Texture, resources.Blockers.Texture, Exposure);
        _waterCpu = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
    }

    internal bool BindWaterSubmersion(in BeforePostShaderRenderEvent args, EntityUid receiver)
    {
        if (!_system.Quality.SurfaceEffects || !_system.Controls.Reflections
            || _system.Controls.View != CinematicDebugView.Final
            || !_system.IsEligible(args.Viewport, _lights)
            || _lastResources is not { SurfacePrepared: true, Water: { } water } resources
            || resources.Frame.Viewport != args.Viewport || !water.ContainsReceiver(receiver))
            return false;
        var start = Stopwatch.GetTimestamp();
        water.BindResponse(args.Shader, resources.Reflections!.Texture, resources.Blockers.Texture, Exposure);
        _waterCpu += Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        return true;
    }

    private float Exposure => Math.Clamp(_config.GetCVar(CCVars.CMUCinematicExposure), 0f, 3f);

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;
        var start = Stopwatch.GetTimestamp();
        var resources = Begin(args, ScreenTexture);
        if (!resources.SurfacePrepared && !BeginGeometry(resources, args))
            return;
        var frame = resources.Frame;
        if (!_system.Quality.SurfaceEffects || !resources.SurfacePrepared)
            resources.Bloom.Render(resources.Emission, resources.OpticalSeeds, resources.Pyramid,
                resources.Blockers.Texture, resources.Blockers);
        if (resources.AtmosphereTarget != null)
            resources.Atmosphere!.Render(resources.AtmosphereTarget);
        // Normal mapping stays presentation-only: it never modifies the native light map.
        // Perform this before releasing the shared visibility meshes used by its light pass.
        if (resources.NormalRenderer != null && resources.Normals != null && resources.NormalLight != null)
            resources.NormalRenderer.Render(resources.Normals, resources.NormalLight, _system.NormalsAll);
        resources.Occlusion.EndMeshes();

        var composite = resources.Composite;
        frame.BindFrame(composite);
        composite.SetParameter("target_size", (Vector2) args.Viewport.Size);
        composite.SetParameter("scene_texture", ScreenTexture);
        composite.SetParameter("surface_texture", _stencils.GetNonSpaceStencil(args).Texture);
        composite.SetParameter("emission_texture", resources.Emission.Texture);
        composite.SetParameter("atmosphere_texture", (resources.AtmosphereTarget ?? resources.Emission).Texture);
        composite.SetParameter("atmospheric_effects", resources.AtmosphereTarget != null);
        composite.SetParameter("bloom_compact", resources.Pyramid[0].Texture);
        composite.SetParameter("bloom_medium", resources.Pyramid[Math.Min(1, resources.Pyramid.Length - 1)].Texture);
        composite.SetParameter("bloom_wide", resources.Pyramid[^1].Texture);
        composite.SetParameter("local_exposure", Exposure);
        composite.SetParameter("debug_view", (int) _system.Controls.View);
        composite.SetParameter("glow_enabled", _system.Controls.Glow);
        composite.SetParameter("blocker_texture", resources.Blockers.Texture);
        composite.SetParameter("direct_texture", (resources.Irradiance ?? resources.Emission).Texture);
        composite.SetParameter("reflection_texture", (resources.Reflections ?? resources.Emission).Texture);
        composite.SetParameter("material_texture", (resources.Materials ?? resources.Emission).Texture);
        composite.SetParameter("native_texture", (resources.NativeLightCopy ?? resources.Emission).Texture);
        composite.SetParameter("normal_texture", (resources.Normals ?? resources.Emission).Texture);
        composite.SetParameter("normal_light_texture", (resources.NormalLight ?? resources.Emission).Texture);
        composite.SetParameter("normals_enabled", resources.NormalRenderer != null);
        args.WorldHandle.SetTransform(Matrix3x2.Identity);
        args.WorldHandle.UseShader(composite);
        args.WorldHandle.DrawRect(args.WorldBounds, Color.White);
        args.WorldHandle.UseShader(null);
        resources.SurfacePrepared = false;
        _system.LastFrame = new CinematicLightingStatistics(_system.Quality.Level, frame.Sources.Count,
            resources.Atmosphere?.LastSources ?? 0, resources.Occlusion.EdgeCount, resources.Occlusion.MeshRebuilds,
            resources.TargetCount, resources.TargetBytes,
            _prepareCpu + (_system.Quality.SurfaceEffects ? _surfaceCpu + _lightingCpu + _waterCpu : 0)
            + Stopwatch.GetElapsedTime(start).TotalMilliseconds);
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
        Copy("native-light", resources.Frame.Viewport.LightRenderTarget);
        if (resources.NativeLightCopy != null)
            Copy("native-light-original", resources.NativeLightCopy);
        Copy("emission", resources.Emission);
        Copy("optical-seeds", resources.OpticalSeeds);
        if (resources.Materials != null)
        {
            Copy("materials", resources.Materials);
            Copy("irradiance", resources.Irradiance!);
            Copy("reflections", resources.Reflections!);
            if (resources.Normals != null)
                Copy("normals", resources.Normals);
            if (resources.NormalLight != null)
                Copy("normal-lights", resources.NormalLight);
            Copy("atmosphere", resources.AtmosphereTarget!);
        }
        for (var i = 0; i < resources.Pyramid.Length; i++)
            Copy($"bloom-{i}", resources.Pyramid[i]);
        return;

        void Copy(string name, IRenderTexture target) => target.CopyPixelsToMemory<Rgba32>(img => capture(name, img));
    }
}
