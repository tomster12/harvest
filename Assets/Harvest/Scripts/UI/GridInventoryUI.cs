using System.Collections.Generic;
using UnityEngine;
using static GridInventory;

public class GridInventoryUI : MonoBehaviour, IItemContainerUI
{
    // Values calculated from the image
    public static float BG_IMAGE_GRID_SIZE = 80;
    public static float BG_IMAGE_BORDER_SIZE = 1;
    public static float BG_IMAGE_PPU = 100;
    public static float GRID_SIZE => BG_IMAGE_GRID_SIZE / (BG_IMAGE_PPU / 100.0f);
    public static float GRID_BORDER_SIZE => BG_IMAGE_BORDER_SIZE / (BG_IMAGE_PPU / 100.0f);

    public struct InteractResponse
    {
        public GridInventory.InteractResponse inventoryResponse;
        public Vector2Int slot;

        public InteractResponse(GridInventory.InteractResponse inventoryResponse, Vector2Int slot)
        {
            this.inventoryResponse = inventoryResponse;
            this.slot = slot;
        }
    }

    public RectTransform Rect => (RectTransform)transform;

    public GridInventory Inventory { get; private set; }

    public static Vector2 GetGridSize(int gridSizeX, int gridSizeY)
    {
        float width = gridSizeX * (GRID_SIZE + GRID_BORDER_SIZE) - GRID_BORDER_SIZE;
        float height = gridSizeY * (GRID_SIZE + GRID_BORDER_SIZE) - GRID_BORDER_SIZE;
        return new Vector2(width, height);
    }

    public void SetInventory(GridInventory newInventory)
    {
        // Clear up old inventory
        if (Inventory != null)
        {
            Inventory.OnItemAdded -= OnItemAdded;
            Inventory.OnItemRemoved -= OnItemRemoved;
        }

        foreach (Transform child in itemContainer) DestroyImmediate(child.gameObject);
        itemUIs.Clear();

        // Set new inventory and subscribe to events
        Inventory = newInventory;
        Inventory.OnItemAdded += OnItemAdded;
        Inventory.OnItemRemoved += OnItemRemoved;

        // Rescale main panel to fit the inventory size
        Rect.sizeDelta = GetGridSize(Inventory.SizeX, Inventory.SizeY);

        // Create item UIs for existing items in the inventory
        foreach (ItemInstance itemInstance in Inventory.ItemInstances) OnItemAdded(itemInstance);
    }

    public InteractResponse PlaceOrStackHeldItem(ItemUI heldItemUI, bool preview = false)
    {
        // Try and place the item into the inventory
        Vector2 offsetTopLeft = (Vector2)heldItemUI.Rect.position + GetGridSize(1, 1) / 2;
        Vector2Int hoveringSlot = ConvertWorldToGridPos(offsetTopLeft);
        GridInventory.InteractResponse response = Inventory.PlaceOrStackItem(heldItemUI.ItemInstance, hoveringSlot.x, hoveringSlot.y, preview);

        if (preview) return new InteractResponse(response, hoveringSlot);

        // Placed or stacked so turn off held item
        if (response.type == InteractResponseType.Placed || (response.type == InteractResponseType.Stacked && heldItemUI.ItemInstance.Amount == 0))
        {
            heldItemUI.SetItem(null);
        }

        // Placed and swapped with a new item
        else if (response.type == InteractResponseType.Replaced)
        {
            heldItemUI.SetItem(response.itemInstance);
            heldItemUI.SetHeldByMouse(heldItemUI.MouseOffset);
        }

        return new InteractResponse(response, hoveringSlot);
    }

    public InteractResponse PickupItem(ItemUI heldItemUI, ItemUI hoveredItemUI)
    {
        Debug.Assert(hoveredItemUI.ContainerUI == (IItemContainerUI)this, "Cannot pick up item from another inventory UI");

        Vector2 hoveredItemPos = hoveredItemUI.Rect.position;
        GridInventory.InteractResponse response = Inventory.RemoveItem(hoveredItemUI.ItemInstance);

        if (response.type == InteractResponseType.Removed)
        {
            heldItemUI.SetItem(hoveredItemUI.ItemInstance);
            Vector2 offset = (Vector2)Input.mousePosition - hoveredItemPos;
            heldItemUI.SetHeldByMouse(offset);
            return new InteractResponse(response, hoveredItemUI.ItemInstance.InventoryPosition);
        }

        return new InteractResponse(response, Vector2Int.zero);
    }

