using System.Collections.Generic;
using UnityEngine;

public class GridInventoryUI : MonoBehaviour, IItemContainerUI
{
    public struct InteractResponse
    {
        public ItemContainerInteractResponse inventoryResponse;
        public Vector2Int? gridPos;

        public InteractResponse(ItemContainerInteractResponse inventoryResponse, Vector2Int? gridPos = null)
        {
            this.inventoryResponse = inventoryResponse;
            this.gridPos = gridPos;
        }
    }

    // Values calculated from the image
    public static float BG_IMAGE_GRID_SIZE = 80;
    public static float BG_IMAGE_BORDER_SIZE = 1;
    public static float BG_IMAGE_PPU = 100;
    public static float GRID_SIZE => BG_IMAGE_GRID_SIZE / (BG_IMAGE_PPU / 100.0f);
    public static float GRID_BORDER_SIZE => BG_IMAGE_BORDER_SIZE / (BG_IMAGE_PPU / 100.0f);
    public static Vector2 GRID_CELL_SIZE => new Vector2(GRID_SIZE, GRID_SIZE);

    public RectTransform Rect => (RectTransform)transform;
    public GridInventory Inventory { get; private set; }

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

    public static Vector2 GetGridSize(int gridSizeX, int gridSizeY)
    {
        float width = gridSizeX * (GRID_SIZE + GRID_BORDER_SIZE) - GRID_BORDER_SIZE;
        float height = gridSizeY * (GRID_SIZE + GRID_BORDER_SIZE) - GRID_BORDER_SIZE;
        return new Vector2(width, height);
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

    public void ClickItem(ItemUI heldItemUI, ItemUI hoveredItemUI)
    {
        Debug.Assert(heldItemUI != null || hoveredItemUI != null, "Must have either a held item or hovered item to click");

        // Find where the item is in the inventory
        Vector2 offsetTopLeft = (Vector2)heldItemUI.Rect.position + GRID_CELL_SIZE / 2;
        Vector2Int itemGridPos = ConvertWorldToGridPos(offsetTopLeft);

        // Cache hovered offset before it is removed
        Vector2 hoveredItemUIPos = Vector2.zero;
        if (hoveredItemUI) hoveredItemUIPos = hoveredItemUI.Rect.position;

        // Either place or pickup an item from the inventory
        ItemContainerInteractResponse response;
        if (heldItemUI.State != ItemUI.StateType.Empty) response = Inventory.PlaceItem(heldItemUI.ItemInstance, itemGridPos.x, itemGridPos.y, false);
        else response = Inventory.PickupItem(hoveredItemUI.ItemInstance);

        // Update the held item UI with the response
        Vector2 offset = new Vector2(GridInventoryUI.GRID_CELL_SIZE.x, -GridInventoryUI.GRID_CELL_SIZE.y);
        if (hoveredItemUI != null && hoveredItemUI.ItemInstance == response.itemInstance) offset = (Vector2)Input.mousePosition - hoveredItemUIPos;
        heldItemUI.UpdateWithContainerResponse(response, offset);
    }

    public void PreviewClickItem(ItemUI heldItemUI, ItemUI hoveredItemUI)
    {
        if (heldItemUI.State != ItemUI.StateType.Empty)
        {
            // Holding an item, so preview whether can place it
            Vector2 offsetTopLeft = (Vector2)heldItemUI.Rect.position + GRID_CELL_SIZE / 2;
            Vector2Int itemGridPos = ConvertWorldToGridPos(offsetTopLeft);
            ItemContainerInteractResponse response = Inventory.PlaceItem(heldItemUI.ItemInstance, itemGridPos.x, itemGridPos.y, true);
            previewUI.SetPreview(heldItemUI, this, new InteractResponse(response, itemGridPos));
        }
        else if (hoveredItemUI != null && hoveredItemUI.ContainerUI == (IItemContainerUI)this)
        {
            // Not holding anything, so preview hovering and removing an item
            Debug.Assert(Inventory.TryGetItemPosition(hoveredItemUI.ItemInstance, out Vector2Int hoveredItemGridPos), "Hovered item must be in the inventory");
            ItemContainerInteractResponse response = new(ItemContainerInteractType.Removed, hoveredItemUI.ItemInstance);
            previewUI.SetPreview(hoveredItemUI, this, new InteractResponse(response, hoveredItemGridPos));
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

    private readonly Dictionary<ItemInstance, ItemUI> itemUIs = new();

    [Header("References")]
    [SerializeField] private RectTransform itemContainer;

    [Header("Prefabs")]
    [SerializeField] private GameObject itemUIPrefab;
    [SerializeField] private GameObject previewUIPrefab;

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

    private void OnItemAdded(ItemInstance itemInstance)
    {
        // Create new inventory item UI and
        ItemUI itemUI = PlayerUI.InstantiateElement<ItemUI>(itemUIPrefab, $"Inventory Item UI ({itemInstance.Data.Name})", itemContainer);
        itemUI.SetItem(itemInstance);
        Debug.Assert(Inventory.TryGetItemPosition(itemInstance, out Vector2Int itemGridPos), "Item must be in the inventory");
        Vector2 localPos = ConvertGridPosToLocalPos(itemGridPos.x, itemGridPos.y);
        itemUI.SetLocalPosition(this, itemContainer, localPos.x, localPos.y);
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
