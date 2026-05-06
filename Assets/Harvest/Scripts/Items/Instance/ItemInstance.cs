using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ItemRarity { Common, Magic, Rare, Unique }

[Serializable]
public class ItemInstance : ISerdeable<ItemInstanceDTO>
{
    public event Action OnAmountChanged = delegate { };
    public IItemContainer Container { get; private set; }
    public ItemData Data => data;
    public int Amount => amount;
    public IReadOnlyList<PartSlotInstance> PartSlots => partSlots;
    public IReadOnlyList<AffixInstance> Affixes => affixes;

    public static ItemInstance NewResource(ItemData data, int amount)
    {
        return new ItemInstance
        {
            data = data,
            amount = amount
        };
    }

    public static ItemInstance NewComplex(
        ItemData data,
        ItemRarity rarity,
        List<AffixInstance> affixes,
        int level)
    {
        var instance = new ItemInstance
        {
            data = data,
            amount = 1,
            rarity = rarity,
            affixes = affixes,
            level = level,
            partSlots = data.PartSlots.Select(slotDef => new PartSlotInstance(slotDef)).ToList()
        };

        if (data.PartSlots.Count > 0)
        {
            for (int i = 0; i < data.PartSlots.Count; i++)
            {
                PartSlotData slotDef = data.PartSlots[i];
                ItemInstance defaultPart = ItemGenerator.GenerateComplex(slotDef.DefaultItem, level, ItemRarity.Common);
                var partSlot = instance.GetPartSlot(slotDef.RequiredType);
                partSlot?.PlaceItem(defaultPart);
            }
        }

        return instance;
    }

    public void SetContainer(IItemContainer container)
    {
        Container = container;
    }

    public void SetAmount(int amount)
    {
        this.amount = amount;
        OnAmountChanged?.Invoke();
    }

    public PartSlotInstance GetPartSlot(PartType type) =>
        partSlots.FirstOrDefault(s => s.RequiredType == type);

    [SerializeField] private ItemData data;
    [SerializeField] private int amount;
    [SerializeField] private ItemRarity rarity = ItemRarity.Common;
    [SerializeField] private int level = 1;
    [SerializeField] private List<AffixInstance> affixes = new();
    [SerializeField] private List<PartSlotInstance> partSlots = new();

    // -------------------- Serialization  --------------------

    public ItemInstanceDTO Serialize()
    {
        return new ItemInstanceDTO
        {
            ItemID = data.ID,
            Amount = amount,
            Rarity = (int)rarity,
            Level = level,
            Affixes = affixes.Select(affix => affix.Serialize()).ToArray(),
            PartSlots = partSlots.Select(slot => slot.Serialize()).ToArray()
        };
    }

    public void Deserialize(ItemInstanceDTO itemDTO)
    {
        data = AssetDatabase.GetItemData(itemDTO.ItemID);
        amount = itemDTO.Amount;
        rarity = (ItemRarity)itemDTO.Rarity;
        level = itemDTO.Level;
        affixes = itemDTO.Affixes?.Select(affixDTO => AffixInstance.DeserializeNew(affixDTO)).ToList() ?? new();
        for (int i = 0; i < itemDTO.PartSlots.Length; i++)
        {
            partSlots.Add(PartSlotInstance.DeserializeNew(data.PartSlots[i], itemDTO.PartSlots[i]));
        }
    }

    public static ItemInstance DeserializeNew(ItemInstanceDTO itemDTO)
    {
        ItemInstance instance = new();
        instance.Deserialize(itemDTO);
        return instance;
    }

    private ItemInstance() { }
}

public struct ItemInstanceDTO
{
    public string ItemID;
    public int Amount;
    public int Rarity;
    public int Level;
    public AffixInstanceDTO[] Affixes;
    public PartSlotInstanceDTO[] PartSlots;
}