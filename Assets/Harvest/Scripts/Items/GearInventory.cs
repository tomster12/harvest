using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public struct GearInventoryDTO
{
}

public partial class GearInventory : ISerdeable<GearInventoryDTO>, IItemContainer
{
    public event Action<ItemInstance> OnItemAdded = delegate { };

    public event Action<ItemInstance> OnItemRemoved = delegate { };

    public Dictionary<EquipmentType, ItemInstance> EquipmentItems => equipmentItems;
    public ItemInstance ToolItem => toolItem;

    public GearInventoryDTO Serialize()
    {
        return new GearInventoryDTO();
    }

    public void Deserialize(GearInventoryDTO inventoryDTO)
    {
    }

    public ItemContainerInteractResponse PlaceItem(ItemInstance itemInstance, bool preview = false)
    {
        // Place a new equipment
        if (itemInstance.Data.type == ItemType.Equipment)
        {
            // Make sure the item is a valid option
            Debug.Assert(itemInstance.Data.equipmentData != null, "ItemInstance Data must have EquipmentData for Equipment type in GearInventory.");
            Debug.Assert(itemInstance.Data.equipmentData.equipmentType != EquipmentType.None, "EquipmentData must have a valid EquipmentType for Equipment type in GearInventory.");
            EquipmentItemData equipmentData = itemInstance.Data.equipmentData;

            // Replace an existing item
            ItemInstance existingItem = equipmentItems[equipmentData.equipmentType];
            if (existingItem != null)
            {
                if (!preview)
                {
                    equipmentItems[equipmentData.equipmentType] = itemInstance;
                    itemInstance.SetContainer(this);
                    OnItemRemoved?.Invoke(existingItem);
                    OnItemAdded?.Invoke(itemInstance);
                }
                return new ItemContainerInteractResponse(ItemContainerInteractType.Replaced, existingItem);
            }

            // Place as a  new item
            if (!preview)
            {
                equipmentItems[equipmentData.equipmentType] = itemInstance;
                itemInstance.SetContainer(this);
                OnItemAdded?.Invoke(itemInstance);
            }
            return new ItemContainerInteractResponse(ItemContainerInteractType.Placed, itemInstance);
        }

        // Place new tool
        if (itemInstance.Data.type == ItemType.Tool)
        {
            // Make sure the item is a valid option
            Debug.Assert(itemInstance.Data.toolData != null, "ItemInstance Data must have ToolData for Tool type in GearInventory.");
            ToolItemData toolData = itemInstance.Data.toolData;
            Debug.Assert(toolData.toolType != ToolType.None, "ToolData must have a valid ToolType for Tool type in GearInventory.");

            // Replace an existing tool
            if (toolItem != null)
            {
                if (!preview)
                {
                    ItemInstance previousTool = toolItem;
                    toolItem = itemInstance;
                    itemInstance.SetContainer(this);
                    OnItemRemoved?.Invoke(previousTool);
                    OnItemAdded?.Invoke(itemInstance);
                }
                return new ItemContainerInteractResponse(ItemContainerInteractType.Replaced, toolItem);
            }

            // Place as a new tool
            if (!preview)
            {
                toolItem = itemInstance;
                itemInstance.SetContainer(this);
                OnItemAdded?.Invoke(itemInstance);
            }
            return new ItemContainerInteractResponse(ItemContainerInteractType.Placed, itemInstance);
        }

        // If the item is not an equipment or tool, it cannot be placed in the gear inventory
        return new ItemContainerInteractResponse(ItemContainerInteractType.Invalid, itemInstance);
    }

    public ItemContainerInteractResponse PickupItem(ItemInstance itemInstance)
    {
        // Remove an equipment item
        if (itemInstance.Data.type == ItemType.Equipment)
        {
            // Make sure the item is a valid option
            Debug.Assert(itemInstance.Data.equipmentData != null, "ItemInstance Data must have EquipmentData for Equipment type in GearInventory.");
            EquipmentItemData equipmentData = itemInstance.Data.equipmentData;
            Debug.Assert(equipmentData.equipmentType != EquipmentType.None, "EquipmentData must have a valid EquipmentType for Equipment type in GearInventory.");
            Debug.Assert(equipmentItems.ContainsKey(equipmentData.equipmentType), "EquipmentItems dictionary must contain the EquipmentType key for Equipment type.");
            Debug.Assert(equipmentItems[equipmentData.equipmentType] == itemInstance, "EquipmentItems dictionary must contain the itemInstance for the EquipmentType key in GearInventory.");
            Debug.Assert(itemInstance.Container == this, equipmentData.equipmentType + " itemInstance container must match GearInventory for removal.");

            // Remove the item from the inventory
            equipmentItems.Remove(equipmentData.equipmentType);
            itemInstance.SetContainer(null);
            OnItemRemoved?.Invoke(itemInstance);
            return new ItemContainerInteractResponse(ItemContainerInteractType.Removed, itemInstance);
        }

        // Remove a tool item
        if (itemInstance.Data.type == ItemType.Tool)
        {
            // Make sure the item is a valid option
            Debug.Assert(itemInstance.Data.toolData != null, "ItemInstance Data must have ToolData for Tool type in GearInventory.");
            ToolItemData toolData = itemInstance.Data.toolData;
            Debug.Assert(toolData.toolType != ToolType.None, "ToolData must have a valid ToolType for Tool type in GearInventory.");
            Debug.Assert(toolItem == itemInstance, "ToolItem must match the itemInstance for removal.");
            Debug.Assert(itemInstance.Container == this, "Tool itemInstance container must match GearInventory for removal.");

            // Remove the item from the inventory
            toolItem = null;
            itemInstance.SetContainer(null);
            OnItemRemoved?.Invoke(itemInstance);
            return new ItemContainerInteractResponse(ItemContainerInteractType.Removed, itemInstance);
        }

        // If the item is not found, return blocked
        return new ItemContainerInteractResponse(ItemContainerInteractType.Blocked, itemInstance);
    }

    private Dictionary<EquipmentType, ItemInstance> equipmentItems = new();
    private ItemInstance toolItem = null;
}
