using System.Linq;
using System.Numerics;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Screens;
using Content.Client.UserInterface.Systems.Actions.Controls;
using Content.Client.UserInterface.Systems.Actions.Widgets;
using Content.Client.UserInterface.Systems.Alerts.Controls;
using Content.Client.UserInterface.Systems.Alerts.Widgets;
using Content.Client.UserInterface.Systems.Chat.Widgets;
using Content.Client.UserInterface.Systems.Ghost.Widgets;
using Content.Client.UserInterface.Systems.Hotbar.Widgets;
using Content.Client.UserInterface.Systems.Inventory.Widgets;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.Client.CMU14.Medical.Presentation.HUD;
using Content.Client._RMC14.Announce;
using Content.Shared.CCVar;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;

namespace Content.Client.CMU14.Interface.Hud;

/// <summary>Arrange real HUD controls without reparenting them or changing network input.</summary>
[UsedImplicitly]
public sealed class CmuHudLayoutController : UIController, IOnStateExited<GameplayState>
{
    [Dependency] private IConfigurationManager _cfg = default!;
    private CmuHudLayoutSettings _settings = new();
    private bool _arranging;
    private readonly Dictionary<Control, UIBox2> _natural = new();
    private readonly Dictionary<Control, AppliedPlacement> _applied = new();
    private readonly HashSet<Control> _watched = new();
    private readonly Dictionary<Control, CmuHudFamilyLayout> _families = new();
    private List<Entry> _entries = new();
    private readonly List<(Entry Entry, CmuHudLayoutSettings.Placement Placement)> _placements = new();
    private bool _placementsDirty = true;
    private InGameScreen? _trackedScreen;
    private bool _entriesDirty = true;
    private CmuHudLayoutEditor? _editor;
    private string? _snapshot;
    private Control? _ammoAnchor;
    public string Profile => UIManager.ActiveScreen is SeparatedChatGameScreen ? "Separated/" : "Default/";
    public bool CanEdit => UIManager.ActiveScreen is InGameScreen;
    public int EntriesVersion { get; private set; }
    public readonly record struct Entry(string Id, string Caption, Control Control);
    private readonly record struct AppliedPlacement(UIBox2 Bounds, UIBox2 Request);

    public override void Initialize()
    {
        _cfg.OnValueChanged(CCVars.CMUHudLayout, Load, true);
    }

    private void Load(string text)
    {
        ClearFamilySizing();
        _settings = CmuHudLayoutSettings.Parse(text);
        _placementsDirty = true;
        UIManager.ActiveScreen?.InvalidateArrange();
    }

    public override void FrameUpdate(Robust.Shared.Timing.FrameEventArgs args)
    {
        if (UIManager.ActiveScreen is not InGameScreen screen)
            return;
        TrackScreen(screen);
        if (_ammoAnchor?.Parent != screen)
        {
            _ammoAnchor?.Orphan();
            _ammoAnchor = new Control { Name = "CmuAmmoAnchor", SetSize = new Vector2(64), MouseFilter = Control.MouseFilterMode.Ignore };
            screen.AddChild(_ammoAnchor);
            LayoutContainer.SetPosition(_ammoAnchor, new Vector2(12, 360));
        }
        // Controllers run after the engine's layout queue. A descendant can arrange itself
        // without arranging InGameScreen; reconcile only placements that actually drifted.
        if (_settings.Items.Count > 0)
            Arrange(screen, screen.Size);
    }

    public void OpenEditor()
    {
        if (!CanEdit || _editor != null)
            return;
        _snapshot = _settings.Serialize();
        _editor = new CmuHudLayoutEditor(this);
        _editor.OnClose += () =>
        {
            if (_snapshot != null)
                Load(_snapshot);
            _snapshot = null;
            _editor = null;
        };
        _editor.Open();
        // Keep menu/action rails available to grab instead of covering them with the editor.
        if (UIManager.ActiveScreen is InGameScreen screen)
            LayoutContainer.SetPosition(_editor, new Vector2(MathF.Max(0, (screen.Width - _editor.SetWidth) / 2), 12));
    }

