using System;
using UnityEngine;

[Serializable]
public class PartSlotInstance : IItemContainer
{
    public event Action<ItemInstance> OnItemAdded = delegate { };
    public event Action<ItemInstance> OnItemRemoved = delegate { };
    public PartType RequiredType => data.RequiredType;
    public ItemInstance Item => item;

    public PartSlotInstance(PartSlotData data)
    {
        this.data = data;
    }

    public ItemContainerInteractResponse PlaceItem(ItemInstance item, bool preview = false)
    {
        if (item.Data.PartType != data.RequiredType)
        {
            return new ItemContainerInteractResponse(ItemContainerInteractType.Invalid, item);
        }

        ItemInstance displaced = this.item;

        if (!preview)
        {
            if (this.item != null)
            {
                this.item.SetContainer(null);
                OnItemRemoved.Invoke(this.item);
            }
            this.item = item;
            item.SetContainer(this);
            OnItemAdded.Invoke(item);
        }

        return displaced != null
            ? new ItemContainerInteractResponse(ItemContainerInteractType.Replaced, displaced)
            : new ItemContainerInteractResponse(ItemContainerInteractType.Placed, item);
    }

    public ItemContainerInteractResponse PickupItem(ItemInstance item)
    {
        if (this.item != item)
        {
            return new ItemContainerInteractResponse(ItemContainerInteractType.Invalid, item);
        }

        this.item.SetContainer(null);
        OnItemRemoved.Invoke(this.item);
        this.item = null;
        return new ItemContainerInteractResponse(ItemContainerInteractType.Pickup, item);
    }

    [SerializeField] private PartSlotData data;
    [SerializeField] private ItemInstance item = null;

    // -------------------- Serialization  --------------------

    public PartSlotInstanceDTO Serialize()
    {
        return new()
        {
            Part = item != null ? item.Serialize() : null
        };
    }

    public void Deserialize(PartSlotData data, PartSlotInstanceDTO slotDTO)
    {
        this.data = data;

        if (slotDTO.Part.HasValue)
        {
            var item = ItemInstance.DeserializeNew(slotDTO.Part.Value);
            PlaceItem(item);
        }
    }

    public static PartSlotInstance DeserializeNew(PartSlotData data, PartSlotInstanceDTO slotDTO)
    {
        PartSlotInstance instance = new();
        instance.Deserialize(data, slotDTO);
        return instance;
    }

    private PartSlotInstance() { }
}

[Serializable]
public struct PartSlotInstanceDTO
{
    public ItemInstanceDTO? Part;
}
