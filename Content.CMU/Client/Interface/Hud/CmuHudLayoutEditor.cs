using System.Numerics;
using Content.Client.Resources;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Screens;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Input;
using Robust.Shared.Timing;

namespace Content.Client.CMU14.Interface.Hud;

/// <summary>Direct mouse editing of actual HUD bounds, with a compact movable toolbar.</summary>
public sealed class CmuHudLayoutEditor : DefaultWindow
{
    private readonly CmuHudLayoutController _controller;
    private readonly OptionButton _selector = new() { HorizontalExpand = true };
    private readonly CheckBox _snap = new() { Text = Loc.GetString("cmu-hud-layout-snap"), Pressed = true };
    private readonly CheckBox _individual = new() { Text = Loc.GetString("cmu-hud-layout-individual") };
    private List<CmuHudLayoutController.Entry> _entries = new();
    private readonly EditOverlay _overlay;
    private int _selected;
    private int _entriesVersion;
    private string? SelectedId => _selected < _entries.Count ? _entries[_selected].Id : null;

    public CmuHudLayoutEditor(CmuHudLayoutController controller)
    {
        _controller = controller;
        Title = Loc.GetString("cmu-hud-layout-title");
        MinSize = new Vector2(420, 150);
        SetSize = new Vector2(560, 180);
        var body = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical, SeparationOverride = 6, Margin = new Thickness(8),
        };
        Contents.AddChild(body);
        var help = new RichTextLabel();
        help.SetMessage(Loc.GetString("cmu-hud-layout-help"));
        body.AddChild(help);
        body.AddChild(_selector);
        var options = new BoxContainer { SeparationOverride = 12 };
        options.AddChild(_snap);
        options.AddChild(_individual);
        body.AddChild(options);
        _individual.OnToggled += _ => RefreshEntries();
        var row = new BoxContainer { SeparationOverride = 6 };
        var reset = new Button { Text = Loc.GetString("cmu-hud-layout-reset-item"), HorizontalExpand = true };
        reset.OnPressed += _ => { if (SelectedId is { } id) _controller.Reset(id); };
        var resetAll = new Button { Text = Loc.GetString("cmu-hud-layout-reset-all"), HorizontalExpand = true };
        resetAll.OnPressed += _ => _controller.Reset();
        row.AddChild(reset);
        row.AddChild(resetAll);
        body.AddChild(row);
        var finish = new BoxContainer { SeparationOverride = 6 };
        var cancel = new Button { Text = Loc.GetString("cmu-hud-layout-cancel"), HorizontalExpand = true };
        var save = new Button { Text = Loc.GetString("cmu-hud-layout-save"), HorizontalExpand = true };
        cancel.OnPressed += _ => Close();
        save.OnPressed += _ => _controller.Save();
        finish.AddChild(cancel);
        finish.AddChild(save);
        body.AddChild(finish);
        _selector.OnItemSelected += args => Select(args.Id);
        _overlay = new EditOverlay(this);
        UserInterfaceManager.WindowRoot.AddChild(_overlay);
        LayoutContainer.SetAnchorPreset(_overlay, LayoutContainer.LayoutPreset.Wide);
        OnClose += () => _overlay.Orphan();
        RefreshEntries();
    }

    private void RefreshEntries()
    {
        var selectedId = SelectedId;
        _entries = _controller.GetEntries();
        _entriesVersion = _controller.EntriesVersion;
        if (!_individual.Pressed)
            _entries.RemoveAll(entry => entry.Id.Contains('/'));
        _selector.Clear();
        var selected = 0;
        for (var i = 0; i < _entries.Count; i++)
        {
            _selector.AddItem(_entries[i].Caption, i);
            if (_entries[i].Id == selectedId)
                selected = i;
        }
        if (_entries.Count > 0)
            Select(selected);
    }

    private void Select(int index)
    {
        if (index < 0 || index >= _entries.Count)
            return;
        _selected = index;
        _selector.SelectId(index);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        if (_entriesVersion != _controller.EntriesVersion)
            RefreshEntries();
    }

    private sealed class EditOverlay : Control
    {
        private const float HandleSize = 12;
        private readonly CmuHudLayoutEditor _editor;
        private readonly Font _font;
        private CmuHudLayoutController.Entry? _drag;
        private Vector2 _grabOffset;
        private Vector2 _startPointer;
        private Vector2 _startPosition;
        private Vector2 _startSize;
        private bool _resizing;

        public EditOverlay(CmuHudLayoutEditor editor)
        {
            _editor = editor;
            Name = "CmuHudEditOverlay";
            MouseFilter = MouseFilterMode.Stop;
            var cache = IoCManager.Resolve<IResourceCache>();
            _font = StyleNano.CrtUiEnabled ? StyleNano.GetCrtFont(cache, 8) : cache.NotoStack(size: 10);
        }

        private static UIBox2 ResizeHandle(Control control) => UIBox2.FromDimensions(
            control.GlobalRect.BottomRight - new Vector2(HandleSize), new Vector2(HandleSize));

        protected override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);
            for (var i = 0; i < _editor._entries.Count; i++)
            {
                var entry = _editor._entries[i];
                if (!entry.Control.VisibleInTree || entry.Control.Width <= 0 || entry.Control.Height <= 0)
                    continue;
                var rect = UIBox2.FromDimensions(entry.Control.GlobalPixelPosition - GlobalPixelPosition, entry.Control.PixelSize);
                var selected = i == _editor._selected;
                var colour = selected ? CmuHudStyle.SelectionColor : CmuHudStyle.TextColor.WithAlpha(0.45f);
                handle.DrawRect(rect, colour, false);
                if (!selected)
                    continue;
                var cornerSize = new Vector2(HandleSize * UIScale);
                handle.DrawRect(UIBox2.FromDimensions(Vector2.Max(rect.TopLeft, rect.BottomRight - cornerSize),
                    Vector2.Min(cornerSize, rect.Size)), colour);
                var caption = new Vector2(rect.Left + 4 * UIScale,
                    MathF.Max(_font.GetAscent(UIScale), rect.Top - 4 * UIScale));
                handle.DrawString(_font, caption, entry.Caption, UIScale, colour);
            }
        }

        private int Pick(Vector2 point)
        {
            var best = -1;
            var smallest = float.PositiveInfinity;
            for (var i = 0; i < _editor._entries.Count; i++)
            {
                var control = _editor._entries[i].Control;
                if (!control.VisibleInTree || !control.GlobalRect.Contains(point))
                    continue;
                var area = control.Width * control.Height;
                if (area <= 0 || area >= smallest)
                    continue;
                smallest = area;
                best = i;
            }
            return best;
        }

        protected override void KeyBindDown(GUIBoundKeyEventArgs args)
        {
            base.KeyBindDown(args);
            if (args.Function == EngineKeyFunctions.CloseModals)
            {
                args.Handle();
                _editor.Close();
                return;
            }
            // LMB also binds Use. Handling that bind aborts the input manager's loop
            // before UIClick can start the drag. MouseFilter.Stop still blocks the HUD below.
            if (args.Function != EngineKeyFunctions.UIClick)
                return;
            args.Handle();
            var point = args.PointerLocation.Position / UIScale;
            var index = _editor._selected;
            _resizing = index < _editor._entries.Count && _editor._entries[index].Control.VisibleInTree
                && ResizeHandle(_editor._entries[index].Control).Contains(point);
            if (!_resizing)
                index = Pick(point);
            if (index < 0 || UserInterfaceManager.ActiveScreen is not InGameScreen screen)
                return;
            _editor.Select(index);
            var target = _editor._entries[index];
            _drag = target;
            _grabOffset = point - target.Control.GlobalPosition;
            _startPointer = point;
            _startPosition = target.Control.GlobalPosition - screen.GlobalPosition;
            _startSize = target.Control.Size;
            UserInterfaceManager.ControlFocused = this;
        }

        protected override void KeyBindUp(GUIBoundKeyEventArgs args)
        {
            base.KeyBindUp(args);
            if (args.Function != EngineKeyFunctions.UIClick)
                return;
            args.Handle();
            EndDrag();
            if (UserInterfaceManager.ControlFocused == this)
                UserInterfaceManager.ControlFocused = null;
        }

        protected override void ControlFocusExited()
        {
            base.ControlFocusExited();
            EndDrag();
        }

        private void EndDrag()
        {
            _drag = null;
            _resizing = false;
        }

        protected override void MouseMove(GUIMouseMoveEventArgs args)
        {
            base.MouseMove(args);
            var point = args.GlobalPosition;
            if (_drag is not { } target || UserInterfaceManager.ActiveScreen is not InGameScreen screen)
            {
                DefaultCursorShape = Pick(point) >= 0 ? CursorShape.Crosshair : CursorShape.Arrow;
                return;
            }
            if (_resizing)
            {
                var size = _startSize + point - _startPointer;
                if (_editor._snap.Pressed)
                    size = Snap(size);
                size = Vector2.Clamp(size, Vector2.Max(Vector2.One, target.Control.MinSize),
                    Vector2.Max(Vector2.Max(Vector2.One, target.Control.MinSize), screen.Size - _startPosition));
                _editor._controller.Place(target, _startPosition, size, true);
            }
            else
            {
                var position = point - _grabOffset - screen.GlobalPosition;
                if (_editor._snap.Pressed)
                    position = Snap(position);
                _editor._controller.Place(target, position, target.Control.Size);
            }
            args.Handle();
        }

        private static Vector2 Snap(Vector2 value) => new(MathF.Round(value.X / 8) * 8, MathF.Round(value.Y / 8) * 8);
    }
}
