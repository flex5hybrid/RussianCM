using Content.Client.Stylesheets;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.CMU14.Interface.Hud;

/// <summary>List rows keep their legacy material when disabled and never tint their item/text children.</summary>
public sealed class CmuHudListStyleBox : StyleBoxFlat
{
    private readonly ContainerButton _button;

    public CmuHudListStyleBox(ContainerButton button) => _button = button;

    protected override void DoDraw(DrawingHandleScreen handle, UIBox2 box, float uiScale)
    {
        var highlighted = _button.DrawMode is BaseButton.DrawModeEnum.Hover or BaseButton.DrawModeEnum.Pressed;
        BackgroundColor = !StyleNano.CrtUiEnabled ? Color.White
            : highlighted ? CmuHudStyle.HoverColor : CmuHudStyle.InsetColor;
        base.DoDraw(handle, box, uiScale);
        if (StyleNano.CrtUiEnabled && highlighted)
            handle.DrawRect(box, CmuHudStyle.SelectionColor, false);
    }
}
