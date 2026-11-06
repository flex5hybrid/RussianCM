using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.CMU14.Interface.Hud;

/// <summary>Transparent item groups with a uniform gap, wrapping only when the viewport cannot fit them.</summary>
public sealed class CmuHudDock : Container
{
    private const float Gap = 4;
    private readonly List<Row> _rows = new();
    private readonly record struct Row(int Start, int Count, float Width, float Height);
    public float ContentLeft { get; private set; }
    public event Action? ContentPositionChanged;

    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        _rows.Clear();
        var start = 0;
        var count = 0;
        var width = 0f;
        var height = 0f;
        var total = Vector2.Zero;
        for (var i = 0; i < ChildCount; i++)
        {
            var child = GetChild(i);
            child.Measure(new Vector2(float.PositiveInfinity));
            var size = child.DesiredSize;
            if (size.X <= 0)
                continue;
            if (count > 0 && width + Gap + size.X > availableSize.X)
            {
                _rows.Add(new Row(start, i - start, width, height));
                total.X = MathF.Max(total.X, width);
                total.Y += height + Gap;
                start = i;
                count = 0;
                width = height = 0;
            }
            width += (count++ > 0 ? Gap : 0) + size.X;
            height = MathF.Max(height, size.Y);
        }
        if (count > 0)
        {
            _rows.Add(new Row(start, ChildCount - start, width, height));
            total.X = MathF.Max(total.X, width);
            total.Y += height;
        }
        return total;
    }

    protected override Vector2 ArrangeOverride(Vector2 finalSize)
    {
        var previous = ContentLeft;
        ContentLeft = finalSize.X / 2;
        var y = 0f;
        foreach (var row in _rows)
        {
            var x = MathF.Max(0, (finalSize.X - row.Width) / 2);
            ContentLeft = MathF.Min(ContentLeft, x);
            for (var i = row.Start; i < row.Start + row.Count; i++)
            {
                var child = GetChild(i);
                var size = child.DesiredSize;
                child.Arrange(UIBox2.FromDimensions(new Vector2(x, y + row.Height - size.Y), size));
                if (size.X > 0)
                    x += size.X + Gap;
            }
            y += row.Height + Gap;
        }
        if (previous != ContentLeft)
            ContentPositionChanged?.Invoke();
        return finalSize;
    }
}
