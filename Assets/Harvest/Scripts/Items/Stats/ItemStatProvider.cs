public class ItemStatProvider : IStatProvider
{
    private readonly ItemInstance item;

    public ItemStatProvider(ItemInstance item)
    {
        this.item = item;
    }

    public float GetStat(StatType stat)
    {
        return StatResolver.ResolveItemStat(item, stat);
    }
}
