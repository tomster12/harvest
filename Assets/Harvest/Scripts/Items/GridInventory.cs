using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public struct GridInventoryDTO
{
    public (Vector2Int, ItemInstanceDTO)[] itemInstanceDTOs;
    public int[,] slots;

    public GridInventoryDTO((Vector2Int, ItemInstanceDTO)[] itemInstanceDTOs, int[,] slots)
    {
        this.itemInstanceDTOs = itemInstanceDTOs;
        this.slots = slots;
    }
}

public partial class GridInventory : ISerdeable<GridInventoryDTO>, IItemContainer
{
    public event Action<ItemInstance> OnItemAdded = delegate { };

    public event Action<ItemInstance> OnItemRemoved = delegate { };

    public List<ItemInstance> ItemInstances => itemInstances;

    public int SizeX => sizeX;
    public int SizeY => sizeY;

    public GridInventory(int sizeX, int sizeY)
    {
        itemInstances = new List<ItemInstance>();
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

    public GridInventoryDTO Serialize()
    {
        (Vector2Int, ItemInstanceDTO)[] itemInstanceDTOs = new (Vector2Int, ItemInstanceDTO)[itemInstances.Count];
        for (int i = 0; i < itemInstances.Count; i++) itemInstanceDTOs[i] = (itemPositions[itemInstances[i]], itemInstances[i].Serialize());
        return new(itemInstanceDTOs, slots);
    }

    public void Deserialize(GridInventoryDTO inventoryDTO)
    {
        slots = inventoryDTO.slots;
        sizeX = inventoryDTO.slots.GetLength(0);
        sizeY = inventoryDTO.slots.GetLength(1);
        itemInstances.Clear();
        for (int i = 0; i < inventoryDTO.itemInstanceDTOs.Length; i++)
        {
            var (itemPos, itemInstanceDTO) = inventoryDTO.itemInstanceDTOs[i];
            ItemInstance itemInstance = ItemInstance.DeserializeNew(itemInstanceDTO);
            itemInstance.SetContainer(this);
            itemInstances.Add(itemInstance);
            itemPositions[itemInstance] = itemPos;
        }
    }

    public ItemContainerInteractResponse PlaceItem(ItemInstance itemInstance, int x, int y, bool preview = false)
    {
        // Check position is in bounds
        if (x < 0 || y < 0 || x + itemInstance.Data.SizeX > sizeX || y + itemInstance.Data.SizeY > sizeY)
        {
            // return (ItemPlaceResponse.OutOfBounds, null);
            return new ItemContainerInteractResponse(ItemContainerInteractType.OutOfBounds);
        }

        // Check if item under cursor matches and stack
        if (slots[x, y] != -1)
        {
            ItemInstance existingItemInstance = itemInstances[slots[x, y]];
            if (StackItemOntoExisting(existingItemInstance, itemInstance, preview))
            {
                return new ItemContainerInteractResponse(ItemContainerInteractType.Stacked, existingItemInstance);
            }
        }

        // Find number of overlapping items
        HashSet<int> overlappingItemInstances = new();
        for (int i = 0; i < itemInstance.Data.SizeX; i++)
        {
            for (int j = 0; j < itemInstance.Data.SizeY; j++)
            {
                if (x + i >= sizeX || y + j >= sizeY || slots[x + i, y + j] == -1) continue;
                overlappingItemInstances.Add(slots[x + i, y + j]);
            }
        }

        // Overlapping 2+ items, therefore blocked
        if (overlappingItemInstances.Count > 1)
        {
            return new ItemContainerInteractResponse(ItemContainerInteractType.Blocked);
        }

        // Overlapping 1 item
        else if (overlappingItemInstances.Count == 1)
        {
            var existingItemIndex = overlappingItemInstances.First();
            var existingItemInstance = itemInstances[existingItemIndex];

            // If item matches try stack
            if (existingItemInstance.Data == itemInstance.Data)
            {
                if (StackItemOntoExisting(existingItemInstance, itemInstance, preview))
                {
                    return new ItemContainerInteractResponse(ItemContainerInteractType.Stacked, existingItemInstance);
                }
            }

            // If have not stacked at this point replace
            if (!preview)
            {
                RemoveItemIndex(existingItemIndex);
                Debug.Assert(PlaceItemAtPosition(itemInstance, x, y, false), "Item place failed after removing single overlapping item, this should never happen.");
            }
            return new ItemContainerInteractResponse(ItemContainerInteractType.Replaced, existingItemInstance);
        }

        // Overlapping nothing, so try place
        if (PlaceItemAtPosition(itemInstance, x, y, preview))
        {
            return new ItemContainerInteractResponse(ItemContainerInteractType.Placed, itemInstance);
        }

        return new ItemContainerInteractResponse(ItemContainerInteractType.Blocked);
    }

    public ItemContainerInteractResponse DepositItem(ItemInstance itemInstance)
    {
        ItemContainerInteractType responseType = ItemContainerInteractType.Blocked;

        // While can stack item, stack it
        while (true)
        {
            bool found = false;

            foreach (ItemInstance existingItemInstance in itemInstances)
            {
                if (StackItemOntoExisting(existingItemInstance, itemInstance))
                {
                    responseType = ItemContainerInteractType.Stacked;
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
                    if (PlaceItemAtPosition(itemInstance, x, y))
                    {
                        responseType = ItemContainerInteractType.Placed;
                        found = true;
                    }
                }
            }
        }

        return new ItemContainerInteractResponse(responseType, itemInstance);
    }

    public ItemContainerInteractResponse RemoveItem(int x, int y)
    {
        if (slots[x, y] == -1) return new ItemContainerInteractResponse(ItemContainerInteractType.Invalid);
        return RemoveItemIndex(slots[x, y]);
    }

    public ItemContainerInteractResponse PickupItem(ItemInstance itemInstance)
    {
        int index = itemInstances.IndexOf(itemInstance);
        if (index == -1) return new ItemContainerInteractResponse(ItemContainerInteractType.Invalid);
        return RemoveItemIndex(index);
    }

    public bool TryGetItemPosition(ItemInstance itemInstance, out Vector2Int gridPos)
    {
        if (itemPositions.TryGetValue(itemInstance, out gridPos)) return true;
        gridPos = default;
        return false;
    }

    private readonly List<ItemInstance> itemInstances = new();
    private readonly Dictionary<ItemInstance, Vector2Int> itemPositions = new();
    private int[,] slots;
    private int sizeX;
    private int sizeY;

    private bool PlaceItemAtPosition(ItemInstance itemInstance, int x, int y, bool preview = false)
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
                slots[x + i, y + j] = itemInstances.Count;
            }
        }

        itemInstances.Add(itemInstance);
        itemPositions[itemInstance] = new Vector2Int(x, y);
        itemInstance.SetContainer(this);
        OnItemAdded?.Invoke(itemInstance);
        return true;
    }

    private bool StackItemOntoExisting(ItemInstance existingItemInstance, ItemInstance newItemInstance, bool preview = false)
    {
        if (existingItemInstance.Data != newItemInstance.Data) return false;

        if (existingItemInstance.Amount + newItemInstance.Amount <= existingItemInstance.Data.MaxStackSize)
        {
            if (!preview)
            {
                existingItemInstance.SetAmount(existingItemInstance.Amount + newItemInstance.Amount);
                newItemInstance.SetAmount(0);
            }
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

    private ItemContainerInteractResponse RemoveItemIndex(int itemIndex)
    {
        ItemInstance itemInstance = itemInstances[itemIndex];
        itemInstance.SetContainer(null);
        itemInstances.RemoveAt(itemIndex);
        itemPositions.Remove(itemInstance);

        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                if (slots[x, y] == itemIndex) slots[x, y] = -1;
                else if (slots[x, y] > itemIndex) slots[x, y]--;
            }
        }

        OnItemRemoved?.Invoke(itemInstance);
        return new ItemContainerInteractResponse(ItemContainerInteractType.Removed, itemInstance);
    }
}
