using System;
using UnityEngine;

[Serializable]
public class PlayerTools
{
    public bool IsEquipped => CurrentTool != null;
    public PlayerTool CurrentTool { get; private set; } = null;

    public void Init(Player player)
    {
        this.player = player;
        this.player.Persistent.Gear.OnItemAdded += OnItemEquipped;
        this.player.Persistent.Gear.OnItemRemoved += OnItemUnequipped;
    }

    public void OnItemEquipped(ItemInstance itemInstance)
    {
        if (itemInstance.Data.Type == ItemType.Tool)
        {
            switch (itemInstance.Data.ToolData.Type)
            {
                case ToolType.Axe:
                    CurrentTool = new PlayerAxeTool(player, itemInstance);
                    break;

                default:
                    Debug.LogWarning($"Unhandled tool type: {itemInstance.Data.ToolData.Type}");
                    break;
            }

            CurrentTool?.Equip();
        }
    }

    public void OnItemUnequipped(ItemInstance itemInstance)
    {
        if (itemInstance.Data.Type == ItemType.Tool && CurrentTool != null)
        {
            CurrentTool.Unequip();
            CurrentTool = null;
        }
    }

    private Player player;
}
