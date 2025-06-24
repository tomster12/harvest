using System.Collections.Generic;
using UnityEngine;
using static Inventory;

public class InventoryUI : MonoBehaviour
{
    // Values calculated from the image
    public static float BG_IMAGE_GRID_SIZE = 80;
    public static float BG_IMAGE_BORDER_SIZE = 1;
    public static float BG_IMAGE_PPU = 100;
    public static float GRID_SIZE => BG_IMAGE_GRID_SIZE / (BG_IMAGE_PPU / 100.0f);
    public static float GRID_BORDER_SIZE => BG_IMAGE_BORDER_SIZE / (BG_IMAGE_PPU / 100.0f);

    public RectTransform Rect => (RectTransform)transform;

    public Inventory Inventory { get; private set; }

    public static Vector2 GetGridSize(int gridSizeX, int gridSizeY)
    {
        float width = gridSizeX * (GRID_SIZE + GRID_BORDER_SIZE) - GRID_BORDER_SIZE;
        float height = gridSizeY * (GRID_SIZE + GRID_BORDER_SIZE) - GRID_BORDER_SIZE;
        return new Vector2(width, height);
    }

    public void SetInventory(Inventory newInventory)
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

    public (Vector2Int, (ItemPlaceResponse, ItemInstance)) PlaceOrStackHeldItem(InventoryItemUI heldItemUI, bool preview = false)
    {
        // Try and place the item into the inventory
        Vector2 offsetTopLeft = (Vector2)heldItemUI.Rect.position + GetGridSize(1, 1) / 2;
        Vector2Int hoveringSlot = ConvertWorldToGridPos(offsetTopLeft);
        var response = Inventory.PlaceOrStackItem(heldItemUI.ItemInstance, hoveringSlot.x, hoveringSlot.y, preview);

        if (preview) return (hoveringSlot, response);

        // Placed or stacked so turn off held item
        if (response.Item1 == ItemPlaceResponse.Placed || (response.Item1 == ItemPlaceResponse.Stacked && heldItemUI.ItemInstance.Amount == 0))
        {
            heldItemUI.SetItem(null);
        }

        // Placed and swapped with a new item
        else if (response.Item1 == ItemPlaceResponse.Replaced)
        {
            InventoryItemUI newHeldItemUI = itemUIs[response.Item2];
            heldItemUI.SetItem(newHeldItemUI.ItemInstance);
            heldItemUI.SetHeldByMouse(heldItemUI.MouseOffset);
        }

        return (hoveringSlot, response);
    }

    public void PickupItem(InventoryItemUI heldItemUI, InventoryItemUI hoveredItemUI)
    {
        Debug.Assert(hoveredItemUI.InventoryUI == this, "Cannot pick up item from another inventory UI");

        Vector2 hoveredItemPos = hoveredItemUI.Rect.position;
        if (Inventory.RemoveItem(hoveredItemUI.ItemInstance))
        {
            heldItemUI.SetItem(hoveredItemUI.ItemInstance);
            Vector2 offset = (Vector2)Input.mousePosition - hoveredItemPos;
            heldItemUI.SetHeldByMouse(offset);
        }
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
    [SerializeField] private GameObject inventoryItemUIPrefab;

    private Dictionary<ItemInstance, InventoryItemUI> itemUIs = new();

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
        GameObject itemUIGO = Instantiate(inventoryItemUIPrefab, itemContainer);
        InventoryItemUI itemUI = itemUIGO.GetComponent<InventoryItemUI>();
        itemUI.SetItem(item);
        itemUI.SetInInventory(this, item.InventoryPosition.x, item.InventoryPosition.y);
        itemUIs.Add(item, itemUI);
    }

    private void OnItemRemoved(ItemInstance item)
    {
        // Remove inventory item UI
        InventoryItemUI itemUI = itemUIs[item];
        Destroy(itemUI.gameObject);
        itemUIs.Remove(item);
    }
}
