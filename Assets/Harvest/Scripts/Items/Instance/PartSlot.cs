using System;
using UnityEngine;

[Serializable]
public class PartSlot : IItemContainer
{
    public event Action<ItemInstance> OnItemAdded = delegate { };
    public event Action<ItemInstance> OnItemRemoved = delegate { };
    public PartType RequiredType => requiredType;
    public ItemInstance Part => part;

    public PartSlot(PartSlotDefinition slotDefinition)
    {
        requiredType = slotDefinition.RequiredType;
    }

    public ItemContainerInteractResponse PlaceItem(ItemInstance item, bool preview = false)
    {
        if (item.Data.PartType != requiredType)
        {
            return new ItemContainerInteractResponse(ItemContainerInteractType.Invalid, item);
        }

        ItemInstance displaced = part;

        if (!preview)
        {
            if (part != null)
            {
                part.SetContainer(null);
                OnItemRemoved.Invoke(part);
            }
            part = item;
            item.SetContainer(this);
            OnItemAdded.Invoke(item);
        }

        return displaced != null
            ? new ItemContainerInteractResponse(ItemContainerInteractType.Replaced, displaced)
            : new ItemContainerInteractResponse(ItemContainerInteractType.Placed, item);
    }

    public ItemContainerInteractResponse PickupItem(ItemInstance item)
    {
        if (part != item)
        {
            return new ItemContainerInteractResponse(ItemContainerInteractType.Invalid, item);
        }

        part.SetContainer(null);
        OnItemRemoved.Invoke(part);
        part = null;
        return new ItemContainerInteractResponse(ItemContainerInteractType.Pickup, item);
    }

    [SerializeField] private PartType requiredType;
    [SerializeField] private ItemInstance part = null;

    // -------------------- Serialization  --------------------

    public PartSlotDTO Serialize()
    {
        return new()
        {
            part = part != null ? part.Serialize() : null
        };
    }

    public void Deserialize(PartSlotDefinition slotDefinition, PartSlotDTO slotDTO)
    {
        requiredType = slotDefinition.RequiredType;

        if (slotDTO.part.HasValue)
        {
            var item = ItemInstance.DeserializeNew(slotDTO.part.Value);
            PlaceItem(item);
        }
    }

    public static PartSlot DeserializeNew(PartSlotDefinition slotDefinition, PartSlotDTO slotDTO)
    {
        PartSlot instance = new();
        instance.Deserialize(slotDefinition, slotDTO);
        return instance;
    }

    private PartSlot() { }
}

[Serializable]
public struct PartSlotDTO
{
    public ItemInstanceDTO? part;
}
