using System;
using UnityEngine;

[Serializable]
public class PlayerToolHandler
{
    public ItemInstance CurrentTool { get; private set; } = null;

    public void Init(Player player)
    {
        this.player = player;

        player.Persistent.Gear.OnItemAdded += OnItemAdded;
        player.Persistent.Gear.OnItemRemoved += OnItemRemoved;
    }

    private Player player;

    private void OnItemAdded(ItemInstance itemInstance)
    {
        Debug.Log($"Item added: name={itemInstance.Data.Name} id={itemInstance.Data.ID} type={itemInstance.Data.Type}");
        if (itemInstance.Data.Type == ItemType.Tool) CurrentTool = itemInstance;
    }

    private void OnItemRemoved(ItemInstance itemInstance)
    {
        Debug.Log($"Item removed: name={itemInstance.Data.Name} id={itemInstance.Data.ID} type={itemInstance.Data.Type}");
        if (itemInstance == CurrentTool) CurrentTool = null;
    }
}
