using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.CMU14.Interface.Hud;

/// <summary>
/// Three functional groups: worn items, hands and item status, pockets. On a narrow viewport the
/// worn items and pockets move above the hands rather than shrinking hit targets or hiding slots.
/// </summary>
public sealed class CmuHudDock : Container
{
    private const float Gap = 8;
    public bool Stacked { get; private set; }
    public float ContentLeft { get; private set; }
    public event Action? ContentPositionChanged;

    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        if (ChildCount != 3)
            return base.MeasureOverride(availableSize);

        for (var i = 0; i < ChildCount; i++)
            GetChild(i).Measure(new Vector2(availableSize.X, float.PositiveInfinity));
        var left = GetChild(0).DesiredSize;
        var middle = GetChild(1).DesiredSize;
        var right = GetChild(2).DesiredSize;
        Stacked = left.X + middle.X + right.X + Gap * 2 > availableSize.X;
        if (!Stacked)
            return new Vector2(left.X + middle.X + right.X + Gap * 2, MathF.Max(middle.Y, MathF.Max(left.Y, right.Y)));
        return new Vector2(MathF.Max(middle.X, left.X + right.X + Gap), middle.Y + MathF.Max(left.Y, right.Y) + Gap);
    }

    protected override Vector2 ArrangeOverride(Vector2 finalSize)
    {
        if (ChildCount != 3)
            return base.ArrangeOverride(finalSize);
        var left = GetChild(0).DesiredSize;
        var middle = GetChild(1).DesiredSize;
        var right = GetChild(2).DesiredSize;
        var previousLeft = ContentLeft;
        if (!Stacked)
        {
            var x = MathF.Max(0, (finalSize.X - left.X - middle.X - right.X - Gap * 2) / 2);
            ContentLeft = x;
            Arrange(0, x, finalSize.Y - left.Y, left);
            Arrange(1, x + left.X + Gap, finalSize.Y - middle.Y, middle);
            Arrange(2, x + left.X + middle.X + Gap * 2, finalSize.Y - right.Y, right);
        }
        else
        {
            var x = MathF.Max(0, (finalSize.X - left.X - right.X - Gap) / 2);
            ContentLeft = x;
            Arrange(0, x, 0, left);
            Arrange(2, x + left.X + Gap, 0, right);
            Arrange(1, MathF.Max(0, (finalSize.X - middle.X) / 2), finalSize.Y - middle.Y, middle);
        }
        if (previousLeft != ContentLeft)
            ContentPositionChanged?.Invoke();
        return finalSize;
    }

    private void Arrange(int index, float x, float y, Vector2 size) =>
        GetChild(index).Arrange(UIBox2.FromDimensions(new Vector2(x, y), size));
}
