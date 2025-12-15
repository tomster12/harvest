using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public struct GearInventoryDTO
{
}

[Serializable]
public partial class GearInventory : ISerdeable<GearInventoryDTO>, IItemContainer
{
    public event Action<ItemInstance> OnItemAdded = delegate { };

    public event Action<ItemInstance> OnItemRemoved = delegate { };

    public Dictionary<EquipmentType, ItemInstance> EquipmentItems => equipmentItems;
    public ItemInstance ToolItem => toolItem;

    public ItemContainerInteractResponse PlaceItem(ItemInstance itemInstance, bool preview = false)
    {
        // Place a new equipment
        if (itemInstance.Data.Type == ItemType.Equipment)
        {
            // Make sure the item is a valid option
            Debug.Assert(itemInstance.Data.EquipmentData != null, "ItemInstance Data must have EquipmentData for Equipment type in GearInventory.");
            Debug.Assert(itemInstance.Data.EquipmentData.Type != EquipmentType.None, "EquipmentData must have a valid EquipmentType for Equipment type in GearInventory.");
            EquipmentItemData equipmentData = itemInstance.Data.EquipmentData;

            // Replace an existing item
            ItemInstance existingItem = equipmentItems[equipmentData.Type];
            if (existingItem != null && existingItem.Data != null)
            {
                if (!preview)
                {
                    equipmentItems[equipmentData.Type] = itemInstance;
                    itemInstance.SetContainer(this);
                    OnItemRemoved?.Invoke(existingItem);
                    OnItemAdded?.Invoke(itemInstance);
                }
                return new ItemContainerInteractResponse(ItemContainerInteractType.Replaced, existingItem);
            }

            // Place as a  new item
            if (!preview)
            {
                equipmentItems[equipmentData.Type] = itemInstance;
                itemInstance.SetContainer(this);
                OnItemAdded?.Invoke(itemInstance);
            }
            return new ItemContainerInteractResponse(ItemContainerInteractType.Placed, itemInstance);
        }

        // Place new tool
        if (itemInstance.Data.Type == ItemType.Tool)
        {
            // Make sure the item is a valid option
            Debug.Assert(itemInstance.Data.ToolData != null, "ItemInstance Data must have ToolData for Tool type in GearInventory.");
            ToolItemData toolData = itemInstance.Data.ToolData;
            Debug.Assert(toolData.Type != ToolType.None, "ToolData must have a valid ToolType for Tool type in GearInventory.");

            // Replace an existing tool
            if (toolItem != null && toolItem.Data != null)
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
        if (itemInstance.Data.Type == ItemType.Equipment)
        {
            // Make sure the item is a valid option
            Debug.Assert(itemInstance.Data.EquipmentData != null, "ItemInstance Data must have EquipmentData for Equipment type in GearInventory.");
            EquipmentItemData equipmentData = itemInstance.Data.EquipmentData;
            Debug.Assert(equipmentData.Type != EquipmentType.None, "EquipmentData must have a valid EquipmentType for Equipment type in GearInventory.");
            Debug.Assert(equipmentItems.ContainsKey(equipmentData.Type), "EquipmentItems dictionary must contain the EquipmentType key for Equipment type.");
            Debug.Assert(equipmentItems[equipmentData.Type] == itemInstance, "EquipmentItems dictionary must contain the itemInstance for the EquipmentType key in GearInventory.");
            Debug.Assert(itemInstance.Container == this, equipmentData.Type + " itemInstance container must match GearInventory for removal.");

            // Remove the item from the inventory
            equipmentItems.Remove(equipmentData.Type);
            itemInstance.SetContainer(null);
            OnItemRemoved?.Invoke(itemInstance);
            return new ItemContainerInteractResponse(ItemContainerInteractType.Pickup, itemInstance);
        }

        // Remove a tool item
        if (itemInstance.Data.Type == ItemType.Tool)
        {
            // Make sure the item is a valid option
            Debug.Assert(itemInstance.Data.ToolData != null, "ItemInstance Data must have ToolData for Tool type in GearInventory.");
            ToolItemData toolData = itemInstance.Data.ToolData;
            Debug.Assert(toolData.Type != ToolType.None, "ToolData must have a valid ToolType for Tool type in GearInventory.");
            Debug.Assert(toolItem == itemInstance, "ToolItem must match the itemInstance for removal.");
            Debug.Assert(itemInstance.Container == this, "Tool itemInstance container must match GearInventory for removal.");

            // Remove the item from the inventory
            toolItem = null;
            itemInstance.SetContainer(null);
            OnItemRemoved?.Invoke(itemInstance);
            return new ItemContainerInteractResponse(ItemContainerInteractType.Pickup, itemInstance);
        }

        // If the item is not found, return blocked
        return new ItemContainerInteractResponse(ItemContainerInteractType.Blocked, itemInstance);
    }

    public GearInventoryDTO Serialize()
    {
        return new GearInventoryDTO();
    }

    public void Deserialize(GearInventoryDTO inventoryDTO)
    {
    }

    private Dictionary<EquipmentType, ItemInstance> equipmentItems = new();
    private ItemInstance toolItem = null;
}
