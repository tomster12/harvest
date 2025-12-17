using UnityEngine;
using System.Threading;
using System.Threading.Tasks;

public class PlayerPickupItemAction : PlayerAction
{
    private readonly LooseItem item;
    private readonly Vector3 pickupPosition;

    public PlayerPickupItemAction(Player player, LooseItem item, Vector3 position) : base(player)
    {
        this.item = item;
        this.pickupPosition = position;

        AddPlayerRestriction(Player.Restriction.InteractContainers | Player.Restriction.Movement);
        AddCancelCondition(new CancelOnMovementInput());
        SetCancellable(true);
    }

    protected override async Task Start(CancellationToken ct)
    {
        player.Movement.MoveTowardsPosition(pickupPosition, 0.5f);
        await AsyncUtil.WaitUntil(() => player.Movement.HasReachedTarget, ct);

        ItemInstance itemInstance = item.Pickup();
        player.Interaction.HeldItemUI.SetItem(itemInstance);
        Vector2 offset = player.Interaction.HeldItemUI.Rect.sizeDelta / 2;
        offset = new(offset.x, -offset.y);
        player.Interaction.HeldItemUI.SetStateToMouse(offset);
    }
}
