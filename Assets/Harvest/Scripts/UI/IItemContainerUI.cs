public interface IItemContainerUI
{
    void ClickItem(ItemUI heldItemUI, ItemUI hoveredItemUI);

    void PreviewClickItem(ItemUI heldItemUI, ItemUI hoveredItemUI);

    void DisablePreview();
}
