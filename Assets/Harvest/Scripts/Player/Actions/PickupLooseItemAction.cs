using UnityEngine;
using System.Threading;
using System.Threading.Tasks;

public class PickupLooseItemAction : PlayerAction
{
    private readonly LooseItem item;
    private readonly Vector3 pickupPosition;

    public PickupLooseItemAction(LooseItem item, Vector3 position)
    {
        this.item = item;
        this.pickupPosition = position;

        AddPlayerBlock(PlayerBlockFlags.Inventory | PlayerBlockFlags.Movement);
        AddCancelCondition(new CancelOnMovementInput());
        SetCancellable(true);
    }

    public override async Task RunAsync(Player player, CancellationToken token)
    {
        // Move towards target
        player.Movement.MoveTowardsPosition(pickupPosition);
        while (!player.Movement.ReachedTarget(0.5f))
        {
            await Task.Yield();
            token.ThrowIfCancellationRequested();
        }

        // Pickup the item and update held item
        ItemInstance itemInstance = item.Pickup();
        player.Interactor.HeldItemUI.SetItem(itemInstance);
        Vector2 offset = new(player.Interactor.HeldItemUI.Rect.sizeDelta.x / 2, -player.Interactor.HeldItemUI.Rect.sizeDelta.y / 2);
        player.Interactor.HeldItemUI.SetStateToMouse(offset);
    }
}
