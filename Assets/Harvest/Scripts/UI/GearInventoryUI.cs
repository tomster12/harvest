using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static GridInventoryUI;

public class GearInventoryUI : MonoBehaviour, IItemContainerUI
{
    public RectTransform Rect => (RectTransform)transform;
    public GearInventory Inventory { get; private set; }

    public void SetInventory(GearInventory newInventory)
    {
        // Clear up old inventory
        if (Inventory != null)
        {
            Inventory.OnItemAdded -= OnItemAdded;
            Inventory.OnItemRemoved -= OnItemRemoved;
        }

        Inventory = newInventory;
        Inventory.OnItemAdded += OnItemAdded;
        Inventory.OnItemRemoved += OnItemRemoved;

        foreach (EquipmentType equipmentType in Inventory.EquipmentItems.Keys)
        {
            ItemInstance itemInstance = Inventory.EquipmentItems[equipmentType];
            if (itemInstance != null) OnItemAdded(itemInstance);
        }
    }

    public void ClickItem(ItemUI heldItemUI, ItemUI hoveredItemUI)
    {
        Debug.Assert(heldItemUI != null || hoveredItemUI != null, "Must have either a held item or hovered item to click");

        // Find which slot is being hovered
        (bool isHoveringEquipmentSlot, bool isHoveringToolSlot, EquipmentType hoveredEquipmentType) = GetHoveredSlot();
        if (!isHoveringEquipmentSlot && !isHoveringToolSlot) return;


        // Cache hovered offset before it is removed
        Vector2 hoveredItemUIPos = Vector2.zero;
        if (hoveredItemUI) hoveredItemUIPos = hoveredItemUI.Rect.position;

        // Either place or pickup an item from the inventory
        ItemContainerInteractResponse response;
        if (heldItemUI.State != ItemUI.StateType.Empty) response = Inventory.PlaceItem(heldItemUI.ItemInstance, false);
        else response = Inventory.PickupItem(hoveredItemUI.ItemInstance);

        // Update the held item UI with the response
        Vector2 offset = new Vector2(GRID_CELL_SIZE.x, -GRID_CELL_SIZE.y);
        if (hoveredItemUI != null && hoveredItemUI.ItemInstance == response.itemInstance) offset = (Vector2)Input.mousePosition - hoveredItemUIPos;
        heldItemUI.UpdateWithContainerResponse(response, offset);
    }

    public void PreviewClickItem(ItemUI heldItemUI, ItemUI hoveredItemUI)
    {
        if (heldItemUI.State != ItemUI.StateType.Empty)
        {
            // Holding an item, so preview whether can place it
            ItemContainerInteractResponse response = Inventory.PlaceItem(heldItemUI.ItemInstance, true);
            // TODO
        }
        else if (hoveredItemUI != null && hoveredItemUI.ContainerUI == (IItemContainerUI)this)
        {
            // Not holding anything, so preview hovering and removing an item
            ItemContainerInteractResponse response = new(ItemContainerInteractType.Removed, hoveredItemUI.ItemInstance);
            // TODO
        }
        else
        {
            // Otherwise disable the preview
            DisablePreview();
        }
    }

    public void DisablePreview()
    {
    }

    [Header("Prefabs")]
    [SerializeField] private GameObject itemUIPrefab;

    [Header("Referneces")]
    [SerializeField] private RectTransform equipmentHeadContainer;
    [SerializeField] private RectTransform equipmentChestContainer;
    [SerializeField] private RectTransform equipmentLegsContainer;
    [SerializeField] private RectTransform equipmentFeetContainer;
    [SerializeField] private RectTransform equipmentHandsContainer;
    [SerializeField] private RectTransform toolContainer;

    private struct SlotInfo
    {
        public RectTransform Container;
        public Image Background;
        public ItemUI ItemUI;
    }

    private Dictionary<EquipmentType, SlotInfo> equipmentSlots = new();
    private SlotInfo toolSlot;

    private void Awake()
    {
        // Move the rect to an arbitrary offset from top left
        Rect.pivot = new(0, 1);
        Rect.anchorMin = new(0, 1);
        Rect.anchorMax = new(0, 1);
        Rect.anchoredPosition = new(40f, -40f);

        // Initialize the slot information for equipment / tools
        Action<EquipmentType, RectTransform> setupSlot = new((type, container) =>
        {
            equipmentSlots[type] = new SlotInfo
            {
                Container = container,
                Background = container.GetChild(0).GetComponent<Image>(),
                ItemUI = container.GetComponentInChildren<ItemUI>(),
            };
        });

        setupSlot(EquipmentType.Head, equipmentHeadContainer);
        setupSlot(EquipmentType.Body, equipmentChestContainer);
        setupSlot(EquipmentType.Legs, equipmentLegsContainer);
        setupSlot(EquipmentType.Feet, equipmentFeetContainer);
        setupSlot(EquipmentType.Hand, equipmentHandsContainer);

        toolSlot = new SlotInfo
        {
            Container = toolContainer,
            Background = toolContainer.GetChild(0).GetComponent<Image>(),
            ItemUI = toolContainer.GetComponentInChildren<ItemUI>()
        };
    }

