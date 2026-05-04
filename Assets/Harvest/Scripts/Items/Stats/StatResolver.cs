public static class StatResolver
{
    public static float ResolveItemStat(ItemInstance item, StatType stat)
    {
        if (item == null) return 0f;

        float value = GetBaseValue(item.Data, stat);
        float totalAdditive = 0.0f;
        float totalMultiplicative = 1.0f;

        void ApplyAffix(AffixInstance affix)
        {
            if (affix.Data.ValueType == AffixValueType.Additive)
            {
                totalAdditive += affix.RolledValue;
            }
            else
            {
                totalMultiplicative += affix.RolledValue;
            }
        }

        // Add affix contributions from this item
        foreach (var affix in item.Affixes)
        {
            if (affix.Data.Stat != stat) continue;
            ApplyAffix(affix);
        }

        // Add affix contributions from each fitted part
        foreach (var slot in item.PartSlots)
        {
            foreach (var affix in slot.Part.Affixes)
            {
                if (affix.Data.Stat != stat) continue;
                ApplyAffix(affix);
            }
        }

        return (value + totalAdditive) * totalMultiplicative;
    }

    private static float GetBaseValue(ItemData itemData, StatType stat)
    {
        return stat switch
        {
            StatType.SwingDamage => 1f,
            StatType.SwingSpeed => 1f,
            StatType.ResourceYield => 1f,
            StatType.Armour => 1f,
            StatType.MovementSpeed => 1f,
            StatType.CarryCapacity => 1f,
            _ => throw new System.NotImplementedException()
        };
    }
}
