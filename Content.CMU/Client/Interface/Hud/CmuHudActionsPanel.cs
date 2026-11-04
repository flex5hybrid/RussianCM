using System.Linq;
using Content.Client.CMU14.UserInterface;
using Content.Client.UserInterface.Controls;
using Content.Client.Verbs;
using Content.Client.Verbs.UI;
using Content.Shared.Verbs;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.CMU14.Interface.Hud;

/// <summary>World interactions for the object selected through the native context menu.</summary>
public sealed class CmuHudActionsPanel : BoxContainer
{
    // Collection expressions targeting List<T> can emit CollectionsMarshal.SetCount,
    // which is not permitted by the content sandbox. Keep a regular collection initializer.
    private static readonly List<Type> WorldVerbTypes = new List<Type>
    {
        typeof(Verb), typeof(InteractionVerb), typeof(UtilityVerb), typeof(InnateVerb),
        typeof(AlternativeVerb), typeof(ActivationVerb), typeof(ExamineVerb), typeof(EquipmentVerb),
    };
    private readonly IEntityManager _entities;
    private readonly IEntitySystemManager _systems;
    private readonly IPlayerManager _player;
    private readonly VerbMenuUIController _context;
    private readonly GridContainer _grid;
    private readonly Label _targetName;
    private readonly Label _hint;
    private readonly Button _refresh;
    private readonly SortedSet<Verb> _verbs = new();
    private VerbSystem? _system;
    private NetEntity? _target;
    private EntityUid? _user;
    private bool _dirty;
    private float _buttonWidth = 200;
    public event Action<int>? CountChanged;

    public CmuHudActionsPanel()
    {
        // Only resolve services during screen construction, never a running entity system.
        _entities = IoCManager.Resolve<IEntityManager>();
        _systems = IoCManager.Resolve<IEntitySystemManager>();
        _player = IoCManager.Resolve<IPlayerManager>();
        _context = UserInterfaceManager.GetUIController<VerbMenuUIController>();
        Orientation = LayoutOrientation.Vertical;
        HorizontalExpand = true;
        Margin = new Thickness(6, 4);
        var header = new BoxContainer { Orientation = LayoutOrientation.Horizontal, SeparationOverride = 4 };
        _targetName = new Label { StyleClasses = { CmuHudStyle.Body }, HorizontalExpand = true, ClipText = true };
        _refresh = new Button { Text = "↻", StyleClasses = { CmuHudStyle.Command }, ToolTip = Loc.GetString("cmu-hud-world-refresh"), Disabled = true };
        _refresh.OnPressed += _ => RefreshTarget();
        header.AddChild(_targetName);
        header.AddChild(_refresh);
        AddChild(header);
        _hint = new Label { StyleClasses = { CmuHudStyle.Body, CmuHudStyle.Muted }, ClipText = true };
        AddChild(_hint);
        _grid = new GridContainer { Columns = 2, HSeparationOverride = 4, VSeparationOverride = 4 };
        var scroll = new ScrollContainer { HScrollEnabled = false, ReturnMeasure = true, MaxHeight = 160 };
        scroll.AddChild(_grid);
        AddChild(scroll);
        ClearTarget();
    }

    protected override void EnteredTree()
    {
        base.EnteredTree();
        _context.WorldTargetSelected += OnTargetSelected;
        _systems.SystemLoaded += OnSystemLoaded;
        _systems.SystemUnloaded += OnSystemUnloaded;
        BindSystem(_systems.GetEntitySystemOrNull<VerbSystem>());
        _user = _player.LocalEntity;
    }

    protected override void ExitedTree()
    {
        _context.WorldTargetSelected -= OnTargetSelected;
        _systems.SystemLoaded -= OnSystemLoaded;
        _systems.SystemUnloaded -= OnSystemUnloaded;
        BindSystem(null);
        ClearTarget();
        base.ExitedTree();
    }

    private void OnSystemLoaded(object? sender, SystemChangedArgs args)
    {
        if (args.System is VerbSystem system)
            BindSystem(system);
    }

    private void OnSystemUnloaded(object? sender, SystemChangedArgs args)
    {
        if (ReferenceEquals(args.System, _system))
            BindSystem(null);
    }

    private void BindSystem(VerbSystem? system)
    {
        if (ReferenceEquals(_system, system))
            return;
        if (_system != null)
            _system.OnVerbsResponse -= OnResponse;
        ClearTarget();
        _system = system;
        if (system != null)
            system.OnVerbsResponse += OnResponse;
    }

