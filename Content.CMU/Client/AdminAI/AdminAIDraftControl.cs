using System;
using System.Linq;
using System.Collections.Generic;
using Content.Client.Administration.Systems;
using Content.Client.Guidebook;
using Content.Shared.Administration.AdminAI;
using Content.Shared.Guidebook;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.Timing;

namespace Content.Client.CMU14.AdminAI;

/// <summary>One private workspace per admin window, not one heavyweight editor per roster entry.</summary>
public sealed partial class AdminAIDraftControl : BoxContainer
{
    [Dependency] private IEntitySystemManager _systems = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private IGameTiming _timing = default!;
    private readonly Label _status = new() { HorizontalExpand = true, ClipText = true };
    private readonly Label _worker = new() { HorizontalExpand = true, ClipText = true };
    private readonly Button _prepare = new() { Name = "ai-prepare" }, _cancel = new() { Name = "ai-cancel" },
        _insert = new() { Name = "ai-insert" }, _sourceToggle = new() { Name = "ai-sources" };
    private readonly Button _expand = new() { ToggleMode = true, Name = "ai-expand" };
    private readonly TextEdit _editor = new() { SetHeight = 100, HorizontalExpand = true, Name = "ai-editor" };
    private readonly BoxContainer _body = new() { Orientation = LayoutOrientation.Vertical, Visible = false, Name = "ai-body" };
    private readonly BoxContainer _sources = new() { Orientation = LayoutOrientation.Vertical, Visible = false };
    private readonly TabContainer _tabs = new();
    private NetUserId? _channel;
    private Guid _selection, _view;
    private bool _hasDraft;
    private string _lastServerDraft = "";
    private AdminAISource[] _lastSources = Array.Empty<AdminAISource>();
    private TimeSpan _expiresAt;
    public event Action<string>? InsertDraft;
    public event Action<AdminAIDraftRequest>? RequestDraft;

    public AdminAIDraftControl()
    {
        IoCManager.InjectDependencies(this);
        Orientation = LayoutOrientation.Vertical; HorizontalExpand = true;
        _expand.Text = Loc.GetString("cmu-admin-ai-draft-title");
        _prepare.Text = Loc.GetString("cmu-admin-ai-draft-prepare");
        _cancel.Text = Loc.GetString("cmu-admin-ai-draft-cancel");
        _insert.Text = Loc.GetString("cmu-admin-ai-draft-insert");
        _sourceToggle.Text = Loc.GetString("cmu-admin-ai-draft-sources");
        AddChild(_expand); AddChild(_body);
        _body.AddChild(new Label { Text = Loc.GetString("cmu-admin-ai-draft-private"), ClipText = true });
        _body.AddChild(_status); _body.AddChild(_worker); _body.AddChild(_tabs);
        var draft = new BoxContainer { Orientation = LayoutOrientation.Vertical };
        _tabs.AddChild(draft); _tabs.SetTabTitle(0, Loc.GetString("cmu-admin-ai-tab-draft"));
        draft.AddChild(_editor);
        var actions = new BoxContainer { Orientation = LayoutOrientation.Horizontal, HorizontalExpand = true };
        foreach (var button in new[] { _prepare, _cancel, _insert, _sourceToggle }) actions.AddChild(button);
        draft.AddChild(actions);
        var scroll = new ScrollContainer { SetHeight = 110, HorizontalExpand = true, Visible = false };
        scroll.AddChild(_sources); draft.AddChild(scroll);
        InitializeCases();
        _expand.OnToggled += args => SetExpanded(args.Pressed);
        _prepare.OnPressed += _ => Send(AdminAIDraftAction.Prepare);
        _cancel.OnPressed += _ => Send(AdminAIDraftAction.Cancel);
        _sourceToggle.OnPressed += _ => { scroll.Visible = !scroll.Visible; _sources.Visible = scroll.Visible; };
        _insert.OnPressed += _ => InsertCurrentDraft();
        Clear();
    }

