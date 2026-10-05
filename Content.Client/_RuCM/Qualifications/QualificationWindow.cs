using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.Client.Lobby.UI;
using Content.Client.CMU14.Interface;
using Content.Client.Stylesheets;
using Content.Shared._RuCM.Qualifications;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using Robust.Shared.Timing;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;

namespace Content.Client._RuCM.Qualifications;

/// <summary>Private training terminal shared by EUI and BUI. Authorization remains server-owned.</summary>
public sealed partial class QualificationWindow : DefaultWindow
{
    private readonly Action<QualificationAction, QualificationRequest> _send;
    private readonly BoxContainer _root = Column();
    private readonly BoxContainer _content = Column();
    private readonly BoxContainer _navigation = Column();
    private readonly RichTextLabel _feedback = new() { HorizontalExpand = true };
    private readonly Dictionary<string, string> _drafts = new();
    private readonly Dictionary<string, bool> _checks = new();
    private readonly List<(Button Button, Func<bool> Enabled)> _actions = new();
    private QualificationView _view = new();
    private string _page = "dossier";
    private string _selectedDefinition = "enlisted";
    private string _selectedJob = "";
    private string _scope = "";
    private bool _confirmed;
    private bool _pending;
    private readonly bool _preview;
    private QualificationAction _submittedAction;
    private string _reason = "";
    private Guid _draftTarget;
    private bool _deferredCenter;
    private int _centerFrames;
    private bool _receivedView;
    private Guid _pendingRequest;
    private readonly Font _controlFont = IoCManager.Resolve<IResourceCache>().NotoStack(size: 14);

    public QualificationWindow(Action<QualificationAction, QualificationRequest> send, bool preview = false)
    {
        _send = send;
        _preview = preview;
        Title = L("title");
        MinSize = new(760, 520);
        var viewport = IoCManager.Resolve<IUserInterfaceManager>().WindowRoot.Size;
        SetSize = viewport.X > 0 && viewport.Y > 0
            ? new(Math.Max(760, Math.Min(1080, viewport.X - 40)), Math.Max(520, Math.Min(740, viewport.Y - 40)))
            : new(1080, 740);
        _root.SeparationOverride = 10;
        _root.Margin = new Thickness(12);
        _root.VerticalExpand = true;
        var screen = new PanelContainer { HorizontalExpand = true, VerticalExpand = true,
            StyleClasses = { StyleNano.StyleClassCrtScreenPanel } };
        screen.AddChild(_root);
        Contents.AddChild(screen);
        Contents.AddChild(new CrtScreenControl { Source = screen, HorizontalExpand = true, VerticalExpand = true,
            Roll = false, Grain = false, Curvature = 0, Vignette = 0, ArtifactAmount = 0 });
        Render();
    }

    private static string L(string key) => Loc.GetString("rucm-qualifications-" + key);
    private static string Name(string name) => Loc.TryGetString(name, out var translated) ? translated : name;
    private string DefinitionName(string id) => id == "instructor_accreditation" ? L("instructor-accreditation") : id == "recruit" ? L("level-none") : Name(_view.Store.Definitions.GetValueOrDefault(id)?.Name ?? id);
    private string AccountName(Guid id) => id == Guid.Empty ? L("server-console") :
        _view.OnlinePlayers.GetValueOrDefault(id, _view.AccountNames.GetValueOrDefault(id, L("unknown-account")));
    private string TargetName => AccountName(_view.Target);
    private string Author(TrainingContext context) => AccountName(context.Actor) +
        (context.Character.Length == 0 ? "" : " · " + context.Character);
    private PlayerTrainingState? State => _view.Store.Players.GetValueOrDefault(_view.Target);
    private bool CanTrain => _view.Instructor || _view.Management;
    private bool CanSuspend => _view.Officer || _view.CommandingOfficer || _view.Administrator;
    private bool Ready => _view.Available && !_pending && !_preview;
    private bool Confirmed => Ready && _confirmed && !string.IsNullOrWhiteSpace(_reason);
    private bool TrainingTargetReady => !_view.TargetSynthetic && _view.Target != _view.Viewer && (_view.Management || _view.InstructorOnDuty && _view.TargetOnline);
    private static BoxContainer Column() => new() { Orientation = BoxContainer.LayoutOrientation.Vertical,
        HorizontalExpand = true, SeparationOverride = 8 };
    private static BoxContainer Row() => new() { HorizontalExpand = true, SeparationOverride = 10 };

    // All player-authored values use unformatted messages; notes cannot inject UI markup.
    private static RichTextLabel Text(BoxContainer box, string text, Color? color = null)
    {
        var label = new RichTextLabel { HorizontalExpand = true, LineHeightScale = 1.15f };
        label.SetMessage(FormattedMessage.FromUnformatted(text), color ?? StyleNano.CrtGreenSoft);
        box.AddChild(label);
        return label;
    }

    private static void Heading(BoxContainer box, string title)
    {
        // A heading must wrap too: localized role names can be longer than a narrow viewport.
        var heading = Text(box, title, StyleNano.CrtGreen);
        heading.SetMessage(FormattedMessage.FromMarkupPermissive("[font size=16][bold]" + FormattedMessage.EscapeText(title) + "[/bold][/font]"), tagsAllowed: null, defaultColor: StyleNano.CrtGreen);
        heading.Margin = new Thickness(0, 2, 0, 2);
    }

    private static BoxContainer Card(BoxContainer parent, string title, string help = "", bool warning = false)
    {
        var panel = new PanelContainer { HorizontalExpand = true, PanelOverride = new CrtStyleBox
        {
            BackgroundColor = StyleNano.CrtPanelBackgroundAlt,
            BorderColor = (warning ? StyleNano.CrtWarning : StyleNano.CrtGreen).WithAlpha(0.3f),
            CornerColor = (warning ? StyleNano.CrtWarning : StyleNano.CrtGreen).WithAlpha(0.6f),
            BorderThickness = new Thickness(1), CornerLength = 7,
        } };
        var body = Column();
        body.Margin = new Thickness(14, 10);
        panel.AddChild(body);
        parent.AddChild(panel);
        if (title.Length > 0) Heading(body, title);
        if (help.Length > 0) Text(body, help);
        return body;
    }

