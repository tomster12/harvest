using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStatProvider : IStatProvider
{
    public PlayerStatProvider(Player player)
    {
        this.player = player;
        this.player.Persistent.Gear.OnItemAdded += OnPlayerItemAdded;
        this.player.Persistent.Gear.OnItemRemoved += OnPlayerItemRemoved;
        this.player.Buffs.OnChanged += OnPlayerBuffsChanged;

        isDirty = true;
    }

    public float GetStat(Stat stat)
    {
        if (isDirty)
        {
            Recalculate();
            isDirty = false;
        }

        var values = lookup[stat];
        return values.Evaluate();
    }

    private readonly Player player;
    private readonly Dictionary<Stat, StatValues> lookup = new();
    private bool isDirty = true;

    private void OnPlayerItemAdded(ItemInstance _) => isDirty = true;

    private void OnPlayerItemRemoved(ItemInstance _) => isDirty = true;

    private void OnPlayerBuffsChanged() => isDirty = true;

    private void Recalculate()
    {
        lookup.Clear();

        foreach (Stat stat in Enum.GetValues(typeof(Stat)))
        {
            StatValues values = new();
            StatResolver.AccumulateStatFromPlayer(player, stat, values);
            lookup[stat] = values;
        }
    }
}
