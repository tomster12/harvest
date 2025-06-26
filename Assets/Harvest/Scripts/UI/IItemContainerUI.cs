public interface IItemContainerUI
{
    GridInventoryUI.InteractResponse PlaceOrStackHeldItem(ItemUI heldItemUI, bool preview = false);

    GridInventoryUI.InteractResponse PickupItem(ItemUI heldItemUI, ItemUI hoveredItemUI);

    void HoverPreview(ItemUI heldItemUI, ItemUI hoveredItemUI);

    void DisablePreview();
}
