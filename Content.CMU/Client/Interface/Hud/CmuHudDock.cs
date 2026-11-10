using System.Numerics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.CMU14.Interface.Hud;

/// <summary>Transparent dock using the same row packing as resized HUD families.</summary>
public sealed class CmuHudDock : Container, ICmuHudFlowContainer
{
    private readonly CmuHudFlowLayout _flow;
    public Vector2 ContentSize => _flow.ContentSize;
    public float MinimumItemWidth => _flow.MinimumItemWidth;
    public float ContentLeft => _flow.ContentLeft;
    public event Action? ContentPositionChanged;

    public CmuHudDock() => _flow = new CmuHudFlowLayout(this);
    public Vector2 ReflowContent(float width) => _flow.Reflow(width);

    protected override Vector2 MeasureOverride(Vector2 availableSize) =>
        _flow.Measure(CmuHudReflow.ConstrainWidth(this, availableSize.X));

    protected override Vector2 ArrangeOverride(Vector2 finalSize)
    {
        var previous = ContentLeft;
        var result = _flow.Arrange(finalSize, center: true);
        if (previous != ContentLeft)
            ContentPositionChanged?.Invoke();
        return result;
    }
}