    private static bool IsWorldVerb(Verb verb) => WorldVerbTypes.Contains(verb.GetType()) &&
        verb.Category?.Text != VerbCategory.Admin.Text && verb.Category?.Text != VerbCategory.Debug.Text;

    private void OnTargetSelected(NetEntity target, SortedSet<Verb> localVerbs)
    {
        if (_system == null)
            return;
        _user = _player.LocalEntity;
        _target = target;
        _verbs.Clear();
        _verbs.UnionWith(localVerbs.Where(IsWorldVerb));
        _dirty = true;
    }

    private void OnResponse(VerbsResponseEvent response)
    {
        if (_target != response.Entity || _user != _player.LocalEntity)
            return;
        if (response.Verbs == null)
            _verbs.Clear();
        else
            _verbs.UnionWith(response.Verbs.Where(IsWorldVerb));
        _dirty = true;
    }

    private void RefreshTarget()
    {
        if (_system == null || _target is not { } target || _player.LocalEntity is not { } user)
            return;
        _verbs.Clear();
        _verbs.UnionWith(_system.GetVerbs(target, user, WorldVerbTypes, out _).Where(IsWorldVerb));
        _dirty = true;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        if (_user != _player.LocalEntity || _target is { } selected &&
            (!_entities.TryGetEntity(selected, out var local) || _entities.Deleted(local.Value)))
        {
            _user = _player.LocalEntity;
            ClearTarget();
        }
        if (!_dirty)
            return;
        _dirty = false;
        RenderVerbs();
    }

    public void SetAvailableWidth(float width)
    {
        _grid.Columns = width < 420 ? 1 : 2;
        _buttonWidth = MathF.Max(80, (width - 28) / _grid.Columns - 4);
        foreach (var button in _grid.Children)
            button.MaxWidth = _buttonWidth;
    }

    private void ClearRows()
    {
        foreach (var child in _grid.Children.ToArray())
        {
            child.Orphan();
            child.Release();
        }
    }

    private void ClearTarget()
    {
        _target = null;
        _verbs.Clear();
        ClearRows();
        _targetName.Text = Loc.GetString("cmu-hud-world-target");
        _hint.Text = Loc.GetString("cmu-hud-world-select");
        _hint.ToolTip = _hint.Text;
        _hint.Visible = true;
        _refresh.Disabled = true;
        CountChanged?.Invoke(0);
    }

    private void RenderVerbs()
    {
        ClearRows();
        if (_target is not { } target || !_entities.TryGetEntity(target, out var uid) ||
            !_entities.TryGetComponent<MetaDataComponent>(uid.Value, out var metadata))
        {
            ClearTarget();
            return;
        }
        _targetName.Text = metadata.EntityName;
        _targetName.ToolTip = metadata.EntityName;
        _refresh.Disabled = false;
        foreach (var verb in _verbs)
        {
            var text = FormattedMessage.RemoveMarkupPermissive(verb.Text);
            var tooltip = string.IsNullOrWhiteSpace(verb.Message) ? text : FormattedMessage.RemoveMarkupPermissive(verb.Message);
            Button button;
            if (verb.ConfirmationPopup)
            {
                var confirm = new ConfirmButton { Text = text, Disabled = verb.Disabled };
                confirm.OnPressed += _ => Execute(target, verb);
                button = confirm;
            }
            else
            {
                button = new Button { Text = text, Disabled = verb.Disabled };
                button.OnPressed += _ => Execute(target, verb);
            }
            button.AddStyleClass(CmuHudStyle.Command);
            button.HorizontalExpand = true;
            button.MaxWidth = _buttonWidth;
            button.Label.ClipText = true;
            button.Label.HorizontalExpand = true;
            button.ToolTip = verb.Category is { } category ? category.Text + ": " + tooltip : tooltip;
            _grid.AddChild(button);
        }
        _hint.Text = Loc.GetString("cmu-hud-world-empty");
        _hint.Visible = _verbs.Count == 0;
        CountChanged?.Invoke(_verbs.Count);
    }

    private void Execute(NetEntity target, Verb verb)
    {
        if (_system == null || _target != target || _user != _player.LocalEntity || verb.Disabled ||
            !_entities.TryGetEntity(target, out var uid) || _entities.Deleted(uid.Value))
            return;
        _system.ExecuteVerb(target, verb);
        // Drop captured interactions immediately; refresh through the normal server request path.
        RefreshTarget();
    }
}
