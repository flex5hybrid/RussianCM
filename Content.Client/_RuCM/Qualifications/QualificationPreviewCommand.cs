using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Content.Shared._RuCM.Qualifications;
using Content.Shared.Administration;
using Content.Shared.Roles;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;

namespace Content.Client._RuCM.Qualifications;

/// <summary>Local visual QA only. Synthetic data and no network requests or persistence.</summary>
[AnyCommand]
public sealed partial class QualificationPreviewCommand : LocalizedCommands
{
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private IClyde _clyde = default!;
    [Dependency] private IResourceManager _resources = default!;
    private QualificationWindow? _window;
    public override string Command => "qualificationpreview";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length > 1 || args.Length == 1 && args[0] is not "capture" and not "off") { shell.WriteError(Help); return; }
        _window?.Dispose(); _window = null;
        if (args.Length == 1 && args[0] == "off") return;
        var view = CreateView(_prototypes);
        _window = new QualificationWindow((_, _) => { view.Error = "permission"; _window?.Update(view); }, preview: true);
        _window.Update(view);
        _window.Title = Loc.GetString("rucm-qualifications-preview-title");
        _window.OpenCentered();
        shell.WriteLine(Loc.GetString("rucm-qualifications-local-preview"));
        if (args.Length == 1)
            _window.Contents.AddChild(new CaptureControl(_window, _clyde, _resources, shell));
    }

    public static QualificationView CreateView(IPrototypeManager prototypes)
    {
        var player = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var viewer = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var now = DateTimeOffset.Parse("2026-10-03T12:00:00+03:00");
        var context = new TrainingContext(viewer, "Инструктор / Instructor", "AU14JobGOVFORPlatCo", 42, "preview", now);
        var view = new QualificationView { Viewer = viewer, Target = player, Management = true, Instructor = true,
            Officer = true, Available = true, OnlinePlayers = new() { [player] = "Алексей Морозов / Alexey Morozov", [viewer] = "Инструктор / Instructor" } };
        foreach (var (id, itemIds) in new[] { ("enlisted", new[] { "firearms", "tactics", "first_aid", "communications" }),
                     ("medical", new[] { "theory", "safety", "practice" }), ("field_engineering", new[] { "theory", "safety", "practice" }) })
        {
            view.Store.Definitions[id] = new() { Id = id, Name = "rucm-qualifications-definition-" + id,
                Description = "rucm-qualifications-training-help",
                Items = itemIds.Select((item, i) => new ChecklistItem { Id = item, Name = "rucm-qualifications-item-" + item,
                    Description = "rucm-qualifications-training-help", SortOrder = i }).ToList() };
        }
        view.Store.Players[player] = new() { Player = player, FirstTrainingAt = now,
            Progress = new() { ["enlisted"] = new() { ["firearms"] = new("enlisted", "firearms", context, "preview"),
                ["tactics"] = new("enlisted", "tactics", context, "preview") } } };
        view.Store.Notes.Add(new(Guid.NewGuid(), player, context, Robust.Shared.Localization.Loc.GetString("rucm-qualifications-demo-note")));
        foreach (var job in prototypes.EnumeratePrototypes<JobPrototype>().Where(j => j.ID.StartsWith("AU14JobGOVFOR")))
        {
            view.Jobs[job.ID] = job.LocalizedName;
            view.Store.Roles[job.ID] = new() { JobId = job.ID, MinimumLevel = MilitaryLevel.Enlisted,
                Professional = job.ID.Contains("Medic") ? new() { "medical" } : new() };
        }
        view.Metrics["level_None"] = 12;
        view.Metrics["professional_medical"] = 4;
        return view;
    }

    private sealed class CaptureControl : Control
    {
        private readonly QualificationWindow _window;
        private readonly IClyde _clyde;
        private readonly IResourceManager _resources;
        private readonly IConsoleShell _shell;
        private readonly string[] _pages = ["dossier", "training", "role-access", "player-management", "definitions"];
        private int _frames;
        private int _index;
        private bool _saving;
        public CaptureControl(QualificationWindow window, IClyde clyde, IResourceManager resources, IConsoleShell shell)
        { _window = window; _clyde = clyde; _resources = resources; _shell = shell; MouseFilter = MouseFilterMode.Ignore; }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);
            if (_saving || ++_frames < 45) return;
            if (_index >= _pages.Length) { _shell.ExecuteCommand("quit"); return; }
            _saving = true;
            var page = _pages[_index];
            var position = _window.GlobalPixelPosition;
            var size = _window.PixelSize;
            var viewport = _clyde.ScreenSize;
            if (position.X < 0 || position.Y < 0 || position.X + size.X > viewport.X || position.Y + size.Y > viewport.Y)
                _shell.WriteError($"Qualification preview outside viewport: position={position}, size={size}, viewport={viewport}");
            else
                _shell.WriteLine($"Qualification preview bounds OK: position={position}, size={size}, viewport={viewport}");
            _clyde.Screenshot(ScreenshotType.Final, screenshot =>
            {
                var dir = new ResPath("/QualificationPreview");
                _resources.UserData.CreateDir(dir);
                using var file = _resources.UserData.Open(dir / (page + ".png"), FileMode.Create, FileAccess.Write, FileShare.None);
                screenshot.SaveAsPng(file);
                _shell.WriteLine("QualificationPreview/" + page + ".png");
                _index++;
                _frames = 0;
                _saving = false;
                if (_index < _pages.Length) _window.ShowPreviewPage(_pages[_index]);
            });
        }
    }
}
