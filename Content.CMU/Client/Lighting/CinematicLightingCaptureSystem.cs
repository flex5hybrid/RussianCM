using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Numerics;
using Content.Client.Viewport;
using Content.Shared.CCVar;
using Robust.Client.Graphics;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = Robust.Shared.Maths.Color;

namespace Content.Client.CMU14.Lighting;

/// <summary>
/// Explicit developer capture, driven by a console command. Uses the game's viewport screenshot API;
/// does not capture the desktop. Frame intervals are end-to-end client intervals, not GPU query results.
/// </summary>
public sealed partial class CinematicLightingCaptureSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private IResourceManager _resources = default!;
    [Dependency] private IEyeManager _eyes = default!;
    [Dependency] private ILogManager _logs = default!;
    private readonly List<double> _samples = new();
    private readonly List<string> _rows = new(4);
    private readonly CopyPixelsDelegate<Rgba32> _screenshot;
    private bool _active;
    private bool _waitingForScene;
    private bool _waitingForImage;
    // Viewport screenshot completion is delivered by the renderer's frame loop.
    private bool _advancePending;
    private bool _imagesOnly;
    private double _worstFrame;
    private double _worstFrameCpu;
    private double _maxCpu;
    private int _quality;
    private int _seconds;
    private int _originalQuality;
    private int _originalMaxFps;
    private bool _originalVsync;
    private long _started;
    private DateTime _stageUtc;
    private CaptureState? _scene;
    private EntityUid? _reviewMap;
    private long _lastFrame;
    private long _frames;
    private double _total;
    private double _cpuTotal;
    private ResPath _directory;

    public CinematicLightingCaptureSystem()
    {
        _screenshot = SaveScreenshot;
    }

    public override void Initialize()
    {
        base.Initialize();
        if (CinematicLightingCaptureCommand.PendingSeconds is > 0 and var seconds)
        {
            CinematicLightingCaptureCommand.PendingSeconds = 0;
            Start(seconds);
        }
    }

    public void Start(int seconds)
    {
        if (_active)
            throw new InvalidOperationException(Loc.GetString("cmu-cinematic-capture-already-running"));
        if (seconds is < 3 or > 60)
            throw new ArgumentOutOfRangeException(nameof(seconds));
        _originalQuality = _config.GetCVar(CCVars.CMUCinematicLighting);
        _originalMaxFps = _config.GetCVar(CVars.DisplayMaxFPS);
        _originalVsync = _config.GetCVar(CVars.DisplayVSync);
        _seconds = seconds;
        _rows.Clear();
        _samples.EnsureCapacity(65536);
        _imagesOnly = false;
        _scene = null;
        _reviewMap = null;
        _directory = new ResPath($"/cinematic-lighting/{DateTime.UtcNow:yyyyMMdd-HHmmss}");
        _resources.UserData.CreateDir(_directory);
        _active = true;
        _waitingForScene = true;
        _waitingForImage = false;
        _advancePending = false;
        _started = Stopwatch.GetTimestamp();
        _config.SetCVar(CVars.DisplayVSync, false);
        _config.SetCVar(CVars.DisplayMaxFPS, 0);
        _config.SetCVar(CCVars.CMUCinematicLighting, 2);
        Log($"Capture armed; waiting for a lit main viewport. Output: {_directory}");
    }

    public override void FrameUpdate(float frameTime)
    {
        if (!_active)
            return;
        try
        {
            var elapsed = Stopwatch.GetElapsedTime(_started).TotalSeconds;
            if (_waitingForScene)
            {
                if (EntityManager.System<CinematicLightingSystem>().LastFrame.Sources > 0
                    && _eyes.MainViewport is ScalingViewport)
                {
                    _waitingForScene = false;
                    _reviewMap = EntityManager.System<CinematicLightingSystem>().CaptureFrame?.MapUid;
                    BeginStage(0);
                }
                else if (elapsed > 180)
                    Stop("No lit main viewport became available within 180 seconds.");
                return;
            }
            if (_scene is { } fixedScene && ReadScene() != fixedScene)
            {
                Stop("Camera or lighting settings changed during comparison; capture discarded.");
                return;
            }
            if (_waitingForImage)
            {
                if (_advancePending)
                {
                    _waitingForImage = false;
                    _advancePending = false;
                    if (_quality == 3)
                    {
                        Stop("Completed all four presets.");
                    }
                    else
                        BeginStage(_quality + 1);
                }
                else if (elapsed > _seconds + 20)
                    Stop("Viewport screenshot did not complete.");
                return;
            }

            var now = Stopwatch.GetTimestamp();
            if (elapsed < 2)
            {
                _lastFrame = now;
                return;
            }
            // Finish every timing stage before any PNG encoding or GPU readback. Diagnostic
            // targets requested after High used to complete during the following Ultra stage.
            if (_imagesOnly)
            {
                CaptureImages();
                return;
            }
            _scene ??= ReadScene();
            if (_scene == null || _quality >= 2
                && EntityManager.System<CinematicLightingSystem>().LastFrame.Sources == 0)
            {
                Stop("No stable lit main viewport; capture discarded.");
                return;
            }
            var milliseconds = Stopwatch.GetElapsedTime(_lastFrame, now).TotalMilliseconds;
            _lastFrame = now;
            _frames++;
            _total += milliseconds;
            var cpu = EntityManager.System<CinematicLightingSystem>().LastFrame.CpuMilliseconds;
            _cpuTotal += cpu;
            _maxCpu = Math.Max(_maxCpu, cpu);
            if (milliseconds > _worstFrame)
            {
                _worstFrame = milliseconds;
                _worstFrameCpu = cpu;
            }
            if (_samples.Count < 65536)
                _samples.Add(milliseconds);
            if (elapsed < _seconds + 2)
                return;

            _samples.Sort();
            var stats = EntityManager.System<CinematicLightingSystem>().LastFrame;
            var row = $"preset={_quality} stage_utc={_stageUtc:O} frames={_frames} samples={_samples.Count} mean_ms={F(_total / Math.Max(1, _frames))} "
                + $"p50_ms={F(Percentile(0.5))} p95_ms={F(Percentile(0.95))} p99_ms={F(Percentile(0.99))} "
                + $"cinematic_cpu_ms={F(_cpuTotal / Math.Max(1, _frames))} sources={stats.Sources} "
                + $"beams={stats.AtmosphereSources} targets={stats.Targets} target_bytes={stats.TargetBytes} "
                + $"worst_ms={F(_worstFrame)} worst_cmu_ms={F(_worstFrameCpu)} max_cmu_ms={F(_maxCpu)} "
                + $"exposure={F(_scene.Value.Exposure)} atmosphere={F(_scene.Value.Atmosphere)} native_scale={F(_scene.Value.NativeScale)}";
            _rows.Add(row);
            Log(row);
            if (_quality < 3)
                BeginStage(_quality + 1);
            else
            {
                WriteReport();
                _imagesOnly = true;
                Log("Measurements complete without image readbacks. Starting image-only stages.");
                BeginStage(0);
            }
        }
        catch (Exception exception)
        {
            Stop(exception.Message);
        }
    }

    private void BeginStage(int quality)
    {
        _quality = quality;
        _config.SetCVar(CCVars.CMUCinematicLighting, quality);
        _samples.Clear();
        _frames = 0;
        _total = 0;
        _cpuTotal = 0;
        _worstFrame = _worstFrameCpu = _maxCpu = 0;
        _started = _lastFrame = Stopwatch.GetTimestamp();
        _stageUtc = DateTime.UtcNow;
        Log(_imagesOnly ? $"Capturing preset {quality}; two-second warmup."
            : $"Warming preset {quality}; measuring {_seconds} seconds after two-second warmup.");
    }

    private void CaptureImages()
    {
        if (_eyes.MainViewport is not ScalingViewport viewport)
        {
            Stop("The main viewport changed during capture.");
            return;
        }
        _waitingForImage = true;
        if (_quality == 2)
            EntityManager.System<CinematicLightingSystem>().CaptureDebug(SaveDebugImage);
        viewport.Screenshot(_screenshot);
    }

    private CaptureState? ReadScene()
    {
        if (_eyes.MainViewport is not ScalingViewport { Eye: { } eye } viewport)
            return null;
        var controls = EntityManager.System<CinematicLightingSystem>().Controls;
        var ambient = _reviewMap is { } map && EntityManager.TryGetComponent<MapLightComponent>(map, out var light)
            ? light.AmbientLightColor : Color.Black;
        return new CaptureState(eye.Position, eye.Rotation, eye.Zoom, eye.Offset, viewport.PixelSize,
            eye.DrawLight, eye.DrawFov, _config.GetCVar(CCVars.CMUCinematicExposure),
            _config.GetCVar(CCVars.CMUCinematicAtmosphere), _config.GetCVar(CCVars.CMUCinematicAOIntensity),
            _config.GetCVar(CCVars.CMUCinematicAORadius), _config.GetCVar(CCVars.AmbientOcclusion),
            _config.GetCVar(CVars.LightResolutionScale), controls.View, controls.EnabledMask, ambient);
    }

    private readonly record struct CaptureState(MapCoordinates Position, Angle Rotation, Vector2 Zoom, Vector2 Offset,
        Vector2i PixelSize, bool DrawLight, bool DrawFov, float Exposure, float Atmosphere, float AOIntensity,
        float AORadius, bool AOEnabled, float NativeScale, CinematicDebugView DebugView, int EnabledEffects, Color Ambient);

    private double Percentile(double fraction) => _samples.Count == 0 ? 0
        : _samples[Math.Clamp((int) Math.Ceiling((_samples.Count - 1) * fraction), 0, _samples.Count - 1)];

    private static string F(double value) => value.ToString("F3", CultureInfo.InvariantCulture);

    private void SaveDebugImage(string name, Image<Rgba32> image)
    {
        using (image)
        {
            try
            {
                var path = _directory / $"high-{name}.png";
                using var output = _resources.UserData.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
                image.SaveAsPng(output);
            }
            catch (Exception exception)
            {
                Stop(exception.Message);
            }
        }
    }

    private void SaveScreenshot(Image<Rgba32> image)
    {
        using (image)
        {
            if (!_active)
                return;
            try
            {
                var path = _directory / $"quality-{_quality}.png";
                using var output = _resources.UserData.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
                image.SaveAsPng(output);
                Log($"Saved actual world viewport: {path}, {image.Width}x{image.Height}.");
                _advancePending = true;
            }
            catch (Exception exception)
            {
                Stop(exception.Message);
            }
        }
    }

    private void WriteReport()
    {
        using var output = _resources.UserData.Open(_directory / "frame-times.log",
            FileMode.Create, FileAccess.Write, FileShare.None);
        using var writer = new StreamWriter(output);
        writer.WriteLine("Actual native client frame intervals. Not isolated GPU timing. Keep camera and scene fixed.");
        writer.WriteLine("Two-second warmup per preset; up to 65536 interval samples per preset. Images use native viewport resolution.");
        writer.WriteLine("All timing stages precede image-only stages. No diagnostic readback/PNG encoding during measurements.");
        writer.WriteLine($"Fixed capture state: {_scene}");
        foreach (var row in _rows)
            writer.WriteLine(row);
        using var sourceFile = _resources.UserData.Open(_directory / "sources.csv",
            FileMode.Create, FileAccess.Write, FileShare.None);
        using var sources = new StreamWriter(sourceFile);
        sources.WriteLine("entity,profile,shape,x,y,emission_x,emission_y,r,g,b,energy,radius,direct,indirect,reflection,core,inner,outer,falloff,atmosphere,native_energy,mask_rotation_radians,mask");
        foreach (var source in EntityManager.System<CinematicLightingSystem>().Sources)
        {
            var profile = source.Profile;
            var emission = source.LuminousPosition ?? source.EmissionPosition;
            sources.WriteLine($"{source.Uid.Id},{profile.ID},{profile.Shape},{F(source.Position.X)},{F(source.Position.Y)},{F(emission.X)},{F(emission.Y)},{F(source.Color.R)},{F(source.Color.G)},{F(source.Color.B)},{F(source.Energy)},{F(source.Radius)},{F(profile.Spill)},{F(profile.Indirect)},{F(profile.Reflection)},{F(profile.Core)},{F(profile.InnerGlow)},{F(profile.OuterGlow)},{F(profile.Falloff)},{F(profile.Atmosphere)},{F(source.NativeEnergy)},{F(source.Rotation.Theta)},{source.Mask?.Id}");
        }
        if (EntityManager.System<CinematicLightingSystem>().CaptureFrame is { } frame)
        {
            using var viewFile = _resources.UserData.Open(_directory / "view.json",
                FileMode.Create, FileAccess.Write, FileShare.None);
            using var view = new StreamWriter(viewFile);
            view.WriteLine($"{{\"origin\":[{F(frame.WorldOrigin.X)},{F(frame.WorldOrigin.Y)}],\"x\":[{F(frame.WorldX.X)},{F(frame.WorldX.Y)}],\"y\":[{F(frame.WorldY.X)},{F(frame.WorldY.Y)}],\"size\":[{frame.Viewport.Size.X},{frame.Viewport.Size.Y}],\"map\":{(int) frame.Map}}}");
        }
    }

    public void Stop(string reason = "Stopped by command.")
    {
        if (!_active)
            return;
        _active = false;
        _config.SetCVar(CCVars.CMUCinematicLighting, _originalQuality);
        _config.SetCVar(CVars.DisplayVSync, _originalVsync);
        _config.SetCVar(CVars.DisplayMaxFPS, _originalMaxFps);
        Log($"{reason} Original graphics settings restored. Output: {_directory}");
    }

    private void Log(string message) => _logs.GetSawmill("cmu.cinematic-capture").Info(message);

    public override void Shutdown()
    {
        Stop("Client shutdown.");
        base.Shutdown();
    }
}

