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

    public List<ItemInstance> ItemInstances => itemInstances;

    public GearInventoryDTO Serialize()
    {
        return new GearInventoryDTO();
    }

    public void Deserialize(GearInventoryDTO inventoryDTO)
    {
    }

    public ItemContainerInteractResponse PlaceOrStackItem(ItemInstance itemInstance, bool preview = false)
    {
        return new ItemContainerInteractResponse(ItemContainerInteractType.Blocked, itemInstance);
    }

    public ItemContainerInteractResponse RemoveItem(ItemInstance itemInstance)
    {
        return new ItemContainerInteractResponse(ItemContainerInteractType.Blocked, itemInstance);
    }

    private readonly List<ItemInstance> itemInstances = new();
}
