using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class PlayerDragAction : PlayerAction
{
    public override bool IsAvailable =>
        player.Animator.CanControl(ANIMATION_PRIORITY);

    public override bool IsRunnable =>
        hoveredDraggable != null;

    public PlayerDragAction(Player player)
    {
        this.player = player;
        movementCancel = new CancelOnMovementInput();
        Configure(new()
        {
            movementCancel,
            new CancelOnMouseRelease()
        });
    }

    public override void UpdateAvailable()
    {
        // Find draggable object under cursor
        DraggableObject newHovered = null;
        foreach (var hit in player.Input.RaycastHits)
        {
            newHovered = hit.collider.GetComponent<DraggableObject>();
            if (newHovered != null)
            {
                hoveredHit = hit;
                break;
            }
        }

        // Update hover state
        if (newHovered != hoveredDraggable)
        {
            hoveredDraggable?.OnHoverExit();
            hoveredDraggable = newHovered;
            hoveredDraggable?.OnHoverEnter();
        }
    }

    public void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(grabPosition, 0.2f);
    }

    private readonly Player player;
    private PlayerActionCancelCondition movementCancel;
    private RaycastHit hoveredHit;
    private DraggableObject hoveredDraggable;
    private DraggableObject currentDraggable;
    private Vector3 grabPosition;

    protected override async Task Start(CancellationToken ct)
    {
        // Find Where we will grab on the object
        currentDraggable = hoveredDraggable;
        hoveredDraggable = null;
        var grab = currentDraggable.GetGrab(hoveredHit);

        // Walk to object
        movementCancel.Active = true;
        await AsyncUtil.While(() =>
        {
            grabPosition = grab.ResolvePosition();
            player.Movement.SetMovementTarget(grabPosition, 0.5f);
            return player.Movement.HasReachedTarget;
        }, ct);

        // Drag with the players movement
        movementCancel.Active = false;
        await AsyncUtil.While(() =>
        {
            return currentDraggable.DragTo(grab, player.transform.position, 1.0f, 0.5f);
        }, ct);
    }

    protected override Task Stop(CancellationToken ct)
    {
        currentDraggable?.OnHoverExit();
        return Task.CompletedTask;
    }

    private static readonly int ANIMATION_PRIORITY = 0;
}
