using System.Numerics;
using Robust.Client.UserInterface;

namespace Content.Client.CMU14.Interface.Hud;

public interface ICmuHudFlowContainer
{
    Vector2 ContentSize { get; }
    float MinimumItemWidth { get; }
    Vector2 ReflowContent(float width);
}

/// <summary>One row-packing policy for all HUD families, without changing control ownership.</summary>
internal sealed class CmuHudFlowLayout(Control owner)
{
    private readonly List<Row> _rows = new();
    private readonly record struct Row(int Start, int End, float Width, float Height);
    private float _width = float.NaN;
    private float _horizontalGap;
    private float _verticalGap;
    public Vector2 ContentSize { get; private set; }
    public float MinimumItemWidth { get; private set; }
    public float ContentLeft { get; private set; }

    public Vector2 Measure(float width, float horizontalGap = 4, float verticalGap = 4)
    {
        _horizontalGap = horizontalGap;
        _verticalGap = verticalGap;
        foreach (var child in owner.Children)
            child.Measure(new Vector2(float.PositiveInfinity));
        BuildRows(width);
        return ContentSize;
    }

    public Vector2 Reflow(float width)
    {
        BuildRows(width);
        return ContentSize;
    }

    private void BuildRows(float width)
    {
        _rows.Clear();
        _width = width;
        MinimumItemWidth = 1;
        var start = 0;
        var rowWidth = 0f;
        var rowHeight = 0f;
        var count = 0;
        var total = Vector2.Zero;
        for (var i = 0; i < owner.ChildCount; i++)
        {
            var child = owner.GetChild(i);
            var size = child.DesiredSize;
            if (size.X <= 0 || size.Y <= 0)
                continue;
            var minimum = child is ICmuHudFlowContainer flow
                ? flow.MinimumItemWidth + child.Margin.SumHorizontal : size.X;
            MinimumItemWidth = MathF.Max(MinimumItemWidth, minimum);
            if (count > 0 && rowWidth + _horizontalGap + size.X > width)
            {
                _rows.Add(new Row(start, i, rowWidth, rowHeight));
                total.X = MathF.Max(total.X, rowWidth);
                total.Y += rowHeight + _verticalGap;
                start = i;
                rowWidth = rowHeight = 0;
                count = 0;
            }
            rowWidth += (count++ > 0 ? _horizontalGap : 0) + size.X;
            rowHeight = MathF.Max(rowHeight, size.Y);
        }
        if (count > 0)
        {
            _rows.Add(new Row(start, owner.ChildCount, rowWidth, rowHeight));
            total.X = MathF.Max(total.X, rowWidth);
            total.Y += rowHeight;
        }
        ContentSize = total;
    }

    public Vector2 Arrange(Vector2 size, bool center = false, bool backwards = false)
    {
        if (_width != size.X)
            BuildRows(size.X);
        ContentLeft = center ? size.X / 2 : 0;
        var y = backwards ? ContentSize.Y : 0;
        foreach (var row in _rows)
        {
            if (backwards)
                y -= row.Height;
            var x = center ? MathF.Max(0, (size.X - row.Width) / 2) : 0;
            ContentLeft = MathF.Min(ContentLeft, x);
            for (var i = row.Start; i < row.End; i++)
            {
                var child = owner.GetChild(i);
                var childSize = child.DesiredSize;
                if (childSize.X <= 0 || childSize.Y <= 0)
                    continue;
                child.Arrange(UIBox2.FromDimensions(new Vector2(x, y + row.Height - childSize.Y), childSize));
                x += childSize.X + _horizontalGap;
            }
            y += backwards ? -_verticalGap : row.Height + _verticalGap;
        }
        return size;
    }
}
