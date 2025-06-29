using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
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

        // Set new inventory and subscribe to events
        Inventory = newInventory;
        Inventory.OnItemAdded += OnItemAdded;
        Inventory.OnItemRemoved += OnItemRemoved;

        // Update slots with existing items
        foreach (var kvp in Inventory.EquipmentItems)
        {
            EquipmentType equipmentType = kvp.Key;
            ItemInstance itemInstance = kvp.Value;
            GearSlot slot = GetSlotFromItem(itemInstance);
            Debug.Assert(slot != null, $"GearInventoryUI must have a valid slot for {equipmentType}.");
            slot.ItemUI.SetItem(itemInstance);
        }
        ItemInstance toolItemInstance = Inventory.ToolItem;
        toolSlot.ItemUI.SetItem(toolItemInstance);
    }

    public void Click(ItemUI itemUI, Vector2 pos)
    {
        // Find the hovered gear slot
        GearSlot hoveredSlot = GetHoveredSlot(pos);
        if (hoveredSlot == null) return;
        hoveredSlot.GetPointInside(pos, out Vector2 hoveredItemUILocalPos);
        ItemInstance hoveredItemUI = GetItemInSlot(hoveredSlot);

        ItemContainerInteractResponse response;
        if (itemUI.State != ItemUI.StateType.Empty)
        {
            // Holding an item, attempt to equip it
            if (!hoveredSlot.IsItemValid(itemUI.ItemInstance)) return;
            response = Inventory.PlaceItem(itemUI.ItemInstance);
        }
        else
        {
            // Not holding anything, so try to remove the item from the slot
            if (hoveredItemUI == null || hoveredItemUI == null) return;
            response = Inventory.PickupItem(hoveredItemUI);
        }

        // Update the item UI based on the response
        Vector2 offset = new(GRID_CELL_SIZE.x, -GRID_CELL_SIZE.y);
        if (hoveredItemUI != null) offset = hoveredItemUILocalPos;
        itemUI.SetItemWithResponse(response, offset);
    }

    public void PreviewClick(ItemUI itemUI, Vector2 pos)
    {
        // Reset all slot backgrounds to default colour
        foreach (GearSlot slot in gearSlots) slot.Background.color = colourDefault;

        // Find the hovered gear slot
        GearSlot hoveredSlot = GetHoveredSlot(pos);
        if (hoveredSlot == null) return;
        ItemInstance hoveredItemUI = GetItemInSlot(hoveredSlot);

        ItemContainerInteractResponse response;
        if (itemUI.State != ItemUI.StateType.Empty)
        {
            // Preview placing it into the inventory
            if (!hoveredSlot.IsItemValid(itemUI.ItemInstance)) response = new ItemContainerInteractResponse(ItemContainerInteractType.Blocked, null);
            else response = Inventory.PlaceItem(itemUI.ItemInstance, true);
        }
        else
        {
            // Otherwise preview picking up whatever item is hovered
            if (hoveredItemUI == null) return;
            response = new ItemContainerInteractResponse(ItemContainerInteractType.Pickup, null);
        }

        // Update the background of the hovered slot based on the response
        switch (response.type)
        {
            case ItemContainerInteractType.Placed:
            case ItemContainerInteractType.Pickup:
                hoveredSlot.Background.color = colourValid;
                break;

            case ItemContainerInteractType.Stacked:
                hoveredSlot.Background.color = colourStacked;
                break;

            case ItemContainerInteractType.Replaced:
                hoveredSlot.Background.color = colourReplaced;
                break;

            case ItemContainerInteractType.Blocked:
            case ItemContainerInteractType.OutOfBounds:
            case ItemContainerInteractType.Invalid:
                hoveredSlot.Background.color = colourBlocked;
                break;

            default:
                hoveredSlot.Background.color = colourDefault;
                break;
        }
    }

    public void DisablePreview()
    {
        // Reset all slot backgrounds to default colour
        foreach (GearSlot slot in gearSlots) slot.Background.color = colourDefault;
    }

    [Header("Prefabs")]
    [SerializeField] private GameObject itemUIPrefab;

    [Header("References")]
    [SerializeField] private RectTransform equipmentHeadContainer;
    [SerializeField] private RectTransform equipmentChestContainer;
    [SerializeField] private RectTransform equipmentLegsContainer;
    [SerializeField] private RectTransform equipmentFeetContainer;
    [SerializeField] private RectTransform equipmentHandsContainer;
    [SerializeField] private RectTransform toolContainer;

    [Header("Config")]
    [SerializeField] private Color colourDefault = Color.green;
    [SerializeField] private Color colourValid = Color.green;
    [SerializeField] private Color colourStacked = Color.yellow;
    [SerializeField] private Color colourReplaced = Color.red;
    [SerializeField] private Color colourBlocked = Color.gray;

    private List<GearSlot> gearSlots = new();
    private Dictionary<EquipmentType, GearSlot> equipmentSlots = new();
    private GearSlot toolSlot;

    private void Awake()
    {
        // Move the rect to an arbitrary offset from top left
        Rect.pivot = new(0, 1);
        Rect.anchorMin = new(0, 1);
        Rect.anchorMax = new(0, 1);
        Rect.anchoredPosition = new(40f, -40f);

        RegisterSlot(EquipmentType.Head, equipmentHeadContainer);
        RegisterSlot(EquipmentType.Body, equipmentChestContainer);
        RegisterSlot(EquipmentType.Legs, equipmentLegsContainer);
        RegisterSlot(EquipmentType.Feet, equipmentFeetContainer);
        RegisterSlot(EquipmentType.Hand, equipmentHandsContainer);
        RegisterSlot(EquipmentType.None, toolContainer, isTool: true);
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

    private GearSlot RegisterSlot(EquipmentType type, RectTransform container, bool isTool = false)
    {
        GearSlot slot = new()
        {
            EquipmentType = type,
            IsToolSlot = isTool,
            Container = container,
            Background = container.GetChild(0).GetComponent<Image>(),
            ItemUI = container.GetComponentInChildren<ItemUI>()
        };

        gearSlots.Add(slot);
        if (isTool) toolSlot = slot;
        else
        {
            Debug.Assert(!equipmentSlots.ContainsKey(type), $"Equipment slot for {type} already exists.");
            equipmentSlots[type] = slot;
        }

        return slot;
    }

    private GearSlot GetSlotFromItem(ItemInstance item)
    {
        if (item == null) return null;
        if (item.Data.type == ItemType.Equipment)
        {
            Debug.Assert(item.Data.equipmentData != null, "ItemInstance Data must have EquipmentData for Equipment type in GearInventoryUI.");
            EquipmentItemData equipmentData = item.Data.equipmentData;
            Debug.Assert(equipmentSlots.ContainsKey(equipmentData.equipmentType), "EquipmentSlots dictionary must contain the EquipmentType key for Equipment type in GearInventoryUI.");
            return equipmentSlots[equipmentData.equipmentType];
        }
        else if (item.Data.type == ItemType.Tool)
        {
            Debug.Assert(toolSlot != null, "Tool slot must be registered in GearInventoryUI.");
            return toolSlot;
        }
        return null;
    }

    private GearSlot GetHoveredSlot(Vector2 pos)
    {
        return gearSlots.FirstOrDefault(slot => slot.GetPointInside(pos, out _));
    }

    private ItemInstance GetItemInSlot(GearSlot slot)
    {
        if (slot == null) return null;
        return slot.ItemUI.ItemInstance;
    }

    private void OnItemAdded(ItemInstance itemInstance)
    {
        // Find the relevant slot
        GearSlot slot = GetSlotFromItem(itemInstance);
        Debug.Assert(slot != null, "ItemInstance must have a valid slot in GearInventoryUI.");
        Debug.Assert(GetItemInSlot(slot) == null, "Slot should not already have an item when adding a new one.");

        // Update the slot with the new item
        slot.ItemUI.SetItem(itemInstance);
    }

    private void OnItemRemoved(ItemInstance item)
    {
        // Find the relevant slot
        GearSlot slot = GetSlotFromItem(item);
        Debug.Assert(slot != null, "ItemInstance must have a valid slot in GearInventoryUI.");
        Debug.Assert(GetItemInSlot(slot) == item, "Slot should have the item being removed.");

        // Clear the item UI in the slot
        slot.ItemUI.SetItem(null);
    }

    private class GearSlot
    {
        public EquipmentType EquipmentType;
        public bool IsToolSlot;
        public RectTransform Container;
        public Image Background;
        public ItemUI ItemUI;

        public bool GetPointInside(Vector2 pos, out Vector2 localPos)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Container, pos, null, out localPos);
            return Container.rect.Contains(localPos);
        }

        public bool IsItemValid(ItemInstance item)
        {
            var data = item.Data;
            return IsToolSlot
                ? data.type == ItemType.Tool && data.toolData?.toolType != ToolType.None
                : data.type == ItemType.Equipment && data.equipmentData?.equipmentType == EquipmentType;
        }
    }
}
