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
        foreach (ItemInstance itemInstance in Inventory.itemInstances) OnItemAdded(itemInstance);
    }

    public InteractResponse PlaceOrStackHeldItem(ItemUI heldItemUI, ItemUI hoveredItemUI, bool preview = false)
    {
        // Cache this first, as it may be removed during a replace interaction
        Vector2 hoveredItemUIPos = Vector2.zero;
        if (hoveredItemUI) hoveredItemUIPos = hoveredItemUI.Rect.position;

        // Try and place the item into the inventory
        // Calculate its grid pos with an offset from its position on the UI
        Vector2 offsetTopLeft = (Vector2)heldItemUI.Rect.position + GRID_CELL_SIZE / 2;
        Vector2Int hoveringGridPos = ConvertWorldToGridPos(offsetTopLeft);
        ItemContainerInteractResponse response = Inventory.PlaceOrStackItem(heldItemUI.ItemInstance, hoveringGridPos.x, hoveringGridPos.y, preview);

        if (!preview)
        {
            // Placed or stacked, so turn off held item
            if (response.type == ItemContainerInteractType.Placed || (response.type == ItemContainerInteractType.Stacked && heldItemUI.ItemInstance.Amount == 0))
            {
                heldItemUI.SetItem(null);
            }

            // Placed and swapped with a new item
            else if (response.type == ItemContainerInteractType.Replaced)
            {
                heldItemUI.SetItem(response.itemInstance);
                Vector2 offset;
                if (hoveredItemUI != null && hoveredItemUI.ItemInstance == response.itemInstance)
                {
                    offset = (Vector2)Input.mousePosition - hoveredItemUIPos;
                }
                else
                {
                    offset = GRID_CELL_SIZE / 2;
                    offset = new Vector2(offset.x, -offset.y);
                }
                heldItemUI.SetHeldByMouse(offset);
            }
        }

        return new InteractResponse(response, hoveringGridPos);
    }

    public InteractResponse PickupItem(ItemUI heldItemUI, ItemUI hoveredItemUI)
    {
        Vector2 hoveredItemPos = hoveredItemUI.Rect.position;
        Debug.Assert(Inventory.TryGetItemPosition(hoveredItemUI.ItemInstance, out Vector2Int hoveredItemGridPos), "Hovered item must be in the inventory");

        ItemContainerInteractResponse response = Inventory.RemoveItem(hoveredItemUI.ItemInstance);

        if (response.type == ItemContainerInteractType.Removed)
        {
            Vector2 offset = (Vector2)Input.mousePosition - hoveredItemPos;
            heldItemUI.SetItem(hoveredItemUI.ItemInstance);
            heldItemUI.SetHeldByMouse(offset);
            return new InteractResponse(response, hoveredItemGridPos);
        }

        return new InteractResponse(response, hoveredItemGridPos);
    }

    public void HoverPreview(ItemUI heldItemUI, ItemUI hoveredItemUI)
    {
        if (heldItemUI.State != ItemUI.StateType.EMPTY)
        {
            // Holding an item, so preview whether can place it
            var preview = PlaceOrStackHeldItem(heldItemUI, hoveredItemUI, true);
            previewUI.SetPreview(heldItemUI, this, preview);
        }
        else if (hoveredItemUI != null && hoveredItemUI.ContainerUI == (IItemContainerUI)this)
        {
            // Not holding anything, so preview hovering and removing an item
            Debug.Assert(Inventory.TryGetItemPosition(hoveredItemUI.ItemInstance, out Vector2Int hoveredItemGridPos), "Hovered item must be in the inventory");
            InteractResponse response = new(new ItemContainerInteractResponse(ItemContainerInteractType.Removed, hoveredItemUI.ItemInstance), hoveredItemGridPos);
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

    private readonly Dictionary<ItemInstance, ItemUI> itemUIs = new();
    private GridInventoryPreviewUI previewUI;

    private void Awake()
    {
        // Move the rect arbitrarily
        Rect.anchoredPosition = new(150f, -150f);

        // Add the item preview to just below the items
        previewUI = PlayerUI.InstantiateElement<GridInventoryPreviewUI>(previewUIPrefab, "Inventory Item Indicator UI", Rect);
        previewUI.transform.SetSiblingIndex(itemContainer.GetSiblingIndex() - 1);
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