    private void PageHeading(string title, string help)
    {
        Heading(_content, title);
        Text(_content, help);
        _content.AddChild(new PanelContainer { MinHeight = 1, StyleClasses = { StyleNano.StyleClassCrtDivider }, Margin = new Thickness(0, 2, 0, 4) });
    }

    private LineEdit Field(BoxContainer box, string key, string caption, string value = "", string help = "", string placeholder = "")
    {
        Text(box, caption, StyleNano.CrtGreen);
        var draftKey = _scope + "/" + key;
        var edit = new LineEdit { Text = _drafts.GetValueOrDefault(draftKey, value), PlaceHolder = placeholder,
            HorizontalExpand = true, MinHeight = 34, ToolTip = help, Name = key };
        edit.OnTextChanged += _ => { _drafts[draftKey] = edit.Text; RefreshActions(); };
        box.AddChild(edit);
        if (help.Length > 0) Text(box, help);
        return edit;
    }

    private CheckBox Check(BoxContainer box, string key, string caption, bool value)
    {
        var draftKey = _scope + "/" + key;
        var check = new CheckBox { Text = caption, Pressed = _checks.GetValueOrDefault(draftKey, value),
            MinHeight = 30, HorizontalExpand = true, Name = key };
        check.Label.FontOverride = _controlFont;
        check.Label.ClipText = true; check.Label.HorizontalExpand = true;
        check.OnToggled += _ => { _checks[draftKey] = check.Pressed; RefreshActions(); };
        box.AddChild(check);
        return check;
    }

    private Button Button(BoxContainer box, string caption, Action action, Func<bool>? enabled = null, bool danger = false, string name = "")
    {
        var button = new Button { Text = caption, HorizontalExpand = true, MinHeight = 36, Name = name };
        button.Label.HorizontalExpand = true;
        button.Label.ClipText = true;
        button.Label.FontOverride = _controlFont;
        if (danger) button.Label.FontColorOverride = StyleNano.CrtWarning;
        button.OnPressed += _ => action();
        box.AddChild(button);
        if (enabled != null) _actions.Add((button, enabled));
        return button;
    }

    private OptionButton Select(BoxContainer box, IEnumerable<KeyValuePair<string, string>> choices, string current, Action<string> change)
    {
        var entries = choices.OrderBy(x => x.Value).ToArray();
        var select = new OptionButton { HorizontalExpand = true, MinHeight = 34 };
        foreach (var label in Descendants(select).OfType<Label>()) label.ClipText = true;
        for (var i = 0; i < entries.Length; i++)
        {
            select.AddItem(entries[i].Value, i);
            if (entries[i].Key == current) select.SelectId(i);
        }
        select.Disabled = entries.Length == 0;
        select.OnItemSelected += e => { select.SelectId(e.Id); change(entries[e.Id].Key); };
        box.AddChild(select);
        return select;
    }

    private void RefreshActions()
    {
        foreach (var (button, enabled) in _actions) button.Disabled = !enabled();
        foreach (var edit in Descendants(_root).OfType<LineEdit>()) edit.Editable = !_pending;
        foreach (var check in Descendants(_root).OfType<CheckBox>()) check.Disabled = _pending;
        foreach (var select in Descendants(_root).OfType<OptionButton>()) select.Disabled = _pending || select.ItemCount == 0;
    }

    private QualificationRequest Request(string qualification = "") => new()
    { Target = _view.Target, Qualification = qualification, Revision = _view.Store.Revision, Reason = _reason };

    private void Send(QualificationAction action, QualificationRequest request)
    {
        if (_pending || _preview) return;
        _pending = true;
        request.RequestId = _pendingRequest = Guid.NewGuid();
        _submittedAction = action;
        Feedback(L("sending"));
        RefreshActions();
        _send(action, request);
    }

    private void Feedback(string message, bool error = false) => _feedback.SetMessage(
        FormattedMessage.FromUnformatted(message), error ? StyleNano.CrtWarning : StyleNano.CrtGreenSoft);

    public void Update(QualificationView view)
    {
        if (!_receivedView && (view.Management || view.Instructor || view.Officer || view.CommandingOfficer))
            _page = view.Target == view.Viewer ? "players" : view.Management || view.Instructor ? "training" : "dossier";
        _receivedView = true;
        var targetChanged = view.Target != _draftTarget;
        _draftTarget = view.Target;
        // A successful mutation reloads authoritative values. Viewing/refreshing preserves unsaved drafts.
        var acknowledged = _pending && view.ResponseId == _pendingRequest;
        var saved = acknowledged && view.Error.Length == 0 && _submittedAction is not QualificationAction.View and not QualificationAction.MigrationPreview;
        if (acknowledged && view.Error.Length == 0 && _submittedAction == QualificationAction.View && _page == "players")
            _page = view.Management || view.Instructor ? "training" : "dossier";
        if (targetChanged || saved && _submittedAction is not QualificationAction.Complete and not QualificationAction.Note)
        { _drafts.Clear(); _checks.Clear(); _reason = ""; }
        else if (saved && _submittedAction == QualificationAction.Note) _drafts.Remove(_scope + "/note-text");
        _view = view;
        if (acknowledged) _pending = false;
        _confirmed = false;
        Render();
        if (saved) Feedback(L("saved"));
    }

    private void Navigate(string page)
    {
        _page = page;
        _confirmed = false;
        Render();
    }

    /// <summary>Local preview navigation uses the same permission-filtered pages as the real window.</summary>
    internal void ShowPreviewPage(string page) => Navigate(page);

    protected override void Opened()
    {
        base.Opened();
        _deferredCenter = true;
        _centerFrames = 3;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        // Console previews can open before the native viewport has its first layout.
        if (!_deferredCenter || Parent == null || Parent.Width <= 0 || Parent.Height <= 0) return;
        if (_centerFrames == 3)
            SetSize = new(Math.Max(760, Math.Min(1080, Parent.Width - 60)), Math.Max(520, Math.Min(740, Parent.Height - 60)));
        if (--_centerFrames > 0) return;
        // Use arranged size after styles and DPI scaling settle, rather than an earlier desired size.
        LayoutContainer.SetPosition(this, Vector2.Max(Vector2.Zero, (Parent.Size - Size) / 2));
        _deferredCenter = false;
    }

