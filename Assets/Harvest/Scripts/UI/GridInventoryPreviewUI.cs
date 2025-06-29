using UnityEngine;
using UnityEngine.UI;
using static GridInventory;

public class GridInventoryPreviewUI : MonoBehaviour
{
    public RectTransform Rect => (RectTransform)transform;

    public void SetPreview(ItemUI itemUI, GridInventoryUI inventoryUI, GridInventoryUI.InteractResponse preview)
    {
        // If the preview is out of bounds hide the preview
        if (preview.inventoryResponse.type == ItemContainerInteractType.OutOfBounds)
        {
            gameObject.SetActive(false);
            return;
        }

        // Otherwise show to indicate the preview response
        gameObject.SetActive(true);

        // Resize to match the item size
        Rect.sizeDelta = itemUI.Rect.sizeDelta;

        // Move to the correct position in the inventory
        Vector2 localPos = inventoryUI.ConvertGridPosToLocalPos(preview.gridPos.Value.x, preview.gridPos.Value.y);
        Rect.position = (Vector2)inventoryUI.Rect.position + localPos;

        // Recolour based on the preview response
        switch (preview.inventoryResponse.type)
        {
            case ItemContainerInteractType.Placed:
            case ItemContainerInteractType.Pickup:
                image.color = colourValid;
                break;

            case ItemContainerInteractType.Stacked:
                image.color = colourStacked;
                break;

            case ItemContainerInteractType.Replaced:
                image.color = colourReplaced;
                break;

            case ItemContainerInteractType.Blocked:
            case ItemContainerInteractType.OutOfBounds:
            case ItemContainerInteractType.Invalid:
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
