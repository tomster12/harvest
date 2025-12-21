using System;

[Serializable]
public abstract class PlayerTool
{
    protected Player player;
    protected ItemInstance itemInstance;

    public PlayerTool(Player player, ItemInstance itemInstance)
    {
        this.player = player;
        this.itemInstance = itemInstance;
    }

    public virtual void Equip()
    { }

    public virtual void Unequip()
    { }
}
