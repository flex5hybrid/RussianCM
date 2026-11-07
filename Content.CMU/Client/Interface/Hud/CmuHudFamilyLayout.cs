using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.CMU14.Interface.Hud;

/// <summary>Own custom sizing constraints for one family and release them on reset/cancel.</summary>
internal sealed class CmuHudFamilyLayout(Control root, string id)
{
    private readonly Dictionary<Control, Constraints> _native = new();
    private Control? _host;
    private int _version = -1;
    private Vector2 _lastSize;
    private float _minimumWidth = 1;
    private bool _hasSize;
    private Vector2 _lastRequested;
    private Vector2 _lastAvailable;
    private Vector2 _lastContent;
    private float _lastItemWidth;
    public string Id { get; } = id;

    private readonly record struct Constraints(Vector2 SetSize, Vector2 MaxSize,
        Control.HAlignment Horizontal, Control.VAlignment Vertical)
    {
        public static Constraints Capture(Control control) =>
            new(control.SetSize, control.MaxSize, control.HorizontalAlignment, control.VerticalAlignment);

        public void Restore(Control control)
        {
            SetSizeIfChanged(control, SetSize);
            if (control.MaxSize != MaxSize) control.MaxSize = MaxSize;
            if (control.HorizontalAlignment != Horizontal) control.HorizontalAlignment = Horizontal;
            if (control.VerticalAlignment != Vertical) control.VerticalAlignment = Vertical;
        }
    }

    public Vector2 Prepare(Vector2 requested, Vector2 available, int version)
    {
        requested = Vector2.Min(Vector2.Max(Vector2.Max(Vector2.One, root.MinSize), requested), available);
        if (_hasSize && _version == version && requested == _lastRequested && available == _lastAvailable
            && root.IsMeasureValid && root.IsArrangeValid && ConstraintsUnchanged()
            && (_host is not { } previousHost || previousHost is not ICmuHudFlowContainer previousFlow ||
                previousHost.IsMeasureValid && previousHost.IsArrangeValid && previousFlow.ContentSize == _lastContent
                && previousFlow.MinimumItemWidth == _lastItemWidth))
            return _lastSize;
        if (_version != version)
        {
            var host = FindHost(root);
            if (host != _host)
                _minimumWidth = 1;
            _host = host;
            _version = version;
        }
        // Keep the last effective height while measuring. Starting at the requested short
        // height each frame would repeatedly invalidate ancestors before growing it again.
        var previousWidth = _hasSize && _host != null ? _lastSize.X : requested.X;
        var size = new Vector2(MathF.Min(available.X, MathF.Max(MathF.Max(requested.X, _minimumWidth), previousWidth)),
            _hasSize ? MathF.Min(available.Y, MathF.Max(requested.Y, _lastSize.Y)) : requested.Y);
        CmuHudReflow.SetBounds(root, size);
        Relax(root, true);
        SetSizeIfChanged(root, size);
        if (_host is { } hostControl && hostControl is ICmuHudFlowContainer flow)
        {
            for (var control = hostControl; control != root && control != null; control = control.Parent)
                Relax(control, false);
            var insets = GetInsets(hostControl);
            void MeasureHost()
            {
                var width = MathF.Max(1, size.X - insets.X + hostControl.Margin.SumHorizontal);
                hostControl.Measure(new Vector2(width, float.PositiveInfinity));
            }
            MeasureHost();
            _minimumWidth = flow.MinimumItemWidth + insets.X;
            var width = MathF.Min(available.X, MathF.Max(requested.X, _minimumWidth));
            // Do not create an off-screen column when its buttons cannot fit vertically.
            // Probe row packing without repeatedly changing the root's sizing constraints.
            var step = MathF.Max(1, flow.MinimumItemWidth + 4);
            while (flow.ReflowContent(MathF.Max(1, width - insets.X)).Y + insets.Y > available.Y && width < available.X)
                width = MathF.Min(available.X, width + step);
            if (width != size.X)
            {
                size.X = width;
                CmuHudReflow.SetBounds(root, size);
                SetSizeIfChanged(root, size);
                MeasureHost();
            }
            flow.ReflowContent(MathF.Max(1, size.X - insets.X));
            // Nested families can themselves gain rows at the narrower width.
            while (flow.ContentSize.Y + insets.Y > available.Y && size.X < available.X)
            {
                size.X = MathF.Min(available.X, size.X + step);
                CmuHudReflow.SetBounds(root, size);
                SetSizeIfChanged(root, size);
                MeasureHost();
                flow.ReflowContent(MathF.Max(1, size.X - insets.X));
            }
            size.Y = MathF.Min(available.Y, MathF.Max(requested.Y, flow.ContentSize.Y + insets.Y));
        }
        else
            size = requested;
        CmuHudReflow.SetBounds(root, size);
        SetSizeIfChanged(root, size);
        root.Measure(root.Margin.Inflate(size));
        _lastSize = size;
        _lastRequested = requested;
        _lastAvailable = available;
        if (_host is ICmuHudFlowContainer finalFlow)
        {
            _lastContent = finalFlow.ContentSize;
            _lastItemWidth = finalFlow.MinimumItemWidth;
        }
        _hasSize = true;
        return size;
    }