    private void Render()
    {
        _feedback.Orphan(); _navigation.Orphan(); _content.Orphan();
        _root.RemoveAllChildren(); _navigation.RemoveAllChildren(); _content.RemoveAllChildren(); _actions.Clear();
        if (!Pages().Any(p => p.Id == _page)) _page = "dossier";
        _scope = _view.Target + "/" + _page;

        if (_preview) Text(_root, L("local-preview"), StyleNano.CrtWarning);
        var header = Row(); _root.AddChild(header);
        var identity = Column(); header.AddChild(identity);
        Heading(identity, L("terminal-heading"));
        Text(identity, L("terminal-subtitle"));
        var status = Column(); status.HorizontalExpand = false; header.AddChild(status);
        Text(status, _view.Available ? L("link-online") : L("link-offline"), _view.Available ? StyleNano.CrtGreen : StyleNano.CrtWarning);
        Button(status, L("refresh"), () => { if (_preview) Render(); else Send(QualificationAction.View, Request()); }, () => !_pending, name: "refresh");

        var target = Row(); _root.AddChild(target);
        var targetInfo = Column(); target.AddChild(targetInfo);
        Text(targetInfo, L("selected-person") + " · " + TargetName + " · " + L(_view.TargetOnline ? "person-online" : "person-offline"), StyleNano.CrtGreen);
        Text(targetInfo, L("access-scope-" + (_view.Management ? "management" : CanTrain ? "instructor" : CanSuspend ? "officer" : "player")));
        if (_view.Management || _view.Instructor || CanSuspend)
        {
            var picker = Row(); target.AddChild(picker);
            Button(picker, L("choose-player"), () => Navigate("players"), () => !_pending, name: "choose-player");
            Button(picker, L("open-self"), () => { var req = Request(); req.Target = _view.Viewer; Send(QualificationAction.View, req); },
                () => !_pending && _view.Target != _view.Viewer, name: "open-self");
        }

        var body = Row(); body.VerticalExpand = true; _root.AddChild(body);
        var navPanel = new PanelContainer { MinWidth = 220, MaxWidth = 250, HorizontalExpand = false,
            StyleClasses = { StyleNano.StyleClassCrtInsetPanel } };
        var navScroll = new ScrollContainer { HScrollEnabled = false, VerticalExpand = true };
        _navigation.Margin = new Thickness(8); navScroll.AddChild(_navigation); navPanel.AddChild(navScroll); body.AddChild(navPanel);
        foreach (var page in Pages())
        {
            var id = page.Id;
            if (id == "player-management") Text(_navigation, L("management"), StyleNano.CrtGreen);
            var nav = Button(_navigation, L("nav-" + id), () => Navigate(id), () => !_pending, name: "nav-" + id);
            nav.ToggleMode = true; nav.Pressed = id == _page;
            nav.ToolTip = L("help-" + id);
            nav.MinHeight = 42;
            // Native button labels clip long localized captions. Navigation uses wrapped body text.
            nav.Label.Visible = false;
            var caption = Column(); caption.Margin = new Thickness(6, 4);
            Text(caption, L("nav-" + id), id == _page ? StyleNano.CrtGreen : StyleNano.CrtGreenSoft);
            nav.AddChild(caption);
        }
        var scroll = new ScrollContainer { HScrollEnabled = false, HorizontalExpand = true, VerticalExpand = true, Name = "page-scroll" };
        _content.Margin = new Thickness(2, 0, 10, 6); scroll.AddChild(_content); body.AddChild(scroll);
        PageHeading(L("nav-" + _page), L("help-" + _page));
        switch (_page)
        {
            case "players": RenderPlayers(); break;
            case "dossier": RenderDossier(); break;
            case "role-access": RenderRoles(); break;
            case "training": RenderTraining(); break;
            case "suspensions": RenderSuspensions(); break;
            case "recruit-reset": RenderRecruitReset(); break;
            case "player-management": RenderPlayerManagement(); break;
            case "role-settings": RenderRoleSettings(); break;
            case "definitions": RenderDefinitions(); break;
            case "accreditation": RenderAccreditation(); break;
            case "permissions": RenderPermissions(); break;
            case "migration": RenderMigration(); break;
            case "metrics": RenderMetrics(); break;
            case "audit": RenderAudit(); break;
        }
        var footer = Column(); _root.AddChild(footer); footer.AddChild(_feedback);
        if (_pending) Feedback(L("sending"));
        else if (!_view.Available) Feedback(L("storage-unavailable"), true);
        else if (_view.Error == "saved") Feedback(L("saved"));
        else if (_view.Error.Length > 0) Feedback(Loc.GetString("rucm-qualifications-error", ("error", Name("rucm-qualifications-error-" + _view.Error))), true);
        else Feedback(L("footer-hint"));
        CrtLobbyTheme.ApplyWindow(this, useCrtTypography: false);
        RefreshActions();
    }

    private IEnumerable<(string Id, bool Available)> Pages()
    {
        if (_view.Management || _view.Instructor || CanSuspend) yield return ("players", true);
        yield return ("dossier", true);
        yield return ("role-access", true);
        if (CanTrain) yield return ("training", true);
        if (CanSuspend) yield return ("suspensions", true);
        if (_view.Management || CanSuspend) yield return ("recruit-reset", true);
        if (!_view.Management) yield break;
        foreach (var id in new[] { "player-management", "role-settings", "definitions", "accreditation", "permissions", "migration", "metrics", "audit" })
            yield return (id, true);
    }

