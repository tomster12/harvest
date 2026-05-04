using System;
using UnityEngine;

[Serializable]
public class PlayerTools
{
    public bool IsEquipped => CurrentTool != null;
    public PlayerTool CurrentTool => currentTool;

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
            switch (itemInstance.Data.ToolType)
            {
                case ToolType.Axe:
                    currentTool = new PlayerAxeTool(player, itemInstance);
                    break;

                default:
                    Debug.LogWarning($"Unhandled tool type: {itemInstance.Data.ToolType}");
                    break;
            }

            currentTool?.Equip();
        }
    }

    public void OnItemUnequipped(ItemInstance itemInstance)
    {
        if (itemInstance.Data.Type == ItemType.Tool && currentTool != null)
        {
            currentTool.Unequip();
            currentTool = null;
        }
    }

    private Player player;
    [SerializeField] private PlayerTool currentTool;
}
