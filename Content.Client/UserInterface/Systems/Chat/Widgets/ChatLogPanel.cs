using System.Numerics;
using Content.Client.CMU14.Interface;
using Content.Client.CMU14.Interface.Hud;
using Content.Client.Stylesheets;
using Content.Shared.Chat;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Systems.Chat.Widgets;

// CMU14 class: one scrollbar owns input, position and geometry for the virtualized history.
public sealed class ChatLogPanel : PanelContainer
{
    public const int MaxEntries = 2500;
    private const float BottomTolerance = 1f;
    private readonly VScrollBar _scrollBar;
    private readonly ChatLogList _rows;
    private readonly ChatScrollViewport _viewport;
    private readonly Button _scrollToLatest;
    private bool _updatingGeometry;
    private bool _followingBottom = true;
    private bool _scrollInputPending; // CMU14: row-height corrections must not undo explicit input.

    public int EntryCount => _rows.EntryCount;

    public ChatLogPanel()
    {
        HorizontalExpand = VerticalExpand = true;
        MouseFilter = MouseFilterMode.Pass; // Routed wheel events skip Ignore controls, even with a MouseWheel override.
        PanelOverride = new StyleBoxEmpty();
        _rows = new ChatLogList { HorizontalExpand = true };
        _scrollBar = new VScrollBar { VerticalExpand = true, MinWidth = 10 };
        _scrollBar.OnValueChanged += OnScrollChanged;
        if (StyleNano.CrtUiEnabled)
            _scrollBar.AddStyleClass(StyleNano.StyleClassCrtChatScrollBar);
        _viewport = new ChatScrollViewport(this);
        _viewport.AddChild(_rows);
        AddChild(new ChatScrollLayout(_viewport, _scrollBar));

        // Overlaying this control keeps the message viewport the same height when scrolling up.
        _scrollToLatest = new Button
        {
            Text = Loc.GetString("cmu-chat-scroll-latest"),
            Visible = false,
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Bottom,
            Margin = new Thickness(0, 0, 10, 3),
            MinSize = new Vector2(160, 20),
            StyleBoxOverride = new CmuHudStyleBox { Tone = CmuHudTone.Inset, Opacity = 0.9f },
        };
        if (StyleNano.CrtUiEnabled)
        {
            _scrollToLatest.AddStyleClass(StyleNano.StyleClassCrtButton);
            _scrollToLatest.Label.FontColorOverride = CrtTerminalPalette.Accent;
        }
        _scrollToLatest.OnPressed += _ => ScrollToBottom();
        AddChild(_scrollToLatest);
        RefreshChatFont();
    }

    public void RefreshChatFont()
    {
        _scrollToLatest.StyleBoxOverride = StyleNano.CrtUiEnabled
            ? new CmuHudStyleBox { Tone = CmuHudTone.Inset, Opacity = 0.9f } : null;
        _scrollToLatest.Label.FontOverride = StyleNano.CrtUiEnabled
            ? StyleNano.GetChatFont(IoCManager.Resolve<IResourceCache>()) : null;
        _scrollToLatest.Label.FontColorOverride = StyleNano.CrtUiEnabled ? CrtTerminalPalette.Accent : null;
    }

    public ChatLogEntry AddMessage(ChatMessage message, FormattedMessage formatted, Color color,
        Color? accentOverride = null, int? fontSize = null) =>
        AddMessage(message, () => formatted, color, accentOverride, fontSize);

    public ChatLogEntry AddMessage(ChatMessage message, Func<FormattedMessage> format, Color color,
        Color? accentOverride = null, int? fontSize = null)
    {
        var entry = new ChatLogEntry(message, format, color, accentOverride, fontSize);
        var removedHeight = _rows.Add(entry);
        if (!_followingBottom && removedHeight > 0)
        {
            _updatingGeometry = true;
            _scrollBar.Value = MathF.Max(0, _scrollBar.Value - removedHeight);
            _updatingGeometry = false;
        }
        _viewport.InvalidateMeasure();
        return entry;
    }

    public void Clear()
    {
        _rows.Clear();
        _followingBottom = true;
        _updatingGeometry = true;
        _scrollBar.Value = 0;
        _updatingGeometry = false;
        _scrollToLatest.Visible = false;
        _viewport.InvalidateMeasure();
    }

    public void ScrollToBottom()
    {
        _followingBottom = true;
        _scrollInputPending = false;
        _scrollToLatest.Visible = false;
        _viewport.InvalidateMeasure();
    }

    public void RefreshLayout(bool forceScrollToBottom = false)
    {
        foreach (var child in _rows.Children)
        {
            if (child is ChatMessageRow row)
                row.RefreshLayout();
            else
                child.InvalidateMeasure();
        }
        if (forceScrollToBottom)
            ScrollToBottom();
        _rows.InvalidateMeasure();
        _viewport.InvalidateMeasure();
    }