    private void RenderPlayers()
    {
        var selector = Card(_content, L("online-roster"), L("selection-help"));
        var search = Field(selector, "player-search", L("nickname-search"), placeholder: L("nickname-placeholder"));
        var list = Column(); selector.AddChild(list);
        void Populate()
        {
            // Drop detached buttons so repeated filtering does not retain handlers or duplicate actions.
            foreach (var old in Descendants(list).OfType<Button>().ToArray()) _actions.RemoveAll(a => a.Button == old);
            list.RemoveAllChildren();
            var matches = _view.OnlinePlayers.Where(p => p.Value.Contains(search.Text.Trim(), StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p.Key == _view.Viewer).ThenBy(p => p.Value).ToArray();
            Text(list, Loc.GetString("rucm-qualifications-roster-count", ("count", matches.Length)));
            foreach (var (id, name) in matches.Take(50))
            {
                var card = Card(list, name);
                if (id == _view.Viewer) Text(card, L("roster-self"));
                Button(card, L("open-player"), () => { var req = Request(); req.Target = id; Send(QualificationAction.View, req); },
                    () => !_pending, name: "select-player-" + id);
            }
            if (matches.Length == 0) Text(list, L("no-players"), StyleNano.CrtWarning);
            if (matches.Length > 50) Text(list, L("roster-limit"));
            CrtLobbyTheme.Apply(list, useCrtTypography: false); RefreshActions();
        }
        search.OnTextChanged += _ => Populate(); Populate();
        if (!_view.Management) return;
        var offline = Card(_content, L("offline-lookup"), L("offline-lookup-help"));
        var nickname = Field(offline, "offline-nickname", L("account-nickname"), placeholder: L("nickname-placeholder"));
        Button(offline, L("find-account"), () => { var req = Request(); req.TargetName = nickname.Text.Trim(); Send(QualificationAction.View, req); },
            () => Ready && !string.IsNullOrWhiteSpace(nickname.Text), name: "find-account");
        var advanced = Column(); advanced.Visible = false; offline.AddChild(advanced);
        Button(offline, L("technical-account"), () => advanced.Visible = !advanced.Visible);
        var idField = Field(advanced, "target-account", L("account-id"), placeholder: L("uuid-placeholder"));
        Button(advanced, L("open-player"), () => { var req = Request(); req.Target = Guid.Parse(idField.Text); Send(QualificationAction.View, req); },
            () => Ready && Guid.TryParse(idField.Text, out var id) && id != Guid.Empty);
    }

    private QualificationDefinition? DefinitionPicker(BoxContainer box)
    {
        var definitions = _view.Store.Definitions.Values.Where(d => d.Enabled || _view.Management).ToArray();
        if (!definitions.Any(d => d.Id == _selectedDefinition)) _selectedDefinition = definitions.FirstOrDefault()?.Id ?? "";
        Select(box, definitions.Select(d => new KeyValuePair<string, string>(d.Id, Name(d.Name))), _selectedDefinition,
            id => { _selectedDefinition = id; _confirmed = false; Render(); });
        return _view.Store.Definitions.GetValueOrDefault(_selectedDefinition);
    }

    private static void Badge(BoxContainer box, string caption, Color color) => Text(box, "[ " + caption + " ]", color);

    private void RenderDossier()
    {
        var summary = Card(_content, L("service-record"));
        if (_view.TargetSynthetic)
        {
            Heading(summary, L("synthetic-excluded"));
            Text(summary, L("synthetic-excluded-help"));
            return;
        }
        summary.ToolTip = L("level-help");
        var stats = Row(); summary.AddChild(stats);
        var level = Column(); stats.AddChild(level); Text(level, L("level"));
        Heading(level, L("level-" + QualificationRules.EffectiveLevel(State).ToString().ToLowerInvariant()));
        var grants = Column(); stats.AddChild(grants); Text(grants, L("active-qualifications"));
        Heading(grants, (State?.Grants.Values.Count(g => g.Status == QualificationStatus.Active) ?? 0).ToString());
        var pending = _view.Store.Suspensions.Count(s => s.Target == _view.Target && s.Status is "pending" or "active");
        if (pending > 0) Text(summary, L("suspension-notice"), StyleNano.CrtWarning);

        var checklist = Card(_content, L("training-track"));
        var definition = DefinitionPicker(checklist);
        if (definition == null) { Text(checklist, L("empty-definitions")); return; }
        if (definition.Description.Length > 0 && definition.Description != "rucm-qualifications-training-help") Text(checklist, Name(definition.Description));
        var grant = State?.Grants.GetValueOrDefault(definition.Id);
        Badge(checklist, grant == null ? L("not-certified") : L("status-" + grant.Status.ToString().ToLowerInvariant()),
            grant?.Status == QualificationStatus.Active ? StyleNano.CrtGreen : StyleNano.CrtWarning);
        if (grant != null) Text(checklist, L("issued-by") + " " + Author(grant.Issuer) + " · " + grant.Issuer.At.ToLocalTime().ToString("g"));
        var items = definition.Items.Where(i => i.Enabled).OrderBy(i => i.SortOrder).ToArray();
        var required = items.Count(i => i.Required);
        var completed = items.Count(i => i.Required && State?.Progress.GetValueOrDefault(definition.Id)?.ContainsKey(i.Id) == true);
        Text(checklist, Loc.GetString("rucm-qualifications-progress-summary", ("done", completed), ("total", required)));
        checklist.AddChild(new ProgressBar { MinValue = 0, MaxValue = Math.Max(required, 1), Value = completed, MinHeight = 8,
            HorizontalExpand = true, StyleClasses = { StyleNano.StyleClassCrtProgressBar } });
        Text(checklist, L("checklist-help"));
        foreach (var item in items)
        {
            var completion = State?.Progress.GetValueOrDefault(definition.Id)?.GetValueOrDefault(item.Id);
            var card = Card(checklist, (completion == null ? "[ ] " : "[+] ") + Name(item.Name));
            Text(card, item.Required ? L("required-step") : L("optional-step"), completion == null ? StyleNano.CrtWarning : StyleNano.CrtGreen);
            if (ItemHelp(item).Length > 0) Text(card, ItemHelp(item));
            AddTrainingMaterials(card, definition.Id, item.Id);
            if (completion != null) Text(card, L("completed-by") + " " + Author(completion.By) + " · " + completion.By.At.ToLocalTime().ToString("g"));
        }
        if (items.Length == 0) Text(checklist, L("empty-checklist"));
        var suspensions = _view.Store.Suspensions.Where(s => s.Target == _view.Target && s.Status is "pending" or "active").ToArray();
        foreach (var suspension in suspensions)
        {
            var notice = Card(_content, DefinitionName(suspension.Qualification), L("appeal"), true);
            Badge(notice, L("suspension-" + suspension.Status), StyleNano.CrtWarning);
            Text(notice, suspension.Reason);
        }
    }

    private void RenderRoles()
    {
        Text(_content, L(_view.Enforcing ? "gate-insurgency" : "gate-timers"), StyleNano.CrtGreen);
        Text(_content, L(_view.Enforcing ? "gate-insurgency-help" : "gate-timers-help"));
        Text(_content, L("existing-requirements"));
        var search = Field(_content, "role-search", L("search"), placeholder: L("role-search-placeholder"));
        var onlyMissing = Check(_content, "only-missing", L("only-missing"), false);
        var list = Column(); _content.AddChild(list);
        void Populate()
        {
            list.RemoveAllChildren();
            var count = 0;
            foreach (var role in _view.Store.Roles.Values.Where(r => !r.Synthetic && (r.Enabled || QualificationRules.IsDrillInstructor(r.JobId))).OrderBy(r => _view.Jobs.GetValueOrDefault(r.JobId, r.JobId)))
            {
                var name = _view.Jobs.GetValueOrDefault(role.JobId, role.JobId);
                var eligibility = QualificationRules.CanTakeJob(State, role, _view.Store.Instructors.GetValueOrDefault(_view.Target));
                if (!name.Contains(search.Text, StringComparison.OrdinalIgnoreCase) && !role.JobId.Contains(search.Text, StringComparison.OrdinalIgnoreCase) || onlyMissing.Pressed && eligibility.Allowed) continue;
                count++;
                var card = Card(list, name);
                Badge(card, eligibility.Allowed ? L("requirements-met") : L("requirements-missing"), eligibility.Allowed ? StyleNano.CrtGreen : StyleNano.CrtWarning);
                if (!eligibility.Allowed) Text(card, L("needed") + " " + string.Join(", ", eligibility.Missing.Select(DefinitionName)));
            }
            if (count == 0) Text(list, L("no-search-results"));
            CrtLobbyTheme.Apply(list, useCrtTypography: false);
        }
        search.OnTextChanged += _ => Populate(); onlyMissing.OnToggled += _ => Populate(); Populate();
    }

    private void ReasonAndConfirmation(BoxContainer box, bool confirmation, bool compact = false)
    {
        var reason = Field(box, "action-reason", L("reason"), _reason, compact ? "" : L("reason-help"), L("reason-placeholder"));
        _reason = reason.Text;
        reason.OnTextChanged += _ => { _reason = reason.Text; RefreshActions(); };
        if (!confirmation) return;
        var confirm = new CheckBox { Text = L("confirm-target"), MinHeight = 32, Name = "action-confirm" };
        confirm.Label.FontOverride = _controlFont;
        confirm.OnToggled += _ => { _confirmed = confirm.Pressed; RefreshActions(); };
        box.AddChild(confirm);
        Text(box, TargetName, StyleNano.CrtGreen);
        if (!compact) Text(box, L("confirm-help"));
    }

    private void RenderTraining()
    {
        var action = Card(_content, L("instructor-workflow"));
        if (_view.TargetSynthetic) { Text(action, L("synthetic-excluded-help")); return; }
        Text(action, L("training-steps"));
        if (_view.Target == _view.Viewer) Text(action, L("training-self-locked"), StyleNano.CrtWarning);
        else if (!_view.Management && !_view.TargetOnline) Text(action, L("training-offline-locked"), StyleNano.CrtWarning);
        else if (!_view.Management && !_view.InstructorOnDuty) Text(action, L("training-duty-locked"), StyleNano.CrtWarning);
        Text(action, L("program-label"), StyleNano.CrtGreen);
        var definitions = _view.Store.Definitions.Values.Where(d => d.Enabled && (_view.Management ||
            QualificationRules.CanTrain(_view.Store.Instructors.GetValueOrDefault(_view.Viewer), d.Id))).ToArray();
        if (!definitions.Any(d => d.Id == _selectedDefinition)) _selectedDefinition = definitions.FirstOrDefault()?.Id ?? "";
        Select(action, definitions.Select(d => new KeyValuePair<string, string>(d.Id, Name(d.Name))), _selectedDefinition,
            id => { _selectedDefinition = id; Render(); });
        ReasonAndConfirmation(action, true, compact: true);
        var definition = definitions.FirstOrDefault(d => d.Id == _selectedDefinition);
        if (definition != null)
        {
            foreach (var item in definition.Items.Where(i => i.Enabled).OrderBy(i => i.SortOrder))
            {
                var completion = State?.Progress.GetValueOrDefault(definition.Id)?.GetValueOrDefault(item.Id);
                var step = Card(_content, Name(item.Name), ItemHelp(item));
                AddTrainingMaterials(step, definition.Id, item.Id);
                if (completion != null) Text(step, L("completed-by") + " " + Author(completion.By), StyleNano.CrtGreen);
                else Button(step, L("complete"), () => { var req = Request(definition.Id); req.Item = item.Id; Send(QualificationAction.Complete, req); },
                    () => Confirmed && TrainingTargetReady, name: "complete-" + item.Id);
            }
            var canCertify = State != null && State.Grants.GetValueOrDefault(definition.Id)?.Status is not QualificationStatus.Active and not QualificationStatus.Suspended && QualificationRules.ChecklistComplete(State, definition);
            var certify = Card(_content, L("certify"), canCertify ? L("certify-ready") : L("certify-locked"));
            var remaining = definition.Items.Where(i => i.Enabled && i.Required && State?.Progress.GetValueOrDefault(definition.Id)?.ContainsKey(i.Id) != true).ToArray();
            if (remaining.Length > 0) Text(certify, L("remaining-steps") + " " + string.Join(", ", remaining.Select(i => Name(i.Name))));
            if (State?.Grants.GetValueOrDefault(definition.Id) is { } existing)
                Text(certify, L("current-status") + " " + L("status-" + existing.Status.ToString().ToLowerInvariant()));
            Button(certify, L("certify"), () => Send(QualificationAction.Certify, Request(definition.Id)),
                () => canCertify && Confirmed && TrainingTargetReady, name: "certify");
        }
        else Text(action, L("empty-instructor"));
        var notes = Card(_content, L("training-notes"), L("notes-help"));
        var note = Field(notes, "note-text", L("note-text"), help: L("notes-privacy"));
        Button(notes, L("note"), () => { var req = Request(); req.Reason = note.Text; Send(QualificationAction.Note, req); },
            () => Ready && (_view.Management || _view.InstructorOnDuty && _view.TargetOnline) && !string.IsNullOrWhiteSpace(note.Text), name: "save-note");
        foreach (var entry in _view.Store.Notes.Where(n => n.Target == _view.Target).Reverse())
        {
            var card = Card(notes, Author(entry.Author) + " · " + entry.Author.At.ToLocalTime().ToString("g"));
            Text(card, entry.Text);
        }
        var exercises = Card(_content, L("exercises"), L("exercises-help"));
        var expanded = Column(); expanded.Visible = false; exercises.AddChild(expanded);
        for (var i = 1; i <= 12; i++) Text(expanded, L("exercise-" + i));
        Button(exercises, L("show-exercises"), () => expanded.Visible = !expanded.Visible);
    }

    private void RenderSuspensions()
    {
        var form = Card(_content, L("suspend"), L("suspension-help"), true);
        var definition = DefinitionPicker(form);
        ReasonAndConfirmation(form, true);
        Text(form, L("suspension-locked"));
        Button(form, L("suspend"), () => Send(QualificationAction.Suspend, Request(_selectedDefinition)),
            () => Confirmed && definition != null && _view.Target != _view.Viewer && State?.Grants.GetValueOrDefault(_selectedDefinition)?.Status == QualificationStatus.Active, true, "suspend");
        Button(_content, L("reset-recruit"), () => Navigate("recruit-reset"), () => !_pending, true, "open-recruit-reset");
        var entries = _view.Store.Suspensions.Where(s => s.Target == _view.Target && s.Status is "pending" or "active").ToArray();
        if (entries.Length == 0) Text(_content, L("empty-suspensions"));
        foreach (var entry in entries)
        {
            var card = Card(_content, entry.RecruitReset ? L("reset-recruit") : DefinitionName(entry.Qualification), warning: true);
            Badge(card, L("suspension-" + entry.Status), StyleNano.CrtWarning);
            Text(card, entry.Reason); Text(card, L("initiated-by") + " " + Author(entry.Initiator));
            if (entry.Status == "pending" && entry.Initiator.Actor != _view.Viewer && entry.Target != _view.Viewer)
                Button(card, L("confirm-suspension"), () => { var req = Request(); req.Suspension = entry.Id; Send(QualificationAction.ConfirmSuspension, req); }, () => Confirmed, true);
        }
    }

    private void Mutate(QualificationAction action, QualificationConfiguration? configuration = null, string qualification = "", string item = "")
    {
        if (!Confirmed) return;
        var req = Request(qualification); req.Item = item; req.Configuration = configuration; Send(action, req);
    }

    private void ManagementGuard()
    {
        var guard = Card(_content, L("change-control"), L("change-control-help"), true);
        ReasonAndConfirmation(guard, true, compact: true);
    }

    private void RenderRecruitReset()
    {
        if (_view.TargetSynthetic) { Text(_content, L("synthetic-excluded-help")); return; }
        // The header and confirmation already identify the selected player; keep the primary action visible.
        var form = Card(_content, "", L("reset-recruit-help"), true);
        if (_view.Target == _view.Viewer) Text(form, L("reset-self-locked"), StyleNano.CrtWarning);
        ReasonAndConfirmation(form, true, compact: true);
        Button(form, L("reset-recruit"), () => Send(QualificationAction.ResetRecruit, Request()),
            () => Confirmed && _view.Target != _view.Viewer && State != null, true, "reset-recruit");
        foreach (var pending in _view.Store.Suspensions.Where(s => s.Target == _view.Target && s.RecruitReset && s.Status == "pending"))
        {
            Text(_content, L("reset-awaiting-second"), StyleNano.CrtWarning);
            Text(_content, Author(pending.Initiator) + " · " + pending.Reason);
            Button(_content, L("nav-suspensions"), () => Navigate("suspensions"), () => !_pending);
        }
    }

    private void RenderPlayerManagement()
    {
        Text(_content, L("selected-person") + " · " + TargetName, StyleNano.CrtGreen);
        var selection = Card(_content, L("selected-qualification")); DefinitionPicker(selection);
        ManagementGuard();
        var grant = State?.Grants.GetValueOrDefault(_selectedDefinition);
        var actions = Card(_content, L("qualification-actions"), L("grant-help"));
        Text(actions, L("current-status") + " " + (grant == null ? L("not-certified") : L("status-" + grant.Status.ToString().ToLowerInvariant())), StyleNano.CrtGreen);
        Text(actions, L("management-action-steps"));
        Button(actions, L("grant"), () => Mutate(QualificationAction.Grant, qualification: _selectedDefinition), () => Confirmed && grant == null, name: "grant");
        Button(actions, L("restore"), () => Mutate(QualificationAction.Restore, qualification: _selectedDefinition), () => Confirmed && grant != null && grant.Status != QualificationStatus.Active);
        Button(actions, L("revoke"), () => Mutate(QualificationAction.Revoke, qualification: _selectedDefinition), () => Confirmed && grant != null && grant.Status != QualificationStatus.Revoked, danger: true);
        Button(_content, L("reset-recruit"), () => Navigate("recruit-reset"), () => !_pending, true, "open-recruit-reset");
        var correction = Card(_content, L("correct-progress"), L("correction-help"));
        var itemId = "";
        var items = _view.Store.Definitions.GetValueOrDefault(_selectedDefinition)?.Items.Where(i => i.Enabled).ToArray() ?? Array.Empty<ChecklistItem>();
        itemId = items.FirstOrDefault()?.Id ?? "";
        Select(correction, items.Select(i => new KeyValuePair<string, string>(i.Id, Name(i.Name))), itemId, value => itemId = value);
        Button(correction, L("correct-progress"), () => Mutate(QualificationAction.CorrectProgress, qualification: _selectedDefinition, item: itemId),
            () => Confirmed && itemId.Length > 0, danger: true);
    }

    private Dictionary<string, CheckBox> ProfessionalChecks(BoxContainer box, HashSet<string> selected, bool includeCommand = true) => _view.Store.Definitions.Values
        .Where(d => !QualificationRules.Levels.Contains(d.Id) && (includeCommand || d.Id != "commanding_officer"))
        .OrderBy(d => Name(d.Name)).ToDictionary(d => d.Id, d => Check(box, "professional-" + d.Id, Name(d.Name), selected.Contains(d.Id)));

    private void RenderRoleSettings()
    {
        var picker = Card(_content, L("roles"), L("role-settings-help"));
        var filter = Field(picker, "job-filter", L("search"), placeholder: L("role-search-placeholder"));
        var choices = Column(); picker.AddChild(choices);
        void Populate()
        {
            choices.RemoveAllChildren();
            var filtered = _view.Jobs.Where(j => j.Value.Contains(filter.Text, StringComparison.OrdinalIgnoreCase) || j.Key.Contains(filter.Text, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (filtered.Length == 0) { Text(choices, L("no-search-results")); return; }
            // Filtering the menu must not silently switch the role being edited below it.
            if (_selectedJob.Length == 0) _selectedJob = filtered[0].Key;
            Select(choices, filtered, _selectedJob, id => { _selectedJob = id; _confirmed = false; Render(); });
        }
        filter.OnTextChanged += _ => Populate(); Populate();
        if (_selectedJob.Length == 0) return;
        _scope += "/" + _selectedJob;
        var role = _view.Store.Roles.GetValueOrDefault(_selectedJob) ?? new RoleRequirement { JobId = _selectedJob };
        var form = Card(_content, _view.Jobs.GetValueOrDefault(_selectedJob, _selectedJob), L("role-enabled-help"));
        var enabled = Check(form, "role-enabled", L("enabled"), role.Enabled);
        Text(form, L("level"));
        var minimum = role.MinimumLevel;
        Select(form, Enum.GetValues<MilitaryLevel>().Select(l => new KeyValuePair<string, string>(l.ToString(), L("level-" + l.ToString().ToLowerInvariant()))),
            minimum.ToString(), id => minimum = Enum.Parse<MilitaryLevel>(id));
        Text(form, L("professional-help"));
        var professional = ProfessionalChecks(form, role.Professional);
        ManagementGuard();
        Button(_content, L("save-role"), () => Mutate(QualificationAction.SaveRole, new() { Role = new RoleRequirement
        { JobId = _selectedJob, Enabled = enabled.Pressed, MinimumLevel = minimum, Professional = professional.Where(p => p.Value.Pressed).Select(p => p.Key).ToHashSet(), Tracker = role.Tracker, Govfor = role.Govfor } }), () => Confirmed);
    }

    private void RenderDefinitions()
    {
        var picker = Card(_content, L("definitions")); var d = DefinitionPicker(picker);
        d ??= new QualificationDefinition();
        _scope += "/" + _selectedDefinition;
        var form = Card(_content, Name(d.Name), L("definition-help"));
        var id = Field(form, "definition-id", L("qualification-id"), d.Id, L("stable-id-help"));
        var name = Field(form, "definition-name", L("name"), Name(d.Name));
        var description = Field(form, "definition-description", L("description"), Name(d.Description));
        var enabled = Check(form, "definition-enabled", L("enabled"), d.Enabled);
        var items = new List<(ChecklistItem Item, LineEdit Name, LineEdit Description, LineEdit Order, CheckBox Required, CheckBox Enabled)>();
        foreach (var item in d.Items.OrderBy(i => i.SortOrder))
        {
            var card = Card(_content, Name(item.Name));
            Text(card, L("item-id") + ": " + item.Id, StyleNano.CrtGreen);
            var itemName = Field(card, item.Id + "-name", L("name"), Name(item.Name));
            var itemDescription = Field(card, item.Id + "-description", L("description"), Name(item.Description));
            var order = Field(card, item.Id + "-order", L("order"), item.SortOrder.ToString());
            items.Add((item, itemName, itemDescription, order, Check(card, item.Id + "-required", L("required"), item.Required), Check(card, item.Id + "-enabled", L("enabled"), item.Enabled)));
        }
        var newItem = Card(_content, L("new-step"), L("new-step-help"));
        var newId = Field(newItem, "new-item-id", L("new-item-id"), help: L("stable-id-help"));
        var newName = Field(newItem, "new-item-name", L("name"));
        ManagementGuard();
        Button(_content, L("save-definition"), () =>
        {
            var definition = new QualificationDefinition { Id = id.Text, Name = name.Text, Description = description.Text, Enabled = enabled.Pressed };
            foreach (var item in items)
                definition.Items.Add(new ChecklistItem { Id = item.Item.Id, Name = item.Name.Text, Description = item.Description.Text,
                    SortOrder = int.TryParse(item.Order.Text, out var order) ? order : 0, Required = item.Required.Pressed, Enabled = item.Enabled.Pressed });
            if (newId.Text.Length > 0) definition.Items.Add(new ChecklistItem { Id = newId.Text, Name = newName.Text, SortOrder = definition.Items.Count });
            Mutate(QualificationAction.SaveDefinition, new() { Definition = definition });
        }, () => Confirmed && id.Text.Length > 0 && name.Text.Length > 0);
    }

    private void RenderAccreditation()
    {
        var form = Card(_content, L("instructor-management"), L("accreditation-help"));
        Text(form, L("selected-person") + " " + TargetName, StyleNano.CrtGreen);
        var accreditation = _view.Store.Instructors.GetValueOrDefault(_view.Target);
        var active = Check(form, "instructor-active", L("enabled"), accreditation?.Active ?? false);
        var enlisted = Check(form, "instructor-enlisted", L("train-enlisted"), accreditation?.Enlisted ?? false);
        var sergeant = Check(form, "instructor-sergeant", L("train-sergeant"), accreditation?.Sergeant ?? false);
        Text(form, L("instructor-professional-help"));
        var professional = ProfessionalChecks(form, accreditation?.Professional ?? new(), false);
        ManagementGuard();
        Button(_content, L("save-instructor"), () => Mutate(QualificationAction.SaveInstructor, new() { Instructor = new InstructorAccreditation
            (active.Pressed, enlisted.Pressed, sergeant.Pressed, professional.Where(p => p.Value.Pressed).Select(p => p.Key).ToHashSet(), Guid.Empty, default, Guid.Empty, default) }), () => Confirmed);
    }

    private void RenderPermissions()
    {
        var acl = Card(_content, L("management-acl"), L("acl-help"));
        Text(acl, L("named-accounts-help"));
        var management = AccountChecks(acl, "acl", _view.Store.Management);
        ManagementGuard();
        Button(acl, L("save-acl"), () => Mutate(QualificationAction.SaveManagement, new() { Management = management.Where(p => p.Value.Pressed).Select(p => p.Key).ToHashSet() }),
            () => Confirmed);
        var jobs = Card(_content, L("command-recognition"), L("command-jobs-help"));
        var officers = Field(jobs, "officer-jobs", L("officer-jobs"), string.Join(",", _view.Store.OfficerJobs));
        var commanding = Field(jobs, "co-jobs", L("co-jobs"), string.Join(",", _view.Store.CommandingOfficerJobs));
        Button(jobs, L("save-command-jobs"), () => Mutate(QualificationAction.SaveCommandJobs, new() { OfficerJobs = Split(officers.Text), CommandingOfficerJobs = Split(commanding.Text) }), () => Confirmed);
    }

    private void RenderMigration()
    {
        var migration = Card(_content, L("migration"), L("migration-evidence"), true);
        Text(migration, L("migration-steps"));
        Button(migration, L("migration-scan-all"), () =>
        {
            var req = Request();
            req.Configuration = new() { Roster = null };
            Send(QualificationAction.MigrationPreview, req);
        }, () => Ready);

        var selected = Column(); selected.Visible = false; migration.AddChild(selected);
        Button(migration, L("migration-selected-advanced"), () => selected.Visible = !selected.Visible);
        Text(selected, L("named-accounts-help"));
        var roster = AccountChecks(selected, "migration-roster", new());
        var ids = Field(selected, "migration-ids", L("migration-roster"), help: L("uuid-list-help"));
        HashSet<Guid>? Roster()
        {
            var result = ParseIds(ids.Text);
            if (result == null) return null;
            result.UnionWith(roster.Where(p => p.Value.Pressed).Select(p => p.Key));
            return result;
        }
        Button(selected, L("dry-run-selected"), () =>
        {
            var req = Request();
            req.Configuration = new() { Roster = Roster() };
            Send(QualificationAction.MigrationPreview, req);
        }, () => Ready && Roster() is { Count: > 0 });

        if (_view.Preview is { } preview)
        {
            var results = Card(_content, L("preview-results"), L("preview-help"));
            Heading(results, L("migration-scanned", ("count", preview.AccountsScanned)));
            Heading(results, L("migration-eligible", ("count", preview.EligibleAccounts)));
            foreach (var (id, count) in preview.Counts)
                Text(results, DefinitionName(id) + ": " + count);
            Heading(results, L("migration-records") + ": " + preview.Records);
        }
        else Text(migration, L("preview-empty"));

        ManagementGuard();
        Button(_content, L("execute-migration"), () =>
        {
            var req = Request();
            req.PreviewToken = _view.PreviewToken;
            Send(QualificationAction.MigrationExecute, req);
        }, () => Confirmed && _view.PreviewToken.Length > 0, true);

        var settings = Card(_content, L("migration-settings"), L("migration-settings-help"));
        var settingsBody = Column(); settingsBody.Visible = false; settings.AddChild(settingsBody);
        Button(settings, L("show-migration-settings"), () => settingsBody.Visible = !settingsBody.Visible);
        var groups = new Dictionary<string, LineEdit>();
        foreach (var group in _view.Store.MigrationGroups)
            groups[group.Key] = Field(settingsBody, "group-" + group.Key, DefinitionName(group.Key), string.Join(",", group.Value));
        var aliases = new List<(string Old, LineEdit Canonical, CheckBox Keep)>();
        foreach (var alias in _view.Store.TrackerAliases)
            aliases.Add((alias.Key, Field(settingsBody, "alias-" + alias.Key, alias.Key, alias.Value), Check(settingsBody, "keep-" + alias.Key, L("enabled"), true)));
        var oldAlias = Field(settingsBody, "new-alias", L("legacy-tracker"));
        var newAlias = Field(settingsBody, "new-canonical", L("canonical-tracker"));
        Button(settingsBody, L("save-migration-settings"), () =>
        {
            var canonical = aliases.Where(a => a.Keep.Pressed).ToDictionary(a => a.Old, a => a.Canonical.Text);
            if (oldAlias.Text.Length > 0) canonical[oldAlias.Text] = newAlias.Text;
            Mutate(QualificationAction.SaveMigrationSettings, new() { MigrationGroups = groups.ToDictionary(g => g.Key, g => Split(g.Value.Text)), TrackerAliases = canonical });
        }, () => Confirmed);
    }

    private Dictionary<Guid, CheckBox> AccountChecks(BoxContainer box, string key, HashSet<Guid> selected)
    {
        var candidates = _view.OnlinePlayers.Keys.Concat(_view.AccountNames.Keys).Concat(selected).Append(_view.Target)
            .Where(id => id != Guid.Empty).Distinct().OrderBy(AccountName);
        return candidates.ToDictionary(id => id, id => Check(box, key + "-" + id, AccountName(id), selected.Contains(id)));
    }

    private void RenderMetrics()
    {
        if (_view.Metrics.Count == 0) Text(_content, L("empty-metrics"));
        foreach (var (key, value) in _view.Metrics)
        {
            var caption = key.StartsWith("professional_") ? L("owners") + " · " + DefinitionName(key[13..]) :
                key.StartsWith("suspensions_") ? L("suspensions") + " · " + DefinitionName(key[12..]) : Name("rucm-qualifications-metric-" + key);
            var card = Card(_content, caption); Heading(card, value.ToString("0.##"));
        }
    }

    private void RenderAudit()
    {
        var entries = _view.Store.Audit.Where(a => a.Target == _view.Target || a.Target == null).TakeLast(100).Reverse().ToArray();
        if (entries.Length == 0) Text(_content, L("empty-audit"));
        foreach (var entry in entries)
        {
            var card = Card(_content, Name("rucm-qualifications-action-" + entry.Action.ToLowerInvariant()) + " · " + entry.At.ToLocalTime().ToString("g"));
            Text(card, entry.Reason); Text(card, L("audit-author") + ": " + AccountName(entry.Actor));
            var details = Column(); details.Visible = false; card.AddChild(details);
            Text(details, L("account-id") + ": " + entry.Actor);
            Text(details, L("old-state") + ": " + entry.OldState); Text(details, L("new-state") + ": " + entry.NewState);
            Button(card, L("audit-details"), () => details.Visible = !details.Visible);
        }
    }

    private static HashSet<string> Split(string text) => text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToHashSet();
    private static string ItemHelp(ChecklistItem item)
    {
        if (item.Description != "rucm-qualifications-training-help") return Name(item.Description);
        return Loc.TryGetString("rucm-qualifications-step-help-" + item.Id, out var help) ? help : "";
    }
    private static IEnumerable<Control> Descendants(Control root)
    {
        foreach (var child in root.Children)
        {
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }
    private static HashSet<Guid>? ParseIds(string text)
    {
        var result = new HashSet<Guid>();
        foreach (var value in Split(text)) { if (!Guid.TryParse(value, out var id) || id == Guid.Empty) return null; result.Add(id); }
        return result;
    }
}
