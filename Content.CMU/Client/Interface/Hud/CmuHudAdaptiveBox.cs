using System.Numerics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.CMU14.Interface.Hud;

/// <summary>Preserve native box orientation until the user resizes its HUD family.</summary>
public sealed class CmuHudAdaptiveBox : BoxContainer, ICmuHudFlowContainer
{
    private readonly CmuHudFlowLayout _flow;
    public Vector2 ContentSize => _flow.ContentSize;
    public float MinimumItemWidth => _flow.MinimumItemWidth;

    public CmuHudAdaptiveBox() => _flow = new CmuHudFlowLayout(this);
    public Vector2 ReflowContent(float width) => _flow.Reflow(width);

    protected override Vector2 MeasureOverride(Vector2 availableSize) => CmuHudReflow.IsEnabled(this)
        ? _flow.Measure(CmuHudReflow.ConstrainWidth(this, availableSize.X), SeparationOverride ?? 4, SeparationOverride ?? 4)
        : base.MeasureOverride(availableSize);

    protected override Vector2 ArrangeOverride(Vector2 finalSize) => CmuHudReflow.IsEnabled(this)
        ? _flow.Arrange(finalSize)
        : base.ArrangeOverride(finalSize);
}
