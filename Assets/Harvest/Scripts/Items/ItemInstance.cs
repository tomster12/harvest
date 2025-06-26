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
public partial class ItemInstance : ISerdeable<ItemInstanceDTO>
{
    public event Action OnAmountChanged = delegate { };

    public ItemData Data => data;
    public int Amount => amount;
    public GridInventory Inventory { get; private set; } = null;
    public Vector2Int InventoryPosition { get; private set; } = new Vector2Int(-1, -1);

    public ItemInstance(ItemData data, int amount)
    {
        this.data = data;
        this.amount = amount;
    }

    public static ItemInstance DeserializeNew(ItemInstanceDTO itemDTO)
    {
        ItemInstance instance = new();
        instance.Deserialize(itemDTO);
        return instance;
    }

    public ItemInstanceDTO Serialize()
    {
        return new(data.ID, amount);
    }

    public void Deserialize(ItemInstanceDTO itemDTO)
    {
        data = ItemDatabase.GetItemData(itemDTO.itemID);
        amount = itemDTO.amount;
    }

    public void SetAmount(int amount)
    {
        this.amount = amount;
        OnAmountChanged?.Invoke();
    }

    public void SetInventory(GridInventory inventory, int x = -1, int y = -1)
    {
        Inventory = inventory;
        InventoryPosition = new Vector2Int(x, y);
    }

    [SerializeField] private ItemData data;
    [SerializeField] private int amount;

    private ItemInstance()
    { }
}
