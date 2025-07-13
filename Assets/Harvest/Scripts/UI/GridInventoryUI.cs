using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GridInventoryUI : MonoBehaviour, IItemContainerUI
{
    // Values calculated from the image
    public static float BG_IMAGE_GRID_SIZE = 80;
    public static float BG_IMAGE_BORDER_SIZE = 1;
    public static float BG_IMAGE_PPU = 100;
    public static float GRID_SIZE => BG_IMAGE_GRID_SIZE / (BG_IMAGE_PPU / 100.0f);
    public static float GRID_BORDER_SIZE => BG_IMAGE_BORDER_SIZE / (BG_IMAGE_PPU / 100.0f);
    public static Vector2 GRID_CELL_SIZE => new Vector2(GRID_SIZE, GRID_SIZE);

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

        // Rescale main panel to fit the inventory size and move to the correct position
        Rect.sizeDelta = GetGridSize(Inventory.SizeX, Inventory.SizeY);
        Rect.anchoredPosition = new(Screen.width - Rect.sizeDelta.x - 40f, -40f);

        // Create item UIs for existing items in the inventory
        foreach (ItemInstance itemInstance in Inventory.ItemInstances) OnItemAdded(itemInstance);
    }

    public Vector2Int ConvertWorldToGridPos(Vector2 worldPos)
    {
        Vector2 localPos = worldPos - (Vector2)Rect.transform.position;
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

    public void Click(ItemUI itemUI, Vector2 pos)
    {
        // Find the hovered item UI and its local position
        ItemUI hoveredItemUI = GetHoveredItem(pos);
        Vector2 hoveredItemUILocalPos = Vector2.zero;
        if (hoveredItemUI != null) hoveredItemUI.GetPosInside(pos, out hoveredItemUILocalPos);

        ItemContainerInteractResponse response;
        if (itemUI.State != ItemUI.StateType.Empty)
        {
            // Try place item into the inventory
            Vector2 itemOffsetPos = (Vector2)itemUI.Rect.position + GRID_CELL_SIZE / 2;
            Vector2Int itemGridPos = ConvertWorldToGridPos(itemOffsetPos);
            response = Inventory.PlaceItem(itemUI.ItemInstance, itemGridPos.x, itemGridPos.y, false);
        }
        else
        {
            // Otherwise just pickup whatever item is hovered
            if (hoveredItemUI == null || hoveredItemUI.ItemInstance == null) return;
            response = Inventory.PickupItem(hoveredItemUI.ItemInstance);
        }

        // Update the held item UI with the response
        Vector2 offset = new(GRID_CELL_SIZE.x / 2f, -GRID_CELL_SIZE.y / 2f);
        if (hoveredItemUI != null) offset = hoveredItemUILocalPos;
        itemUI.SetItemWithResponse(response, offset);
    }

    public void PreviewClick(ItemUI itemUI, Vector2 pos)
    {
        // Find the hovered item UI
        ItemUI hoveredItemUI = GetHoveredItem(pos);

        // Calculate the preview + item UI for the preview UI
        ItemUI relevantItemUI = null;
        InteractResponse preview = null;
        if (itemUI.State != ItemUI.StateType.Empty)
        {
            // Preview placing it into the inventory
            Vector2 itemOffsetPos = (Vector2)itemUI.Rect.position + GRID_CELL_SIZE / 2;
            Vector2Int itemGridPos = ConvertWorldToGridPos(itemOffsetPos);
            ItemContainerInteractResponse response = Inventory.PlaceItem(itemUI.ItemInstance, itemGridPos.x, itemGridPos.y, true);
            preview = new InteractResponse(response, itemGridPos);
            relevantItemUI = itemUI;
        }
        else if (hoveredItemUI != null && hoveredItemUI.ContainerUI == (IItemContainerUI)this)
        {
            // Preview picking up whatever item is hovered
            ItemContainerInteractResponse response = new(ItemContainerInteractType.Pickup, hoveredItemUI.ItemInstance);
            Vector2 hoveredItemOffsetPos = (Vector2)hoveredItemUI.Rect.position + GRID_CELL_SIZE / 2;
            Vector2Int hoveredItemGridPos = ConvertWorldToGridPos(hoveredItemOffsetPos);
            preview = new InteractResponse(response, hoveredItemGridPos);
            relevantItemUI = hoveredItemUI;
        }

        // Update the preview UI
        if (preview != null) previewUI.SetPreview(relevantItemUI, this, preview);
        else DisablePreview();
    }

    public void DisablePreview()
    {
        previewUI.HidePreview();
    }

    public class InteractResponse
    {
        public ItemContainerInteractResponse inventoryResponse;
        public Vector2Int? gridPos;

        public InteractResponse(ItemContainerInteractResponse inventoryResponse, Vector2Int? gridPos = null)
        {
            this.inventoryResponse = inventoryResponse;
            this.gridPos = gridPos;
        }
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
        // Setup anchors to be consistent
        Rect.pivot = new(0, 1);
        Rect.anchorMin = new(0, 1);
        Rect.anchorMax = new(0, 1);

        // Add the item preview to just below the items
        previewUI = PlayerUI.InstantiateElement<GridInventoryPreviewUI>(previewUIPrefab, "Inventory Item Indicator UI", Rect);
        previewUI.transform.SetSiblingIndex(itemContainer.GetSiblingIndex());
        previewUI.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Inventory != null)
        {
            Inventory.OnItemAdded -= OnItemAdded;
            Inventory.OnItemRemoved -= OnItemRemoved;
        }
    }

    private ItemUI GetHoveredItem(Vector2 pos)
    {
        List<ItemUI> hoveredItems = itemUIs.Values.Where(i => i.GetPosInside(pos, out _)).ToList();
        Debug.Assert(hoveredItems.Count <= 1);
        return hoveredItems.Count > 0 ? hoveredItems[0] : null;
    }

    private void OnItemAdded(ItemInstance itemInstance)
    {
        // Create new inventory item UI and
        ItemUI itemUI = PlayerUI.InstantiateElement<ItemUI>(itemUIPrefab, $"Inventory Item UI ({itemInstance.Data.Name})", itemContainer);
        itemUI.SetItem(itemInstance);
        Debug.Assert(Inventory.TryGetItemPosition(itemInstance, out Vector2Int itemGridPos), "Item must be in the inventory");
        Vector2 localPos = ConvertGridPosToLocalPos(itemGridPos.x, itemGridPos.y);
        itemUI.SetStateToContainer(this, itemContainer, localPos.x, localPos.y);
        itemUIs.Add(itemInstance, itemUI);
    }

    private void OnItemRemoved(ItemInstance item)
    {
        // Remove inventory item UI
        ItemUI itemUI = itemUIs[item];
        Destroy(itemUI.gameObject);
        itemUIs.Remove(item);
    }
}
