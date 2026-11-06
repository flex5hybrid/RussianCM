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
    private CmuHudLayoutEditor? _editor;
    private string? _snapshot;
    private Control? _ammoAnchor;
    public string Profile => UIManager.ActiveScreen is SeparatedChatGameScreen ? "Separated/" : "Default/";
    public bool CanEdit => UIManager.ActiveScreen is InGameScreen;
    public readonly record struct Entry(string Id, string Caption, Control Control);

    public override void Initialize()
    {
        _cfg.OnValueChanged(CCVars.CMUHudLayout, Load, true);
    }

    private void Load(string text)
    {
        _settings = CmuHudLayoutSettings.Parse(text);
        UIManager.ActiveScreen?.InvalidateArrange();
    }

    public override void FrameUpdate(Robust.Shared.Timing.FrameEventArgs args)
    {
        if (UIManager.ActiveScreen is not InGameScreen screen || _ammoAnchor?.Parent == screen)
            return;
        _ammoAnchor?.Orphan();
        _ammoAnchor = new Control { Name = "CmuAmmoAnchor", SetSize = new Vector2(64), MouseFilter = Control.MouseFilterMode.Ignore };
        screen.AddChild(_ammoAnchor);
        LayoutContainer.SetPosition(_ammoAnchor, new Vector2(12, 360));
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
        var entries = new List<Entry>();
        if (UIManager.ActiveScreen is not InGameScreen screen)
            return entries;
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
        return entries;
    }

    public void RestoreNatural(InGameScreen screen)
    {
        if (_arranging || screen != UIManager.ActiveScreen)
            return;
        _arranging = true;
        try
        {
            foreach (var (control, rect) in _natural.Reverse())
            {
                if (control.Parent != null)
                    control.Arrange(rect);
            }
            _natural.Clear();
        }
        finally { _arranging = false; }
    }

    public void Arrange(InGameScreen screen, Vector2 finalSize)
    {
        if (_arranging || screen != UIManager.ActiveScreen || finalSize.X <= 0 || finalSize.Y <= 0)
            return;
        _arranging = true;
        try
        {
            // Parent groups first, then their explicitly placed slots. All coordinates refer to
            // the full screen; moving one group cannot silently change another saved position.
            foreach (var entry in GetEntries())
            {
                if (!_settings.Items.TryGetValue(Profile + entry.Id, out var placement)
                    || entry.Control.Parent == null || !entry.Control.VisibleInTree)
                    continue;
                var control = entry.Control;
                _natural[control] = UIBox2.FromDimensions(control.Position, control.Size);
                var rect = CmuHudLayoutSettings.Resolve(placement, finalSize, control.Size);
                var offset = screen.GlobalPosition - control.Parent.GlobalPosition;
                control.Arrange(UIBox2.FromDimensions(rect.TopLeft + offset, rect.Size));
            }
        }
        finally
        {
            _arranging = false;
        }
    }

    public void Place(Entry entry, Vector2 position, Vector2 size, bool resize = false)
    {
        if (UIManager.ActiveScreen is not InGameScreen screen)
            return;
        var available = Vector2.Max(Vector2.One, screen.Size - size);
        var fraction = Vector2.Clamp(position / available, Vector2.Zero, Vector2.One);
        var previous = _settings.Items.GetValueOrDefault(Profile + entry.Id);
        _settings.Items[Profile + entry.Id] = new CmuHudLayoutSettings.Placement(fraction.X, fraction.Y,
            resize ? size.X : previous.Width, resize ? size.Y : previous.Height);
        screen.InvalidateArrange();
    }

    public UIBox2 GetAnnouncementArea(InGameScreen screen)
    {
        var left = 12f;
        var top = 90f;
        var right = screen.Width - 12;
        var bottom = screen.Height - 100;
        foreach (var entry in GetEntries())
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
        _natural.Clear();
        _ammoAnchor?.Orphan();
        _ammoAnchor = null;
    }
}
