using Content.Client.UserInterface.Systems.Inventory.Widgets;

namespace Content.Client.UserInterface.Systems.Inventory;

public sealed partial class InventoryUIController
{
    private void CMUUseActiveInventoryBar()
    {
        if (UIManager.GetActiveUIWidgetOrNull<InventoryGui>() is not { } gui)
            return;

        var bar = gui.InventoryHotbar;
        if (_inventoryHotbar != bar || !_slotGroups.TryGetValue(bar.SlotGroup, out var registered) || registered != bar)
            RegisterInventoryBarContainer(bar);

        if (_inventoryButton != gui.InventoryButton)
            RegisterInventoryButton(gui.InventoryButton);
    }

    private void CMUSetInventoryBarVisible(bool visible)
    {
        if (_inventoryHotbar == null)
            return;

        // Teardown can run after the screen stopped being active. Follow the registered
        // container's actual owner so its panel cannot remain visible with cleared slots.
        for (var owner = _inventoryHotbar.Parent; owner != null; owner = owner.Parent)
        {
            if (owner is not InventoryGui gui)
                continue;
            gui.CMUSetEquipmentVisible(visible);
            return;
        }

        _inventoryHotbar.Visible = visible;
    }
}