public sealed partial class CinematicLightingCaptureCommand : IConsoleCommand
{
    // Startup console commands run before the client has connected and created its ECS systems.
    internal static int PendingSeconds;
    [Dependency] private IEntityManager _entities = default!;
    public string Command => "cmu_cinematic_capture";
    public string Description => Loc.GetString("cmu-cinematic-capture-description");
    public string Help => Loc.GetString("cmu-cinematic-capture-help");
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 1 && args[0] == "stop")
        {
            PendingSeconds = 0;
            if (_entities.EntitySysManager.TryGetEntitySystem<CinematicLightingCaptureSystem>(out var capture))
                capture.Stop();
            return;
        }
        var seconds = 6;
        if (args.Length > 1 || args.Length == 1 && !int.TryParse(args[0], NumberStyles.Integer,
                CultureInfo.InvariantCulture, out seconds) || seconds is < 3 or > 60)
        {
            shell.WriteError(Help);
            return;
        }
        try
        {
            if (_entities.EntitySysManager.TryGetEntitySystem<CinematicLightingCaptureSystem>(out var capture))
                capture.Start(seconds);
            else
                PendingSeconds = seconds;
            shell.WriteLine(Loc.GetString("cmu-cinematic-capture-started"));
        }
        catch (Exception exception)
        {
            shell.WriteError(exception.Message);
        }
    }
}
