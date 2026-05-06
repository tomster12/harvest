using UnityEngine;

public static class StatResolver
{
    public static void AccumulateStatFromPlayer(Player player, Stat stat, StatValues values)
    {
        // Add base stats from the player
        foreach (var baseStat in player.BaseStats)
        {
            if (baseStat.Stat != stat) continue;

            values.Base += baseStat.Value;
        }

        // Accumulate all stats from all gear
        foreach (var item in player.Persistent.Gear.EquipmentItems)
        {
            AccumulateStatFromItem(item.Value, stat, values);
        }

        if (player.Persistent.Gear.ToolItem != null)
        {
            AccumulateStatFromItem(player.Persistent.Gear.ToolItem, stat, values);
        }
        
        // Accumulate buffs
        player.Buffs.AccumulateStat(stat, values);
    }

    public static void AccumulateStatFromItem(ItemInstance item, Stat stat, StatValues values)
    {
        if (item == null) return;

        // Add base stats on this item
        foreach (var baseStat in item.Data.BaseStats)
        {
            if (baseStat.Stat != stat) continue;

            values.Base += baseStat.Value;
        }

        // Add affix contributions from this item
        foreach (var affix in item.Affixes)
        {
            if (affix.Data.Stat != stat) continue;

            if (affix.Data.ValueType == AffixValueType.Additive)
            {
                values.Additive += affix.RolledValue;
            }
            else
            {
                values.Multiplicative += affix.RolledValue;
            }
        }

        // Add affix contributions from each fitted part
        foreach (var slot in item.PartSlots)
        {
            AccumulateStatFromItem(slot.Item, stat, values);
        }
    }
}

public class StatValues
{
    public float Base = 0f;
    public float Additive = 0f;
    public float Multiplicative = 1f;

    public float Evaluate() => (Base + Additive) * Multiplicative;

    public void Log(Stat stat) => Debug.Log($"Calculated {stat} as {Evaluate()} = ({Base} + {Additive}) * {Multiplicative}");
}
