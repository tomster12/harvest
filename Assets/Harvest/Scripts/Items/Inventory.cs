using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public struct InventoryDTO
{
    public (Vector2Int, ItemInstanceDTO)[] itemInstanceDTOs;
    public int[,] slots;

    public InventoryDTO((Vector2Int, ItemInstanceDTO)[] itemInstanceDTOs, int[,] slots)
    {
        this.itemInstanceDTOs = itemInstanceDTOs;
        this.slots = slots;
    }
}

public partial class Inventory : ISerdeable<InventoryDTO>
{
    public enum ItemPlaceResponse
    { Placed, Stacked, Replaced, Blocked, OutOfBounds };

    public event Action<ItemInstance> OnItemAdded = delegate { };

    public event Action<ItemInstance> OnItemRemoved = delegate { };

    public List<ItemInstance> ItemInstances { get; private set; } = new List<ItemInstance>();
    public int SizeX => sizeX;
    public int SizeY => sizeY;

    public Inventory(int sizeX, int sizeY)
    {
        ItemInstances = new List<ItemInstance>();
        slots = new int[sizeX, sizeY];
        this.sizeX = sizeX;
        this.sizeY = sizeY;

        // Initialize slots to -1
        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                slots[x, y] = -1;
            }
        }
    }

    public InventoryDTO Serialize()
    {
        (Vector2Int, ItemInstanceDTO)[] itemInstanceDTOs = new (Vector2Int, ItemInstanceDTO)[ItemInstances.Count];
        for (int i = 0; i < ItemInstances.Count; i++) itemInstanceDTOs[i] = (ItemInstances[i].InventoryPosition, ItemInstances[i].Serialize());
        return new(itemInstanceDTOs, slots);
    }

    public void Deserialize(InventoryDTO inventoryDTO)
    {
        slots = inventoryDTO.slots;
        sizeX = inventoryDTO.slots.GetLength(0);
        sizeY = inventoryDTO.slots.GetLength(1);
        ItemInstances.Clear();
        for (int i = 0; i < inventoryDTO.itemInstanceDTOs.Length; i++)
        {
            var (itemPos, itemInstanceDTO) = inventoryDTO.itemInstanceDTOs[i];
            ItemInstance itemInstance = ItemInstance.DeserializeNew(itemInstanceDTO);
            itemInstance.SetInventory(this, itemPos.x, itemPos.y);
            ItemInstances.Add(itemInstance);
        }
    }

    public (ItemPlaceResponse, ItemInstance) PlaceOrStackItem(ItemInstance itemInstance, int x, int y, bool preview = false)
    {
        // Check position is in bounds
        if (x < 0 || y < 0 || x + itemInstance.Data.SizeX > sizeX || y + itemInstance.Data.SizeY > sizeY)
        {
            return (ItemPlaceResponse.OutOfBounds, null);
        }

        // Check if item under cursor matches and stack
        if (slots[x, y] != -1)
        {
            ItemInstance existingItemInstance = ItemInstances[slots[x, y]];
            if (StackItemOntoExisting(existingItemInstance, itemInstance, preview)) return (ItemPlaceResponse.Stacked, null);
        }

        // Find number of overlapping items
        HashSet<int> overlappingItems = new();
        for (int i = 0; i < itemInstance.Data.SizeX; i++)
        {
            for (int j = 0; j < itemInstance.Data.SizeY; j++)
            {
                if (x + i >= sizeX || y + j >= sizeY || slots[x + i, y + j] == -1) continue;
                overlappingItems.Add(slots[x + i, y + j]);
            }
        }

        // Overlapping 2+ items, therefore blocked
        if (overlappingItems.Count > 1)
        {
            return (ItemPlaceResponse.Blocked, null);
        }

        // Overlapping 1 item
        else if (overlappingItems.Count == 1)
        {
            var existingItemIndex = overlappingItems.First();
            var existingItem = ItemInstances[existingItemIndex];

            // If item matches try stack
            if (existingItem.Data == itemInstance.Data)
            {
                if (StackItemOntoExisting(existingItem, itemInstance, preview)) return (ItemPlaceResponse.Stacked, null);
            }

            // If have not stacked at this point replace
            if (!preview)
            {
                RemoveItemIndex(existingItemIndex);
                Debug.Assert(PlaceItem(itemInstance, x, y), "Item place failed after removing single overlapping item, this should never happen.");
            }
            return (ItemPlaceResponse.Replaced, existingItem);
        }

        // Overlapping nothing, so try place
        if (PlaceItem(itemInstance, x, y, preview))
        {
            return (ItemPlaceResponse.Placed, itemInstance);
        }

        return (ItemPlaceResponse.Blocked, null);
    }

    public ItemPlaceResponse DepositItem(ItemInstance itemInstance)
    {
        ItemPlaceResponse response = ItemPlaceResponse.Blocked;

        // While can stack item, stack it
        while (true)
        {
            bool found = false;

            foreach (ItemInstance existingItemInstance in ItemInstances)
            {
                if (StackItemOntoExisting(existingItemInstance, itemInstance))
                {
                    response = ItemPlaceResponse.Stacked;
                    found = true;
                }
            }

            if (!found) break;
        }

        // If items are still left, try place anywhere
        if (itemInstance.Amount > 0)
        {
            bool found = false;
            for (int x = 0; x < sizeX && !found; x++)
            {
                for (int y = 0; y < sizeY && !found; y++)
                {
                    if (PlaceItem(itemInstance, x, y))
                    {
                        response = ItemPlaceResponse.Placed;
                        found = true;
                    }
                }
            }
        }

        return response;
    }

    public ItemInstance RemoveItem(int x, int y)
    {
        if (slots[x, y] == -1) return null;
        return RemoveItemIndex(slots[x, y]);
    }

    public bool RemoveItem(ItemInstance itemInstance)
    {
        int index = ItemInstances.IndexOf(itemInstance);
        if (index == -1) return false;
        RemoveItemIndex(index);
        return true;
    }

    private int[,] slots;
    private int sizeX;
    private int sizeY;

    private bool PlaceItem(ItemInstance itemInstance, int x, int y, bool preview = false)
    {
        // Brute force check if the item fits in the inventory
        for (int i = 0; i < itemInstance.Data.SizeX; i++)
        {
            for (int j = 0; j < itemInstance.Data.SizeY; j++)
            {
                if (x + i >= sizeX || y + j >= sizeY || slots[x + i, y + j] != -1) return false;
            }
        }

        if (preview) return true;

        for (int i = 0; i < itemInstance.Data.SizeX; i++)
        {
            for (int j = 0; j < itemInstance.Data.SizeY; j++)
            {
                slots[x + i, y + j] = ItemInstances.Count;
            }
        }

        ItemInstances.Add(itemInstance);
        itemInstance.SetInventory(this, x, y);
        OnItemAdded?.Invoke(itemInstance);
        return true;
    }

    private bool StackItemOntoExisting(ItemInstance existingItemInstance, ItemInstance newItemInstance, bool preview = false)
    {
        if (existingItemInstance.Data != newItemInstance.Data) return false;

        if (existingItemInstance.Amount + newItemInstance.Amount <= existingItemInstance.Data.MaxStackSize)
        {
            if (!preview) existingItemInstance.SetAmount(existingItemInstance.Amount + newItemInstance.Amount);
            return true;
        }
        else if (existingItemInstance.Amount < existingItemInstance.Data.MaxStackSize)
        {
            if (!preview)
            {
                newItemInstance.SetAmount(newItemInstance.Amount - (existingItemInstance.Data.MaxStackSize - existingItemInstance.Amount));
                existingItemInstance.SetAmount(existingItemInstance.Data.MaxStackSize);
            }
            return true;
        }

        return false;
    }

    private ItemInstance RemoveItemIndex(int itemIndex)
    {
        ItemInstance itemInstance = ItemInstances[itemIndex];
        itemInstance.SetInventory(null);
        ItemInstances.RemoveAt(itemIndex);

        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                if (slots[x, y] == itemIndex) slots[x, y] = -1;
                else if (slots[x, y] > itemIndex) slots[x, y]--;
            }
        }

        OnItemRemoved?.Invoke(itemInstance);

        return itemInstance;
    }
}
