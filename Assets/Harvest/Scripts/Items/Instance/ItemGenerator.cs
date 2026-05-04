using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ItemGenerator
{
    private static System.Random rng = new();

    public static ItemInstance GenerateComplex(ItemData data, int level, ItemRarity? forceRarity = null)
    {
        // Roll rarity
        ItemRarity rarity = forceRarity ?? RollRarity();

        // Roll affixes from item specific pool
        List<AffixInstance> affixes = new();
        if (data.AffixPool != null)
        {
            var chosenAffixes = data.AffixPool.RollAffixes(rarity, rng);
            affixes = chosenAffixes.Select(def =>
                new AffixInstance(def, Mathf.Lerp(def.MinValue, def.MaxValue, (float)rng.NextDouble()))
            ).ToList();
        }

        var instance = ItemInstance.NewComplex(
            data: data,
            rarity: rarity,
            affixes: affixes,
            level: level);

        return instance;
    }

    private static ItemRarity RollRarity()
    {
        float roll = (float)rng.NextDouble();
        return roll switch
        {
            < 0.55f => ItemRarity.Common,
            < 0.80f => ItemRarity.Magic,
            < 0.95f => ItemRarity.Rare,
            _ => ItemRarity.Unique
        };
    }
}
