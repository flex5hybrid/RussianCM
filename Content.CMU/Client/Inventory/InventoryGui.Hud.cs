namespace Content.Client.UserInterface.Systems.Inventory.Widgets;

public sealed partial class InventoryGui
{
    /// <summary>Expose populated equipment as one view, independently of inline storage windows.</summary>
    public void CMUSetEquipmentVisible(bool visible)
    {
        InventoryHotbar.Visible = visible;
        EquipmentPanel.Visible = visible;
        InventoryButton.Highlight = visible;
        if (!visible)
            return;

        // A drawer may have been measured while its slots were hidden. Refresh the scroll
        // content before its first visible layout instead of relying on a second toggle.
        InventoryHotbar.InvalidateMeasure();
        EquipmentScroll.InvalidateMeasure();
        EquipmentPanel.InvalidateMeasure();
        InvalidateMeasure();

        // Inline storage is a sibling HUD subtree. Keep equipment above it when both are open.
        if (Parent != null)
            SetPositionInParent(Parent.ChildCount - 1);
    }
}
