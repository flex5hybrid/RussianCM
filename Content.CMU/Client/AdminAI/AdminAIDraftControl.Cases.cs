using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Content.Shared.Administration.AdminAI;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Localization;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Client.CMU14.AdminAI;

public sealed partial class AdminAIDraftControl
{
    private readonly Button _collect = new() { Name = "ai-collect", Disabled = true };
    private readonly SpinBox _before = new() { Value = 5, IsValid = value => value is >= 0 and <= 30 };
    private readonly SpinBox _after = new() { Value = 2, IsValid = value => value is >= 0 and <= 30 };
    private readonly OptionButton _participant = new() { HorizontalExpand = true };
    private readonly TextEdit _report = new() { Editable = false, SetHeight = 130, HorizontalExpand = true, Name = "ai-report" };
    private readonly TextEdit _evidence = new() { Editable = false, SetHeight = 75, HorizontalExpand = true, Name = "ai-evidence" };
    private readonly Label _caseStatus = new() { ClipText = true };
    private readonly BoxContainer _logRows = new() { Orientation = LayoutOrientation.Vertical };
    private Guid _caseReport;
    private int _caseRound;
    private TimeSpan _caseExpiresAt;
    public event Action<AdminAIContextRequest>? RequestContext;

    private void InitializeCases()
    {
        var panel = new BoxContainer { Orientation = LayoutOrientation.Vertical };
        // Keep ordinary ahelp and compose controls available even when evidence is longer than the window.
        var viewport = new ScrollContainer { SetHeight = 230, HorizontalExpand = true };
        viewport.AddChild(panel); _tabs.AddChild(viewport);
        _tabs.SetTabTitle(1, Loc.GetString("cmu-admin-ai-tab-context")); _tabs.SetTabVisible(1, false);
        _collect.Text = Loc.GetString("cmu-admin-ai-collect");
        panel.AddChild(_collect); panel.AddChild(_caseStatus);
        var window = new BoxContainer { Orientation = LayoutOrientation.Horizontal };
        window.AddChild(new Label { Text = Loc.GetString("cmu-admin-ai-window-before") }); window.AddChild(_before);
        window.AddChild(new Label { Text = Loc.GetString("cmu-admin-ai-window-after") }); window.AddChild(_after);
        panel.AddChild(window); panel.AddChild(_participant); panel.AddChild(_report);
        var scroll = new ScrollContainer { SetHeight = 90, HorizontalExpand = true }; scroll.AddChild(_logRows);
        panel.AddChild(scroll); panel.AddChild(_evidence);
        _participant.AddItem(Loc.GetString("cmu-admin-ai-only-subject"), 0);
        _participant.SetItemMetadata(0, Guid.Empty);
        _participant.OnItemSelected += args => _participant.SelectId(args.Id);
        _collect.OnPressed += _ => CollectContext();
        ClearCase();
    }

    public void SetOtherPlayers(IEnumerable<(NetUserId Id, string Name)> players)
    {
        _participant.Clear();
        _participant.AddItem(Loc.GetString("cmu-admin-ai-only-subject"), 0); _participant.SetItemMetadata(0, Guid.Empty);
        var index = 1;
        foreach (var player in players.Where(player => player.Id != _channel).Take(256))
        { _participant.AddItem(player.Name, index); _participant.SetItemMetadata(index++, player.Id.UserId); }
        _participant.SelectId(0);
    }

    internal void CollectContext()
    {
        if (_collect.Disabled || _channel is not { } channel || _view == Guid.Empty) return;
        var other = _participant.SelectedMetadata is Guid id ? id : Guid.Empty;
        RequestContext?.Invoke(new(channel, _selection, _view, _caseRound, _before.Value, _after.Value,
            other == Guid.Empty ? Array.Empty<Guid>() : new[] { other }));
    }

    public void ReceiveCase(AdminAICaseState state)
    {
        if (state.Channel != _channel || state.SelectionId != _selection || state.ViewId != _view) return;
        _caseStatus.Text = Loc.GetString("cmu-admin-ai-case-" + state.Status);
        if (state.Status != "ready" || state.Seconds <= 0) { ClearCase(); return; }
        _caseExpiresAt = _timing.RealTime + TimeSpan.FromSeconds(Math.Clamp(state.Seconds, 0, 300));
        if (_caseReport == state.ReportId) return;
        _caseReport = state.ReportId; _report.TextRope = new Rope.Leaf(AdminAIDraftText.Sanitize(state.Report));
        _logRows.RemoveAllChildren();
        foreach (var row in state.Evidence.Take(128))
        {
            if (row.UtcTicks < DateTime.MinValue.Ticks || row.UtcTicks > DateTime.MaxValue.Ticks) continue;
            var date = new DateTime(row.UtcTicks, DateTimeKind.Utc).ToString("HH:mm:ss.fff 'UTC'", CultureInfo.InvariantCulture);
            var button = new Button { Text = $"{row.Round}:{row.LogId} — {date} — {row.Type}" };
            button.Label.ClipText = true;
            button.OnPressed += _ => _evidence.TextRope = new Rope.Leaf(AdminAIDraftText.Sanitize(row.Message));
            _logRows.AddChild(button);
        }
    }

    private void ClearCase()
    {
        _caseReport = Guid.Empty; _report.TextRope = new Rope.Leaf(""); _evidence.TextRope = new Rope.Leaf("");
        _logRows.RemoveAllChildren();
    }
}
