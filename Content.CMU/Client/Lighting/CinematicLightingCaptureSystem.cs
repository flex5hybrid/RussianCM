using System.Diagnostics;
using System.Globalization;
using System.IO;
using Content.Client.Viewport;
using Content.Shared.CCVar;
using Robust.Client.Graphics;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

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
    private readonly List<double> _samples = new(65536);
    private readonly List<string> _rows = new(4);
    private readonly CopyPixelsDelegate<Rgba32> _screenshot;
    private bool _active;
    private bool _waitingForScene;
    private bool _waitingForImage;
    // Viewport screenshot completion is delivered by the renderer's frame loop.
    private bool _advancePending;
    private int _quality;
    private int _seconds;
    private int _originalQuality;
    private int _originalMaxFps;
    private bool _originalVsync;
    private long _started;
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
                    BeginStage(0);
                }
                else if (elapsed > 180)
                    Stop("No lit main viewport became available within 180 seconds.");
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
                        WriteReport();
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
            var milliseconds = Stopwatch.GetElapsedTime(_lastFrame, now).TotalMilliseconds;
            _lastFrame = now;
            _frames++;
            _total += milliseconds;
            _cpuTotal += EntityManager.System<CinematicLightingSystem>().LastFrame.CpuMilliseconds;
            if (_samples.Count < 65536)
                _samples.Add(milliseconds);
            if (elapsed < _seconds + 2)
                return;

            _samples.Sort();
            var stats = EntityManager.System<CinematicLightingSystem>().LastFrame;
            var row = $"preset={_quality} frames={_frames} samples={_samples.Count} mean_ms={F(_total / Math.Max(1, _frames))} "
                + $"p50_ms={F(Percentile(0.5))} p95_ms={F(Percentile(0.95))} p99_ms={F(Percentile(0.99))} "
                + $"cinematic_cpu_ms={F(_cpuTotal / Math.Max(1, _frames))} sources={stats.Sources} "
                + $"beams={stats.AtmosphereSources} targets={stats.Targets} target_bytes={stats.TargetBytes}";
            _rows.Add(row);
            Log(row);
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
        _started = _lastFrame = Stopwatch.GetTimestamp();
        Log($"Warming preset {quality}; measuring {_seconds} seconds after two-second warmup.");
    }

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
        foreach (var row in _rows)
            writer.WriteLine(row);
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
