using UnityEngine;
using System.Threading;
using System.Threading.Tasks;

public class PickupLooseItemAction : PlayerAction
{
    private readonly LooseItem item;
    private readonly Vector3 pickupPosition;

    public PickupLooseItemAction(Player player, LooseItem item, Vector3 position) : base(player)
    {
        this.item = item;
        this.pickupPosition = position;

        AddPlayerRestriction(PlayerRestrictionFlag.InteractInventory | PlayerRestrictionFlag.DoMovement);
        AddCancelCondition(new CancelOnMovementInput());
        SetCancellable(true);
    }

    protected override async Task RunAsync(CancellationToken ct)
    {
        player.Movement.MoveTowardsPosition(pickupPosition, 0.5f);
        await AsyncUtil.WaitUntil(() => player.Movement.HasReachedTarget, ct);

        ItemInstance itemInstance = item.Pickup();
        player.Interactor.HeldItemUI.SetItem(itemInstance);
        Vector2 offset = player.Interactor.HeldItemUI.Rect.sizeDelta / 2;
        offset = new(offset.x, -offset.y);
        player.Interactor.HeldItemUI.SetStateToMouse(offset);
    }
}