    public void Select(NetUserId? channel)
    {
        Close(); _channel = channel; _selection = Guid.NewGuid(); _view = Guid.Empty;
        if (channel != null) Send(AdminAIDraftAction.Open);
    }
    public void Open() { if (_channel is { } channel) Select(channel); }
    public bool NeedsView(NetUserId channel) => _channel == channel && _view == Guid.Empty;
    internal void SetExpanded(bool expanded)
    { _body.Visible = expanded; if (expanded && _view == Guid.Empty) Open(); }
    internal void InsertCurrentDraft()
    {
        if (!_hasDraft || _timing.RealTime >= _expiresAt) return;
        var text = AdminAIDraftText.Sanitize(Rope.Collapse(_editor.TextRope));
        if (text.Length == 0) return;
        // Insertion never invokes BwoinkSystem.Send or an admin command.
        InsertDraft?.Invoke(text.Replace('\n', ' ').Replace('\t', ' '));
    }
    public void Close()
    {
        if (_channel != null && _view != Guid.Empty) Send(AdminAIDraftAction.Close);
        _view = Guid.Empty; _selection = Guid.NewGuid(); Clear();
    }
    public void Reset() { _view = Guid.Empty; _selection = Guid.NewGuid(); Clear(); }
    public void Receive(AdminAIDraftState state)
    {
        if (_channel != state.Channel || _selection != state.SelectionId) return;
        _view = state.ViewId;
        _caseRound = state.Round; _tabs.SetTabVisible(1, state.CanInvestigate);
        _collect.Disabled = !state.CanInvestigate || _view == Guid.Empty || state.Status == AdminAIDraftStatus.Working;
        if (!state.CanInvestigate) ClearCase();
        var isDraft = state.Draft.Length > 0 && state.DraftSeconds > 0 && state.Status is not (AdminAIDraftStatus.Disabled or AdminAIDraftStatus.Denied or AdminAIDraftStatus.Stale or AdminAIDraftStatus.NoConversation);
        _expiresAt = _timing.RealTime + TimeSpan.FromSeconds(Math.Clamp(state.DraftSeconds, 0, 300));
        _status.Text = Loc.GetString("cmu-admin-ai-draft-status-" + state.Status.ToString().ToLowerInvariant());
        _worker.Text = Loc.GetString("cmu-admin-ai-draft-worker", ("state", state.WorkerState), ("pending", state.Pending));
        _worker.ToolTip = isDraft && state.Sources.Length == 0 ? Loc.GetString("cmu-admin-ai-draft-no-sources") : _worker.Text;
        if (state.Draft != _lastServerDraft || !isDraft)
        {
            _editor.TextRope = new Rope.Leaf(isDraft ? AdminAIDraftText.Sanitize(state.Draft) : "");
            _lastServerDraft = state.Draft;
        }
        _hasDraft = isDraft; _insert.Disabled = !isDraft;
        _prepare.Text = Loc.GetString(isDraft ? "cmu-admin-ai-draft-regenerate" : "cmu-admin-ai-draft-prepare");
        // Temporary protection/worker failures must allow a manual retry. The server rechecks admission;
        // this does not poll, infer automatically or permit duplicate requests while one is running.
        _prepare.Disabled = _view == Guid.Empty || state.Status is AdminAIDraftStatus.Working
            or AdminAIDraftStatus.Disabled or AdminAIDraftStatus.Denied;
        _cancel.Disabled = state.Status != AdminAIDraftStatus.Working;
        _sourceToggle.Disabled = state.Sources.Length == 0;
        if (_lastSources.Select(source => (source.GuideId, source.Title, source.Version, source.Fragment))
            .SequenceEqual(state.Sources.Select(source => (source.GuideId, source.Title, source.Version, source.Fragment)))) return;
        _lastSources = state.Sources;
        _sources.RemoveAllChildren();
        foreach (var source in state.Sources.Take(3))
        {
            var title = Loc.TryGetString(source.Title, out var localized) ? localized : source.Title;
            var link = new Button { Text = title, ToolTip = source.Version };
            link.Label.ClipText = true;
            link.OnPressed += _ =>
            {
                if (_prototypes.HasIndex<GuideEntryPrototype>(source.GuideId))
                    _systems.GetEntitySystem<GuidebookSystem>().OpenHelp(new List<ProtoId<GuideEntryPrototype>> { source.GuideId });
            };
            _sources.AddChild(link);
            var fragment = new RichTextLabel(); fragment.SetMessage(FormattedMessage.FromUnformatted(source.Fragment));
            _sources.AddChild(fragment);
        }
    }
    private void Send(AdminAIDraftAction action)
    {
        if (_channel is not { } channel) return;
        RequestDraft?.Invoke(new(channel, _selection, _view, action));
    }
    private void Clear()
    {
        _hasDraft = false; _lastServerDraft = ""; _editor.TextRope = new Rope.Leaf("");
        _status.Text = Loc.GetString("cmu-admin-ai-draft-status-noconversation"); _worker.Text = "";
        _prepare.Disabled = true; _insert.Disabled = true; _cancel.Disabled = true; _sourceToggle.Disabled = true;
        _sources.RemoveAllChildren();
        _lastSources = Array.Empty<AdminAISource>();
        _collect.Disabled = true; _tabs.SetTabVisible(1, false);
        ClearCase();
    }
    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        if (_hasDraft && _timing.RealTime >= _expiresAt) Clear();
        if (_caseReport != Guid.Empty && _timing.RealTime >= _caseExpiresAt) ClearCase();
    }
}
