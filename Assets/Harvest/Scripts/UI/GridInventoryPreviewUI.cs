using UnityEngine;
using UnityEngine.UI;
using static GridInventory;

public class GridInventoryPreviewUI : MonoBehaviour
{
    public RectTransform Rect => (RectTransform)transform;

    public void SetPreview(ItemUI itemUI, GridInventoryUI inventoryUI, GridInventoryUI.InteractResponse preview)
    {
        // If the preview is out of bounds hide the preview
        if (preview.inventoryResponse.type == InteractResponseType.OutOfBounds)
        {
            gameObject.SetActive(false);
            return;
        }

        // Otherwise show to indicate the preview response
        gameObject.SetActive(true);

        // Resize to match the item size
        Rect.sizeDelta = itemUI.Rect.sizeDelta;

        // Move to the correct position in the inventory
        Vector2 localPos = inventoryUI.ConvertGridPosToLocalPos(preview.slot.x, preview.slot.y);
        Rect.position = (Vector2)inventoryUI.Rect.position + localPos;

        // Recolour based on the preview response
        switch (preview.inventoryResponse.type)
        {
            case InteractResponseType.Placed:
            case InteractResponseType.Removed:
                image.color = colourValid;
                break;

            case InteractResponseType.Stacked:
                image.color = colourStacked;
                break;

            case InteractResponseType.Replaced:
                image.color = colourReplaced;
                break;

            case InteractResponseType.Blocked:
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