    private bool ConstraintsUnchanged()
    {
        foreach (var control in _native.Keys)
        {
            if (control != root && !control.VisibleInTree)
                continue;
            var expected = control == root ? _lastSize : new Vector2(float.NaN);
            if (!control.SetWidth.Equals(expected.X) || !control.SetHeight.Equals(expected.Y)
                || control.MaxSize != new Vector2(float.PositiveInfinity)
                || control.HorizontalAlignment != Control.HAlignment.Stretch
                || control.VerticalAlignment != Control.VAlignment.Stretch)
                return false;
        }
        return CmuHudReflow.IsRoot(root);
    }

    private void Relax(Control control, bool isRoot)
    {
        _native.TryAdd(control, Constraints.Capture(control));
        if (control.HorizontalAlignment != Control.HAlignment.Stretch)
            control.HorizontalAlignment = Control.HAlignment.Stretch;
        if (control.VerticalAlignment != Control.VAlignment.Stretch)
            control.VerticalAlignment = Control.VAlignment.Stretch;
        if (control.MaxSize != new Vector2(float.PositiveInfinity))
            control.MaxSize = new Vector2(float.PositiveInfinity);
        if (!isRoot)
            SetSizeIfChanged(control, new Vector2(float.NaN));
    }

    private Vector2 GetInsets(Control host)
    {
        var insets = Vector2.Zero;
        for (var control = host; control != null; control = control.Parent)
        {
            if (control != root)
                insets += new Vector2(control.Margin.SumHorizontal, control.Margin.SumVertical);
            if (control is PanelContainer panel)
            {
                var box = panel.PanelOverride;
                if (box == null && panel.TryGetStyleProperty<StyleBox>(PanelContainer.StylePropertyPanel, out var styled))
                    box = styled;
                if (box != null)
                    insets += box.MinimumSize;
            }
            if (control == root)
                break;
        }
        return insets;
    }

    public void Reset()
    {
        CmuHudReflow.SetBounds(root, Vector2.Zero);
        foreach (var (control, constraints) in _native)
        {
            if (!control.Disposed)
                constraints.Restore(control);
        }
        // Ancestors may otherwise reuse rectangles measured with the explicit custom size.
        for (var control = root; control != null; control = control.Parent)
            control.InvalidateMeasure();
        _native.Clear();
    }

    private static void SetSizeIfChanged(Control control, Vector2 size)
    {
        if (!control.SetWidth.Equals(size.X)) control.SetWidth = size.X;
        if (!control.SetHeight.Equals(size.Y)) control.SetHeight = size.Y;
    }

    private static Control? FindHost(Control control)
    {
        if (control is ICmuHudFlowContainer)
        {
            // HandsContainer wraps its actual button grid in another slot container.
            // Its minimum width is one hand, not the full currently measured inner grid.
            if (control.ChildCount == 1 && control.GetChild(0) is ICmuHudFlowContainer)
                return FindHost(control.GetChild(0));
            return control;
        }
        foreach (var child in control.Children)
        {
            if (FindHost(child) is { } host)
                return host;
        }
        return null;
    }
}