    public void Save()
    {
        _snapshot = null;
        _cfg.SetCVar(CCVars.CMUHudLayout, _settings.Serialize());
        _cfg.SaveToFile();
        _editor?.Close();
    }

    public void Reset(string? id = null)
    {
        ClearFamilySizing(id);
        _placementsDirty = true;
        if (id != null)
            _settings.Items.Remove(Profile + id);
        else
        {
            foreach (var key in _settings.Items.Keys.Where(key => key.StartsWith(Profile, StringComparison.Ordinal)).ToArray())
                _settings.Items.Remove(key);
        }
        UIManager.ActiveScreen?.InvalidateMeasure();
    }

    public List<Entry> GetEntries()
    {
        if (UIManager.ActiveScreen is not InGameScreen screen)
            return new List<Entry>();
        return new List<Entry>(GetCachedEntries(screen));
    }

    private List<Entry> GetCachedEntries(InGameScreen screen)
    {
        TrackScreen(screen);
        if (!_entriesDirty)
            return _entries;
        var entries = new List<Entry>();
        var seen = new HashSet<string>();
        void Add(string id, string caption, Control control)
        {
            if (seen.Add(id))
                entries.Add(new Entry(id, caption, control));
        }
        void Visit(Control control)
        {
            var id = control switch
            {
                GameTopMenuBar => "menu",
                ActionsBar => "actions",
                AlertsUI => "status",
                GhostGui => "ghost",
                BodyZoneTargetWidget => "targeting",
                ChatBox => "chat",
                AnnouncementWidget => "announcements",
                _ when control == _ammoAnchor => "ammo",
                _ => null,
            };
            if (id != null)
                Add(id, Loc.GetString("cmu-hud-layout-" + id), control);
            // Native dock groups and inline storage rows can be placed independently.
            if (control.Name is "SecondHotbar" or "MainHotbar" or "StatusPanelRight" or "StatusPanelLeft"
                or "HandContainer" or "SingleStorageContainer" or "DoubleStorageContainer"
                or "EquipmentPanel" or "InventoryButton" or "VoteMenu")
                Add(control.Name, Loc.GetString("cmu-hud-layout-" + control.Name.ToLowerInvariant()), control);
            // Individual indicators/menu buttons use stable prototype or control names.
            if (control is AlertControl alert)
                Add("alert/" + alert.Alert.ID, Loc.GetString("cmu-hud-layout-indicator") + " " + alert.Alert.ID, control);
            if (control is MenuButton && control.Parent?.Parent?.Parent is GameTopMenuBar && !string.IsNullOrEmpty(control.Name))
                Add("menu/" + control.Name, Loc.GetString("cmu-hud-layout-menu") + " / " + control.Name, control);
            if (control is SlotControl slot && !string.IsNullOrEmpty(slot.SlotName) && slot.SlotName != "null")
                Add("slot/" + slot.SlotName, Loc.GetString("cmu-hud-layout-slot") + " " + slot.SlotName, control);
            if (control is ActionButton action && action.Parent is GridContainer grid)
                Add("action/" + action.GetPositionInParent(), Loc.GetString("cmu-hud-layout-actions") + " / "
                    + (action.GetPositionInParent() + 1), control);
            foreach (var child in control.Children)
                Visit(child);
        }
        Visit(screen);
        _entries = entries;
        _entriesDirty = false;
        _placementsDirty = true;
        return _entries;
    }

    private void TrackScreen(InGameScreen screen)
    {
        if (_trackedScreen == screen)
            return;
        if (_trackedScreen != null)
            RestoreNatural(_trackedScreen);
        ClearTracking();
        _trackedScreen = screen;
        Watch(screen);
    }

