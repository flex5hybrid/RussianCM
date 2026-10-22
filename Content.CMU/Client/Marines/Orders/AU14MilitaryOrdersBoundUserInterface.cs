using Content.Client.UserInterface.Controls;
using Content.Shared.CMU14.Marines.Orders;
using Robust.Client.UserInterface;
using Robust.Shared.Utility;

namespace Content.Client.CMU14.Marines.Orders;

public sealed class AU14MilitaryOrdersBoundUserInterface(EntityUid owner, Enum uiKey)
    : BoundUserInterface(owner, uiKey)
{
    private SimpleRadialMenu? _menu;

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<SimpleRadialMenu>();
        _menu.SetButtons(
        [
            MakeOption(AU14MilitaryOrder.Silence, "au14-military-order-silence", "Interface/Emotes/whistle.png"),
            MakeOption(AU14MilitaryOrder.Salute, "au14-military-order-salute", "Interface/Emotes/salute.png"),
            MakeOption(AU14MilitaryOrder.Attention, "au14-military-order-attention", "Interface/Emotes/salute.png"),
        ]);
        _menu.OpenOverMouseScreenPosition();
    }

    private RadialMenuOptionBase MakeOption(AU14MilitaryOrder order, string title, string icon)
    {
        return new RadialMenuActionOption<AU14MilitaryOrder>(SelectOrder, order)
        {
            ToolTip = Loc.GetString(title),
            IconSpecifier = RadialMenuIconSpecifier.With(
                new SpriteSpecifier.Texture(new ResPath(icon))),
        };
    }

    private void SelectOrder(AU14MilitaryOrder order)
    {
        SendPredictedMessage(new AU14MilitaryOrderSelectedBuiMsg(order));
    }
}