    private void OnScrollChanged(Robust.Client.UserInterface.Controls.Range range)
    {
        if (_updatingGeometry)
            return;
        _scrollInputPending = true;
        _followingBottom = _scrollBar.Value + _scrollBar.Page >= _scrollBar.MaxValue - BottomTolerance;
        _scrollToLatest.Visible = !_followingBottom;
        // Scroll changes must remeasure the virtualized window before applying its message anchor.
        _viewport.InvalidateMeasure();
    }

    private float PrepareViewport(Vector2 size)
    {
        var page = float.IsFinite(size.Y) ? MathF.Max(0, size.Y) : _viewport.Height;
        var width = float.IsFinite(size.X) ? MathF.Max(0, size.X) : MathF.Max(1, _viewport.Width);
        var input = _scrollBar.Value;
        _rows.SetViewport(input, page, _followingBottom);
        _rows.Measure(new Vector2(width, float.PositiveInfinity));
        if (_scrollInputPending && MathF.Abs(_rows.AnchoredScroll - input) > 0.01f)
        {
            // Revisit the requested window after estimates become exact, so preserving the
            // wheel offset cannot leave only the previous anchor's rows materialized.
            _rows.InvalidateMeasure();
            _rows.Measure(new Vector2(width, float.PositiveInfinity));
        }
        var content = _rows.DesiredSize.Y;
        var maximum = MathF.Max(0, content - page);
        // Newly materialized rows can change estimates by almost an entire wheel step.
        // Keep the player's requested offset during wheel/drag input; use a message anchor only
        // for passive reflow (resizing, font changes and incoming messages).
        var anchored = Math.Clamp(_scrollInputPending ? input : _rows.AnchoredScroll, 0, maximum);
        _scrollInputPending = false;
        var target = _scrollBar.ValueTarget;
        _updatingGeometry = true;
        try
        {
            _scrollBar.MaxValue = MathF.Max(content, page);
            _scrollBar.Page = page;
            if (_followingBottom || maximum == 0)
                _scrollBar.Value = maximum;
            else if (MathF.Abs(_scrollBar.Value - anchored) > 0.01f)
            {
                _scrollBar.Value = anchored;
                // Keep a native scrollbar drag/animation target when measured row heights change.
                _scrollBar.ValueTarget = target + anchored - input;
            }
        }
        finally
        {
            _updatingGeometry = false;
        }
        if (maximum == 0)
            _followingBottom = true;
        _scrollToLatest.Visible = !_followingBottom;
        return _scrollBar.Value;
    }

    protected override void MouseWheel(GUIMouseWheelEventArgs args)
    {
        base.MouseWheel(args);
        ScrollWheel(args.Delta.Y);
        if (args.Delta.Y != 0)
            args.Handle();
    }

    private void ScrollWheel(float delta)
    {
        if (delta == 0)
            return;
        if (delta > 0)
            _followingBottom = false;
        // No hidden second scrollbar, delayed bottom snap or per-frame target overwrite.
        _scrollInputPending = true;
        _scrollBar.Value = _scrollBar.ValueTarget - delta * 96;
        _viewport.InvalidateMeasure();
    }

    private sealed class ChatScrollViewport : Container
    {
        private readonly ChatLogPanel _owner;

        public ChatScrollViewport(ChatLogPanel owner)
        {
            _owner = owner;
            MouseFilter = MouseFilterMode.Pass;
            RectClipContent = true;
            HorizontalExpand = VerticalExpand = true;
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            // Floating Default HUD is measured with the screen's available area, then arranged
            // into its smaller anchored chat rectangle. Only ArrangeOverride knows the page and
            // wrapping width: changing the scrollbar here can clamp input and resume following
            // the bottom before that real rectangle is applied.
            return Vector2.Zero;
        }

        protected override Vector2 ArrangeOverride(Vector2 finalSize)
        {
            var offset = _owner.PrepareViewport(finalSize);
            _owner._rows.Arrange(UIBox2.FromDimensions(new Vector2(0, -offset),
                new Vector2(finalSize.X, MathF.Max(finalSize.Y, _owner._rows.DesiredSize.Y))));
            return finalSize;
        }
    }

    private sealed class ChatScrollLayout : Container
    {
        private readonly ChatScrollViewport _viewport;
        private readonly VScrollBar _bar;

        public ChatScrollLayout(ChatScrollViewport viewport, VScrollBar bar)
        {
            _viewport = viewport;
            _bar = bar;
            HorizontalExpand = VerticalExpand = true;
            AddChild(viewport);
            AddChild(bar);
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            _bar.Measure(availableSize);
            _viewport.Measure(new Vector2(MathF.Max(0, availableSize.X - _bar.DesiredSize.X), availableSize.Y));
            return new Vector2(_bar.DesiredSize.X, _bar.DesiredSize.Y);
        }

        protected override Vector2 ArrangeOverride(Vector2 finalSize)
        {
            var width = MathF.Max(0, finalSize.X - _bar.DesiredSize.X);
            _viewport.Arrange(UIBox2.FromDimensions(Vector2.Zero, new Vector2(width, finalSize.Y)));
            _bar.Arrange(new UIBox2(width, 0, finalSize.X, finalSize.Y));
            return finalSize;
        }
    }
}
