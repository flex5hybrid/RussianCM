using Content.Client.Stylesheets;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Client.CMU14.Interface.Hud;

/// <summary>List rows keep their legacy material when disabled and never tint their item/text children.</summary>
public sealed class CmuHudListStyleBox : StyleBox
{
    private readonly ContainerButton _button;

    public CmuHudListStyleBox(ContainerButton button) => _button = button;

    protected override void DoDraw(DrawingHandleScreen handle, UIBox2 box, float uiScale)
    {
        var highlighted = _button.DrawMode is BaseButton.DrawModeEnum.Hover or BaseButton.DrawModeEnum.Pressed;
        var background = !StyleNano.CrtUiEnabled ? Color.White
            : highlighted ? CmuHudStyle.HoverColor : CmuHudStyle.InsetColor;
        handle.DrawRect(box, background);
        if (StyleNano.CrtUiEnabled && highlighted)
            handle.DrawRect(box, CmuHudStyle.SelectionColor, false);
    }

    protected override float GetDefaultContentMargin(Margin margin) => 0;
}
