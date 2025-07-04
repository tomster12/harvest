using System;

[Serializable]
public abstract class PlayerTool
{
    protected Player player;
    protected ItemInstance itemInstance;

    public virtual void Equip(Player player, ItemInstance itemInstance)
    {
        this.player = player;
        this.itemInstance = itemInstance;
    }

    public virtual void Unequip()
    {
    }

    public virtual void UpdateTool()
    { }

    public virtual void UseTool()
    { }

    public virtual void DebugGizmos()
    { }
}
