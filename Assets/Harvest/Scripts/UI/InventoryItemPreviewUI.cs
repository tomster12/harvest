using UnityEngine;
using UnityEngine.UI;
using static Inventory;

public class InventoryItemPreviewUI : MonoBehaviour
{
    public RectTransform Rect => (RectTransform)transform;

    public void SetPreview(InventoryItemUI inventoryItemUI, InventoryUI inventoryUI, (Vector2Int, (ItemPlaceResponse, ItemInstance)) preview)
    {
        // If the preview is out of bounds, hide the preview
        if (preview.Item2.Item1 == ItemPlaceResponse.OutOfBounds)
        {
            gameObject.SetActive(false);
            return;
        }

        // Otherwise show to indicate the preview response
        gameObject.SetActive(true);

        // Resize to match the item size
        Rect.sizeDelta = inventoryItemUI.Rect.sizeDelta;

        // Move to the correct position in the inventory
        Vector2 localPos = inventoryUI.ConvertGridPosToLocalPos(preview.Item1.x, preview.Item1.y);
        Rect.position = (Vector2)inventoryUI.Rect.position + localPos;

        // Recolour based on the preview response
        switch (preview.Item2.Item1)
        {
            case ItemPlaceResponse.Placed:
                image.color = colourValid;
                break;

            case ItemPlaceResponse.Stacked:
                image.color = colourStacked;
                break;

            case ItemPlaceResponse.Replaced:
                image.color = colourReplaced;
                break;

            case ItemPlaceResponse.Blocked:
                image.color = colourBlocked;
                break;
        }
    }

    public void HidePreview()
    {
        gameObject.SetActive(false);
    }

    [Header("References")]
    [SerializeField] private Image image;

    [Header("Config")]
    [SerializeField] private Color colourValid = Color.green;
    [SerializeField] private Color colourStacked = Color.yellow;
    [SerializeField] private Color colourReplaced = Color.red;
    [SerializeField] private Color colourBlocked = Color.gray;
}