    private void Watch(Control control)
    {
        if (!_watched.Add(control))
            return;
        control.OnChildAdded += ChildAdded;
        control.OnChildRemoved += ChildRemoved;
        foreach (var child in control.Children)
            Watch(child);
    }

    private void ChildAdded(Control child)
    {
        _entriesDirty = true;
        EntriesVersion++;
        Watch(child);
    }

    private void ChildRemoved(Control child)
    {
        _entriesDirty = true;
        EntriesVersion++;
        child.OnChildAdded -= ChildAdded;
        child.OnChildRemoved -= ChildRemoved;
        _watched.Remove(child);
        _natural.Remove(child);
        _applied.Remove(child);
        foreach (var descendant in child.Children)
            ChildRemoved(descendant);
        if (_families.Remove(child, out var family))
            family.Reset();
    }

    private void ClearTracking()
    {
        ClearFamilySizing();
        foreach (var control in _watched)
        {
            control.OnChildAdded -= ChildAdded;
            control.OnChildRemoved -= ChildRemoved;
        }
        _watched.Clear();
        _entries.Clear();
        _placements.Clear();
        _placementsDirty = true;
        _entriesDirty = true;
        EntriesVersion++;
        _trackedScreen = null;
        _natural.Clear();
        _applied.Clear();
    }

    private void ClearFamilySizing(string? id = null)
    {
        foreach (var (control, family) in _families.Reverse().ToArray())
        {
            if (id != null && family.Id != id)
                continue;
            family.Reset();
            _families.Remove(control);
        }
    }

    public void RestoreNatural(InGameScreen screen)
    {
        if (_arranging || screen != _trackedScreen)
            return;
        _arranging = true;
        try
        {
            foreach (var (control, rect) in _natural.Reverse())
            {
                // A newer native arrange already superseded this custom rectangle.
                if (control.Parent != null && _applied.TryGetValue(control, out var applied)
                    && Bounds(control) == applied.Bounds)
                    control.Arrange(rect);
            }
            _natural.Clear();
            _applied.Clear();
        }
        finally { _arranging = false; }
    }

    public void Arrange(InGameScreen screen, Vector2 finalSize)
    {
        if (_arranging || screen != UIManager.ActiveScreen || finalSize.X <= 0 || finalSize.Y <= 0)
            return;
        var entries = GetCachedEntries(screen);
        if (_placementsDirty)
        {
            _placements.Clear();
            var profile = Profile;
            foreach (var entry in entries)
            {
                if (_settings.Items.TryGetValue(profile + entry.Id, out var placement))
                    _placements.Add((entry, placement));
            }
            _placementsDirty = false;
        }
        _arranging = true;
        try
        {
            // Parent groups first, then their explicitly placed slots. All coordinates refer to
            // the full screen; moving one group cannot silently change another saved position.
            foreach (var (entry, placement) in _placements)
            {
                if (entry.Control.Parent == null || !entry.Control.VisibleInTree)
                    continue;
                var control = entry.Control;
                var bounds = Bounds(control);
                var hasApplied = _applied.TryGetValue(control, out var applied);
                if (!hasApplied || bounds != applied.Bounds)
                    _natural[control] = control.Margin.Inflate(bounds);
                var rect = CmuHudLayoutSettings.Resolve(placement, finalSize, control.Size);
                if (placement.Width > 0 || placement.Height > 0)
                {
                    if (!_families.TryGetValue(control, out var family))
                    {
                        family = new CmuHudFamilyLayout(control, entry.Id);
                        _families.Add(control, family);
                    }
                    rect = GrowToFitContent(rect, family.Prepare(rect.Size, finalSize, EntriesVersion), finalSize);
                }
                var offset = screen.GlobalPosition - control.Parent.GlobalPosition;
                var request = control.Margin.Inflate(UIBox2.FromDimensions(rect.TopLeft + offset, rect.Size));
                if (hasApplied && bounds == applied.Bounds && request == applied.Request && control.IsArrangeValid)
                    continue;
                control.Arrange(request);
                _applied[control] = new AppliedPlacement(Bounds(control), request);
            }
        }
        finally
        {
            _arranging = false;
        }
    }

