public interface IItemContainerUI
{
    void PlaceHeldItem(ItemUI heldItemUI, ItemUI hoveredItemUI);

    void PickupItem(ItemUI heldItemUI, ItemUI hoveredItemUI);

    void HoverPreview(ItemUI heldItemUI, ItemUI hoveredItemUI);

    void DisablePreview();
}
