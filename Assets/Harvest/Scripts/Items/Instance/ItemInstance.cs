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
    public IReadOnlyList<AffixInstance> Affixes => affixes;
    public IReadOnlyList<PartSlot> PartSlots => partSlots;

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
            partSlots = data.PartSlotDefinitions.Select(slotDef => new PartSlot(slotDef)).ToList()
        };

        if (data.PartSlotDefinitions.Count > 0)
        {
            for (int i = 0; i < data.PartSlotDefinitions.Count; i++)
            {
                PartSlotDefinition slotDef = data.PartSlotDefinitions[i];
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

    public PartSlot GetPartSlot(PartType type) =>
        partSlots.FirstOrDefault(s => s.RequiredType == type);

    [SerializeField] private ItemData data;
    [SerializeField] private int amount;
    [SerializeField] private ItemRarity rarity = ItemRarity.Common;
    [SerializeField] private int level = 1;
    [SerializeField] private List<AffixInstance> affixes = new();
    [SerializeField] private List<PartSlot> partSlots = new();

    // -------------------- Serialization  --------------------

    public ItemInstanceDTO Serialize()
    {
        return new ItemInstanceDTO
        {
            itemID = data.ID,
            amount = amount,
            rarity = (int)rarity,
            level = level,
            affixes = affixes.Select(affix => affix.Serialize()).ToArray(),
            partSlots = partSlots.Select(slot => slot.Serialize()).ToArray()
        };
    }

    public void Deserialize(ItemInstanceDTO itemDTO)
    {
        data = AssetDatabase.GetItemData(itemDTO.itemID);
        amount = itemDTO.amount;
        rarity = (ItemRarity)itemDTO.rarity;
        level = itemDTO.level;
        affixes = itemDTO.affixes?.Select(affixDTO => AffixInstance.DeserializeNew(affixDTO)).ToList() ?? new();
        for (int i = 0; i < itemDTO.partSlots.Length; i++)
        {
            partSlots.Add(PartSlot.DeserializeNew(data.PartSlotDefinitions[i], itemDTO.partSlots[i]));
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
    public string itemID;
    public int amount;
    public int rarity;
    public int level;
    public AffixInstanceDTO[] affixes;
    public PartSlotDTO[] partSlots;
}