    private static UIBox2 Bounds(Control control) => UIBox2.FromDimensions(control.Position, control.Size);

    private static UIBox2 GrowToFitContent(UIBox2 rect, Vector2 content, Vector2 available)
    {
        var size = Vector2.Min(Vector2.Max(rect.Size, content), Vector2.Max(Vector2.One, available));
        var position = Vector2.Min(rect.TopLeft, Vector2.Max(Vector2.Zero, available - size));
        return UIBox2.FromDimensions(position, size);
    }

    public void Place(Entry entry, Vector2 position, Vector2 size, bool resize = false)
    {
        if (UIManager.ActiveScreen is not InGameScreen screen)
            return;
        var previous = _settings.Items.GetValueOrDefault(Profile + entry.Id);
        var width = resize ? size.X : previous.Width;
        var height = resize ? size.Y : previous.Height;
        // Resolve uses the archived dimensions before growing a family for wrapped rows.
        // Normalize against those same dimensions so moving an auto-grown family stays under the cursor.
        var reference = new Vector2(width > 0 ? width : size.X, height > 0 ? height : size.Y);
        var available = Vector2.Max(Vector2.One, screen.Size - reference);
        var fraction = Vector2.Clamp(position / available, Vector2.Zero, Vector2.One);
        _settings.Items[Profile + entry.Id] = new CmuHudLayoutSettings.Placement(fraction.X, fraction.Y,
            width, height);
        _placementsDirty = true;
        screen.InvalidateArrange();
    }

    public UIBox2 GetAnnouncementArea(InGameScreen screen)
    {
        var left = 12f;
        var top = 90f;
        var right = screen.Width - 12;
        var bottom = screen.Height - 100;
        foreach (var entry in GetCachedEntries(screen))
        {
            if (!entry.Control.VisibleInTree)
                continue;
            var rect = entry.Control.GlobalRect;
            var origin = screen.GlobalPosition;
            if (entry.Id == "menu" && rect.Top - origin.Y < screen.Height * 0.25f) top = MathF.Max(top, rect.Bottom - origin.Y + 12);
            if (entry.Id == "actions" && rect.Left - origin.X < screen.Width * 0.25f) left = MathF.Max(left, rect.Right - origin.X + 12);
            if (entry.Id == "chat" && rect.Left - origin.X > screen.Width * 0.45f) right = MathF.Min(right, rect.Left - origin.X - 12);
        }
        left = Math.Clamp(left, 0, MathF.Max(0, screen.Width - 64));
        top = Math.Clamp(top, 0, MathF.Max(0, screen.Height - 64));
        right = Math.Clamp(right, left + MathF.Min(64, screen.Width), MathF.Max(screen.Width, left + 64));
        bottom = MathF.Max(top + MathF.Min(64, screen.Height), bottom);
        return new UIBox2(left, top, right, bottom);
    }

    public UIBox2 PlaceOverlay(string id, UIBox2 natural)
    {
        if (UIManager.ActiveScreen is not InGameScreen screen)
            return natural;
        if (!_settings.Items.TryGetValue(Profile + id, out var placement))
        {
            if (id == "ammo" && _ammoAnchor?.Parent == screen)
                LayoutContainer.SetPosition(_ammoAnchor, natural.TopLeft / screen.UIScale - screen.GlobalPosition);
            return natural;
        }
        var rect = CmuHudLayoutSettings.Resolve(placement, screen.Size, natural.Size / screen.UIScale);
        return UIBox2.FromDimensions((rect.TopLeft + screen.GlobalPosition) * screen.UIScale, rect.Size * screen.UIScale);
    }

    public void OnStateExited(GameplayState state)
    {
        _editor?.Close();
        _ammoAnchor?.Orphan();
        _ammoAnchor = null;
        ClearTracking();
    }
}
