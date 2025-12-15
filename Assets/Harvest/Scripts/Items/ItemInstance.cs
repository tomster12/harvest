using System;
using UnityEngine;

public struct ItemInstanceDTO
{
    public string itemID;
    public int amount;

    public ItemInstanceDTO(string itemID, int amount)
    {
        this.itemID = itemID;
        this.amount = amount;
    }
}

[Serializable]
public class ItemInstance : ISerdeable<ItemInstanceDTO>
{
    public event Action OnAmountChanged = delegate { };

    public ItemData Data => data;
    public int Amount => amount;
    public IItemContainer Container { get; private set; } = null;

    public ItemInstance(ItemData data, int amount)
    {
        this.data = data;
        this.amount = amount;
    }

    public void SetAmount(int amount)
    {
        this.amount = amount;
        OnAmountChanged?.Invoke();
    }

    public void SetContainer(IItemContainer container)
    {
        Container = container;
    }

    public ItemInstanceDTO Serialize()
    {
        return new(data.ID, amount);
    }

    public void Deserialize(ItemInstanceDTO itemDTO)
    {
        data = AssetDatabase.GetItemData(itemDTO.itemID);
        amount = itemDTO.amount;
    }

    public static ItemInstance DeserializeNew(ItemInstanceDTO itemDTO)
    {
        ItemInstance instance = new();
        instance.Deserialize(itemDTO);
        return instance;
    }

    [Header("Variables")]
    [SerializeField] private ItemData data;
    [SerializeField] private int amount;

    private ItemInstance()
    { }
}