    public void HoverPreview(ItemUI heldItemUI, ItemUI hoveredItemUI)
    {
        if (heldItemUI.State != ItemUI.StateType.EMPTY)
        {
            // Holding an item, so preview whether can place it
            var preview = PlaceOrStackHeldItem(heldItemUI, true);
            previewUI.SetPreview(heldItemUI, this, preview);
        }
        else if (hoveredItemUI != null && hoveredItemUI.ContainerUI == (IItemContainerUI)this)
        {
            // Not holding anything, so preview hovering and removing an item
            Vector2Int slot = hoveredItemUI.ItemInstance.InventoryPosition;
            InteractResponse response = new(new GridInventory.InteractResponse(InteractResponseType.Removed, hoveredItemUI.ItemInstance), slot);
            previewUI.SetPreview(hoveredItemUI, this, response);
        }
        else
        {
            // Otherwise disable the preview
            DisablePreview();
        }
    }

    public void DisablePreview()
    {
        previewUI.HidePreview();
    }

    public Vector2Int ConvertWorldToGridPos(Vector2 worldPos)
    {
        Vector2 localPos = worldPos - (Vector2)Rect.position;
        int x = Mathf.FloorToInt(localPos.x / (GRID_SIZE + GRID_BORDER_SIZE));
        int y = Mathf.FloorToInt(localPos.y / (GRID_SIZE + GRID_BORDER_SIZE));
        return new Vector2Int(x, -y);
    }

    public Vector2 ConvertGridPosToLocalPos(int gridPosX, int gridPosY)
    {
        float x = gridPosX * (GRID_SIZE + GRID_BORDER_SIZE);
        float y = gridPosY * (GRID_SIZE + GRID_BORDER_SIZE);
        return new Vector2(x, -y);
    }

    [Header("References")]
    [SerializeField] private RectTransform itemContainer;

    [Header("Prefabs")]
    [SerializeField] private GameObject itemUIPrefab;
    [SerializeField] private GameObject previewUIPrefab;

    private Dictionary<ItemInstance, ItemUI> itemUIs = new();
    private GridInventoryPreviewUI previewUI;

    private void Awake()
    {
        // Move the rect arbitrarily
        Rect.anchoredPosition = new(150f, -150f);

        // Add the item preview to just below the items
        GameObject itemPreviewUIObject = Instantiate(previewUIPrefab, Rect);
        previewUI = itemPreviewUIObject.GetComponent<GridInventoryPreviewUI>();
        itemPreviewUIObject.name = "Inventory Item Indicator UI";
        itemPreviewUIObject.transform.SetSiblingIndex(itemContainer.GetSiblingIndex() - 1);
        itemPreviewUIObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Inventory != null)
        {
            Inventory.OnItemAdded -= OnItemAdded;
            Inventory.OnItemRemoved -= OnItemRemoved;
        }
    }

    private void OnItemAdded(ItemInstance item)
    {
        // Create new inventory item UI and
        GameObject itemUIGO = Instantiate(itemUIPrefab, itemContainer);
        ItemUI itemUI = itemUIGO.GetComponent<ItemUI>();
        itemUI.SetItem(item);
        Vector2 localPos = ConvertGridPosToLocalPos(item.InventoryPosition.x, item.InventoryPosition.y);
        itemUI.SetLocalPosition(this, itemContainer, localPos.x, localPos.y);
        itemUIs.Add(item, itemUI);
    }

    private void OnItemRemoved(ItemInstance item)
    {
        // Remove inventory item UI
        ItemUI itemUI = itemUIs[item];
        Destroy(itemUI.gameObject);
        itemUIs.Remove(item);
    }
}
