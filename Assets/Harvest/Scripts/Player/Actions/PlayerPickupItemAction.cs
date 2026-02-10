using UnityEngine;
using System.Threading;
using System.Threading.Tasks;

public class PlayerPickupItemAction : PlayerAction
{
    public override bool IsAvailable =>
        !player.Interactor.IsHoveringContainer &&
        !player.Interactor.IsHoldingItem &&
        player.Animator.CanControl(ANIMATION_PRIORITY);

    public override bool IsRunnable =>
        hoveredLooseItem != null;

    public PlayerPickupItemAction(Player player)
    {
        this.player = player;

        Configure(new()
        {
            new CancelOnMovementInput()
        });
    }

    public override void UpdateAvailable()
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

    private readonly Player player;
    private LooseItem hoveredLooseItem;
    private LooseItem currentItem;
    private Vector3 currentPickupPosition;

    protected override async Task Start(CancellationToken ct)
    {
        player.Input.IsMousePressed = false;
        currentItem = hoveredLooseItem;
        currentPickupPosition = hoveredLooseItem.transform.position - (hoveredLooseItem.transform.position - player.transform.position).normalized * 0.1f;

        player.Movement.SetMovementTarget(currentPickupPosition, 0.5f);
        await AsyncUtil.While(() => player.Movement.HasReachedTarget, ct);

        ItemInstance itemInstance = currentItem.Pickup();
        player.Interactor.HeldItemUI.SetItem(itemInstance);
        Vector2 offset = player.Interactor.HeldItemUI.Rect.sizeDelta / 2;
        offset = new(offset.x, -offset.y);
        player.Interactor.HeldItemUI.SetStateToMouse(offset);
    }

    private static readonly int ANIMATION_PRIORITY = 0;
}
