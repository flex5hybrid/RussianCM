using System.Numerics;
using Content.Client.UserInterface.Screens;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Input;

namespace Content.Client.CMU14.Interface.Hud;

/// <summary>A reversible HUD edit session. The overlay handles dragging; gameplay input stays untouched.</summary>
public sealed class CmuHudLayoutEditor : DefaultWindow
{
    private readonly CmuHudLayoutController _controller;
    private readonly OptionButton _selector = new() { HorizontalExpand = true };
    private readonly SpinBox _x = Number();
    private readonly SpinBox _y = Number();
    private readonly SpinBox _width = Number();
    private readonly SpinBox _height = Number();
    private readonly CheckBox _snap = new() { Text = Loc.GetString("cmu-hud-layout-snap"), Pressed = true };
    private List<CmuHudLayoutController.Entry> _entries = new();
    private readonly EditOverlay _overlay;
    private bool _updating;
    private int _selected;

    public CmuHudLayoutEditor(CmuHudLayoutController controller)
    {
        _controller = controller;
        Title = Loc.GetString("cmu-hud-layout-title");
        MinSize = new Vector2(340, 340);
        SetSize = new Vector2(380, 420);
        var body = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, SeparationOverride = 8, Margin = new Thickness(8) };
        Contents.AddChild(body);
        var help = new RichTextLabel();
        help.SetMessage(Loc.GetString("cmu-hud-layout-help"));
        body.AddChild(help);
        body.AddChild(_selector);
        AddRow(body, "cmu-hud-layout-x", _x);
        AddRow(body, "cmu-hud-layout-y", _y);
        AddRow(body, "cmu-hud-layout-width", _width);
        AddRow(body, "cmu-hud-layout-height", _height);
        body.AddChild(_snap);
        var apply = new Button { Text = Loc.GetString("cmu-hud-layout-apply") };
        apply.OnPressed += _ => ApplyNumbers();
        body.AddChild(apply);
        var reset = new Button { Text = Loc.GetString("cmu-hud-layout-reset-item") };
        reset.OnPressed += _ => { if (_entries.Count > _selected) _controller.Reset(_entries[_selected].Id); };
        body.AddChild(reset);
        var resetAll = new Button { Text = Loc.GetString("cmu-hud-layout-reset-all") };
        resetAll.OnPressed += _ => _controller.Reset();
        body.AddChild(resetAll);
        var row = new BoxContainer { SeparationOverride = 8 };
        var cancel = new Button { Text = Loc.GetString("cmu-hud-layout-cancel"), HorizontalExpand = true };
        var save = new Button { Text = Loc.GetString("cmu-hud-layout-save"), HorizontalExpand = true };
        cancel.OnPressed += _ => Close();
        save.OnPressed += _ => _controller.Save();
        row.AddChild(cancel);
        row.AddChild(save);
        body.AddChild(row);
        _selector.OnItemSelected += args => { _selector.SelectId(args.Id); Select(args.Id); };
        _overlay = new EditOverlay(this);
        UserInterfaceManager.WindowRoot.AddChild(_overlay);
        LayoutContainer.SetAnchorPreset(_overlay, LayoutContainer.LayoutPreset.Wide);
        OnClose += () => _overlay.Orphan();
        RefreshEntries();
    }

    private static SpinBox Number() => new() { IsValid = value => value >= 0 && value <= 8192, HorizontalExpand = true };

    private static void AddRow(BoxContainer body, string key, Control field)
    {
        var row = new BoxContainer { SeparationOverride = 8 };
        row.AddChild(new Label { Text = Loc.GetString(key), MinWidth = 120 });
        row.AddChild(field);
        body.AddChild(row);
    }

    private void RefreshEntries()
    {
        _entries = _controller.GetEntries();
        _selector.Clear();
        for (var i = 0; i < _entries.Count; i++)
            _selector.AddItem(_entries[i].Caption, i);
        if (_entries.Count > 0)
            Select(0);
    }

    private void Select(int index)
    {
        if (index >= _entries.Count || UserInterfaceManager.ActiveScreen is not InGameScreen screen)
            return;
        _selected = index;
        _selector.SelectId(index);
        var control = _entries[index].Control;
        var position = control.GlobalPosition - screen.GlobalPosition;
        _updating = true;
        _x.OverrideValue(Math.Max(0, (int) position.X));
        _y.OverrideValue(Math.Max(0, (int) position.Y));
        _width.OverrideValue(Math.Max(1, (int) control.Width));
        _height.OverrideValue(Math.Max(1, (int) control.Height));
        _updating = false;
    }

    private void ApplyNumbers()
    {
        if (_updating || _entries.Count <= _selected)
            return;
        _controller.Place(_entries[_selected], new Vector2(_x.Value, _y.Value),
            new Vector2(Math.Max(1, _width.Value), Math.Max(1, _height.Value)), true);
    }

    private sealed class EditOverlay : Control
    {
        private readonly CmuHudLayoutEditor _editor;
        private CmuHudLayoutController.Entry? _drag;
        private Vector2 _offset;

        public EditOverlay(CmuHudLayoutEditor editor)
        {
            _editor = editor;
            MouseFilter = MouseFilterMode.Stop;
        }

        protected override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);
            for (var i = 0; i < _editor._entries.Count; i++)
            {
                var entry = _editor._entries[i];
                if (!entry.Control.VisibleInTree)
                    continue;
                var rect = UIBox2.FromDimensions(entry.Control.GlobalPixelPosition - GlobalPixelPosition, entry.Control.PixelSize);
                handle.DrawRect(rect, i == _editor._selected ? Color.Cyan : Color.White.WithAlpha(0.25f), false);
            }
        }

        protected override void KeyBindDown(GUIBoundKeyEventArgs args)
        {
            base.KeyBindDown(args);
            args.Handle();
            if (args.Function != EngineKeyFunctions.UIClick)
                return;
            var point = args.PointerLocation.Position / UIScale;
            // Explicit selection wins over overlapping parent/child groups.
            if (_editor._entries.Count > _editor._selected)
            {
                var selected = _editor._entries[_editor._selected];
                if (selected.Control.VisibleInTree && selected.Control.GlobalRect.Contains(point))
                    _drag = selected;
            }
            if (_drag == null)
            {
                for (var i = _editor._entries.Count - 1; i >= 0; i--)
                {
                    var entry = _editor._entries[i];
                    if (!entry.Control.VisibleInTree || !entry.Control.GlobalRect.Contains(point))
                        continue;
                    _editor.Select(i);
                    _drag = entry;
                    break;
                }
            }
            if (_drag is { } target)
                _offset = point - target.Control.GlobalPosition;
        }

        protected override void KeyBindUp(GUIBoundKeyEventArgs args)
        {
            base.KeyBindUp(args);
            args.Handle();
            if (args.Function == EngineKeyFunctions.UIClick)
            {
                _drag = null;
                _editor.Select(_editor._selected);
            }
        }

        protected override void MouseMove(GUIMouseMoveEventArgs args)
        {
            base.MouseMove(args);
            if (_drag is not { } target || UserInterfaceManager.ActiveScreen is not InGameScreen screen)
                return;
            var position = args.GlobalPosition - _offset - screen.GlobalPosition;
            if (_editor._snap.Pressed)
                position = new Vector2(MathF.Round(position.X / 8) * 8, MathF.Round(position.Y / 8) * 8);
            _editor._controller.Place(target, position, target.Control.Size);
            args.Handle();
        }
    }
}
