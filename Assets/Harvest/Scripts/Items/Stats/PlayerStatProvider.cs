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

        // Items already equipped need their part slots watched too
        foreach (var item in this.player.Persistent.Gear.EquipmentItems.Values) SubscribeToParts(item);
        SubscribeToParts(this.player.Persistent.Gear.ToolItem);

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

    private void OnPlayerItemAdded(ItemInstance item)
    {
        SubscribeToParts(item);
        isDirty = true;
    }

    private void OnPlayerItemRemoved(ItemInstance item)
    {
        UnsubscribeFromParts(item);
        isDirty = true;
    }

    private void OnPlayerPartAdded(ItemInstance part) => OnPlayerItemAdded(part);

    private void OnPlayerPartRemoved(ItemInstance part) => OnPlayerItemRemoved(part);

    private void SubscribeToParts(ItemInstance item)
    {
        if (item == null) return;

        foreach (var slot in item.PartSlots)
        {
            slot.OnItemAdded -= OnPlayerPartAdded;
            slot.OnItemRemoved -= OnPlayerPartRemoved;
            slot.OnItemAdded += OnPlayerPartAdded;
            slot.OnItemRemoved += OnPlayerPartRemoved;
            SubscribeToParts(slot.Item);
        }
    }

    private void UnsubscribeFromParts(ItemInstance item)
    {
        if (item == null) return;

        foreach (var slot in item.PartSlots)
        {
            slot.OnItemAdded -= OnPlayerPartAdded;
            slot.OnItemRemoved -= OnPlayerPartRemoved;
            UnsubscribeFromParts(slot.Item);
        }
    }

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