    private void OnDestroy()
    {
        // Clear up the inventory when destroyed
        if (Inventory != null)
        {
            Inventory.OnItemAdded -= OnItemAdded;
            Inventory.OnItemRemoved -= OnItemRemoved;
            Inventory = null;
        }
    }

    private (bool, bool, EquipmentType) GetHoveredSlot()
    {
        // Check if hovering over any equipment slots
        foreach (var kvp in equipmentSlots)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(kvp.Value.Container, Input.mousePosition))
            {
                return (true, false, kvp.Key);
            }
        }
        // Check if hovering over the tool slot
        if (RectTransformUtility.RectangleContainsScreenPoint(toolSlot.Container, Input.mousePosition))
        {
            return (false, true, EquipmentType.None);
        }
        return (false, false, EquipmentType.None);
    }

    private bool IsItemValid(ItemInstance itemInstance, bool isHoveringToolSlot, bool isHoveringEquipmentSlot, EquipmentType hoveredEquipmentType)
    {
        if (isHoveringEquipmentSlot)
        {
            return itemInstance.Data.type == ItemType.Equipment &&
                   itemInstance.Data.equipmentData != null &&
                   itemInstance.Data.equipmentData.equipmentType == hoveredEquipmentType;
        }
        else if (isHoveringToolSlot)
        {
            return itemInstance.Data.type == ItemType.Tool &&
                   itemInstance.Data.toolData != null &&
                   itemInstance.Data.toolData.toolType != ToolType.None;
        }
        return false;
    }

    private void OnItemAdded(ItemInstance itemInstance)
    {
        // Update the UI for the added item
        if (itemInstance.Data.type == ItemType.Equipment)
        {
            Debug.Assert(itemInstance.Data.equipmentData != null, "ItemInstance Data must have EquipmentData for Equipment type in GearInventoryUI.");
            EquipmentItemData equipmentData = itemInstance.Data.equipmentData;
            Debug.Assert(equipmentData.equipmentType != EquipmentType.None, "EquipmentData must have a valid EquipmentType for Equipment type in GearInventoryUI.");
            Debug.Assert(equipmentSlots.ContainsKey(equipmentData.equipmentType), "EquipmentSlots dictionary must contain the EquipmentType key for Equipment type in GearInventoryUI.");

            SlotInfo slotInfo = equipmentSlots[equipmentData.equipmentType];
            slotInfo.ItemUI.SetItem(itemInstance);
        }
        else if (itemInstance.Data.type == ItemType.Tool)
        {
            Debug.Assert(itemInstance.Data.toolData != null, "ItemInstance Data must have ToolData for Tool type in GearInventoryUI.");
            ToolItemData toolData = itemInstance.Data.toolData;
            Debug.Assert(toolData.toolType != ToolType.None, "ToolData must have a valid ToolType for Tool type in GearInventoryUI.");

            toolSlot.ItemUI.SetItem(itemInstance);
        }
        else
        {
            Debug.Assert(false, $"GearInventoryUI does not support item type {itemInstance.Data.type}.");
        }
    }

    private void OnItemRemoved(ItemInstance item)
    {
        // Update the UI for the removed item
        if (item.Data.type == ItemType.Equipment)
        {
            Debug.Assert(item.Data.equipmentData != null, "ItemInstance Data must have EquipmentData for Equipment type in GearInventoryUI.");
            EquipmentItemData equipmentData = item.Data.equipmentData;
            Debug.Assert(equipmentData.equipmentType != EquipmentType.None, "EquipmentData must have a valid EquipmentType for Equipment type in GearInventoryUI.");
            Debug.Assert(equipmentSlots.ContainsKey(equipmentData.equipmentType), "EquipmentSlots dictionary must contain the EquipmentType key for Equipment type in GearInventoryUI.");
            Debug.Assert(equipmentSlots[equipmentData.equipmentType].ItemUI.ItemInstance == item, "ItemInstance must match the one in the slot for Equipment type in GearInventoryUI.");

            SlotInfo slotInfo = equipmentSlots[equipmentData.equipmentType];
            slotInfo.ItemUI.SetItem(null);
        }
        else if (item.Data.type == ItemType.Tool)
        {
            Debug.Assert(item.Data.toolData != null, "ItemInstance Data must have ToolData for Tool type in GearInventoryUI.");
            Debug.Assert(toolSlot.ItemUI.ItemInstance == item, "ItemInstance must match the one in the slot for Tool type in GearInventoryUI.");

            toolSlot.ItemUI.SetItem(null);
        }
    }
}
