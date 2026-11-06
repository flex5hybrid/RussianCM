using Robust.Client.Graphics;

namespace Content.Client.CMU14.Interface.Hud;

/// <summary>A CRT backing and border with native content margins and hit geometry.</summary>
public sealed class CmuCrtFillStyleBox : StyleBox
{
    public Color BackgroundColor { get; set; }
    public Color BorderColor { get; set; }
    public Thickness BorderThickness { get; set; }

    public CmuCrtFillStyleBox()
    {
    }

    public CmuCrtFillStyleBox(CmuCrtFillStyleBox other) : base(other)
    {
        BackgroundColor = other.BackgroundColor;
        BorderColor = other.BorderColor;
        BorderThickness = other.BorderThickness;
    }

    protected override void DoDraw(DrawingHandleScreen handle, UIBox2 box, float uiScale)
    {
        var thickness = BorderThickness.Scale(uiScale);
        CmuCrtWindowsUIController.DrawFill(handle, thickness.Deflate(box), BackgroundColor);
        var (left, top, right, bottom) = thickness;
        if (left > 0)
            CmuCrtWindowsUIController.DrawFill(handle, new UIBox2(box.Left, box.Top, box.Left + left, box.Bottom), BorderColor);
        if (top > 0)
            CmuCrtWindowsUIController.DrawFill(handle, new UIBox2(box.Left, box.Top, box.Right, box.Top + top), BorderColor);
        if (right > 0)
            CmuCrtWindowsUIController.DrawFill(handle, new UIBox2(box.Right - right, box.Top, box.Right, box.Bottom), BorderColor);
        if (bottom > 0)
            CmuCrtWindowsUIController.DrawFill(handle, new UIBox2(box.Left, box.Bottom - bottom, box.Right, box.Bottom), BorderColor);
    }

    protected override float GetDefaultContentMargin(Margin margin) => margin switch
    {
        Margin.Left => BorderThickness.Left,
        Margin.Top => BorderThickness.Top,
        Margin.Right => BorderThickness.Right,
        Margin.Bottom => BorderThickness.Bottom,
        _ => throw new ArgumentOutOfRangeException(nameof(margin)),
    };
}
