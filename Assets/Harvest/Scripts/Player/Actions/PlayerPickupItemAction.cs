using UnityEngine;
using System.Threading;
using System.Threading.Tasks;

public class PlayerPickupItemAction : PlayerAction
{
    public override bool IsAvailable =>
            !player.Interaction.IsHoveringContainer &&
            !player.Interaction.IsHoldingItem &&
            !player.IsRestricted(Player.Restriction.InteractLooseItems);

    public override bool IsRunnable => IsAvailable && hoveredLooseItem != null;

    private readonly Player player;
    private LooseItem hoveredLooseItem;
    private LooseItem currentItem;
    private Vector3 currentPickupPosition;

    public PlayerPickupItemAction(Player player)
    {
        this.player = player;

        ConfigActionPlayerRestriction(Player.Restriction.InteractContainers | Player.Restriction.Movement);
        ConfigActionCancelCondition(new CancelOnMovementInput());
        ConfigActionCancellable(true);
    }

    public override void Preview()
    {
        // Get first hovered transform that is a loose item
        LooseItem newHoveredLooseItem = null;
        foreach (RaycastHit hit in player.Input.RaycastHits)
        {
            if (hit.rigidbody != null)
            {
                newHoveredLooseItem = hit.rigidbody.GetComponent<LooseItem>();
                if (newHoveredLooseItem != null) break;
            }
        }

        // Hovering a new loose item so update and preview
        if (newHoveredLooseItem != hoveredLooseItem)
        {
            if (hoveredLooseItem != null) hoveredLooseItem.OnHoverExit();
            hoveredLooseItem = newHoveredLooseItem;
            if (hoveredLooseItem != null) hoveredLooseItem.OnHoverEnter();
        }
    }

    protected override async Task Start(CancellationToken ct)
    {
        player.Input.IsMousePressed = false;
        currentItem = hoveredLooseItem;
        currentPickupPosition = hoveredLooseItem.transform.position - (hoveredLooseItem.transform.position - player.transform.position).normalized * 0.1f;

        player.Movement.MoveTowardsPosition(currentPickupPosition, 0.5f);
        await AsyncUtil.WaitUntil(() => player.Movement.HasReachedTarget, ct);

        ItemInstance itemInstance = currentItem.Pickup();
        player.Interaction.HeldItemUI.SetItem(itemInstance);
        Vector2 offset = player.Interaction.HeldItemUI.Rect.sizeDelta / 2;
        offset = new(offset.x, -offset.y);
        player.Interaction.HeldItemUI.SetStateToMouse(offset);
    }
}
