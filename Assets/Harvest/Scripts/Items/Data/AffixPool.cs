using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "AffixPool_", menuName = "AffixPool")]
public class AffixPool : ScriptableObject
{
    public int MinAffixes = 0;
    public int MaxAffixes = 3;
    public List<AffixData> Affixes = new();

    public List<AffixData> RollAffixes(ItemRarity rarity, System.Random rng)
    {
        // Roll number of affixes based on rarity
        int rarityBonus = rarity switch
        {
            ItemRarity.Magic => 1,
            ItemRarity.Rare => 2,
            ItemRarity.Unique => MaxAffixes,
            _ => 0
        };

        int count = Mathf.Min(
            rng.Next(MinAffixes, MaxAffixes + 1) + rarityBonus,
            MaxAffixes
        );

        // Weighted sampling without replacement
        var affixPool = Affixes
            .Where(a => rarity == ItemRarity.Unique || !a.IsUnique)
            .ToList();

        float totalWeight = affixPool.Sum(a => a.Weight);

        var chosenAffixes = new List<AffixData>();

        for (int i = 0; i < count && affixPool.Count > 0; i++)
        {
            AffixData chosen = null;

            float weightRoll = (float)rng.NextDouble() * totalWeight;
            float weightAcc = 0f;
            foreach (var a in affixPool)
            {
                weightAcc += a.Weight;
                if (weightRoll <= weightAcc) { chosen = a; break; }
            }
            if (chosen == null)
            {
                chosen = affixPool[^1];
            }

            chosenAffixes.Add(chosen);
            totalWeight -= chosen.Weight;
            affixPool.Remove(chosen);
        }

        return chosenAffixes;
    }
}