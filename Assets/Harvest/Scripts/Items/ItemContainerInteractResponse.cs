public enum ItemContainerInteractType
{ Placed, Stacked, Replaced, Removed, Blocked, OutOfBounds, Invalid };

public struct ItemContainerInteractResponse
{
    public ItemContainerInteractType type;
    public ItemInstance itemInstance;

    public ItemContainerInteractResponse(ItemContainerInteractType type, ItemInstance itemInstance = null)
    {
        this.type = type;
        this.itemInstance = itemInstance;
    }
}
