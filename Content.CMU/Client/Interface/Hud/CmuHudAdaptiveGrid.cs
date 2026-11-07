using System.Numerics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.CMU14.Interface.Hud;

/// <summary>Keep authored row/column rules by default; custom HUD families use width-driven flow.</summary>
[Virtual]
public class CmuHudAdaptiveGrid : GridContainer, ICmuHudFlowContainer
{
    private readonly CmuHudFlowLayout _flow;
    public Vector2 ContentSize => _flow.ContentSize;
    public float MinimumItemWidth => _flow.MinimumItemWidth;

    public CmuHudAdaptiveGrid() => _flow = new CmuHudFlowLayout(this);
    public Vector2 ReflowContent(float width) => _flow.Reflow(width);

    protected override Vector2 MeasureOverride(Vector2 availableSize) => CmuHudReflow.IsEnabled(this)
        ? _flow.Measure(CmuHudReflow.ConstrainWidth(this, availableSize.X), HSeparationOverride ?? 4, VSeparationOverride ?? 4)
        : base.MeasureOverride(availableSize);

    protected override Vector2 ArrangeOverride(Vector2 finalSize) => CmuHudReflow.IsEnabled(this)
        ? _flow.Arrange(finalSize, backwards: ExpandBackwards)
        : base.ArrangeOverride(finalSize);
}
