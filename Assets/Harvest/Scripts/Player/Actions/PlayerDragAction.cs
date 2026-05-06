using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerDragAction : PlayerAction
{
    public override bool IsAvailable =>
        !player.Gear.IsEquipped &&
        player.Animator.CanControl(ANIMATION_PRIORITY);

    public override bool IsRunnable =>
        hoveredDraggable != null;

    public PlayerDragAction(Player player)
    {
        this.player = player;
        this.movementCancel = new CancelOnMovementInput();

        Configure(new()
        {
            movementCancel,
            new CancelOnMouseRelease()
        });

        // Get animation transforms
        atLeftHand = player.CustomTags.Get(CustomTagType.AnimationTransform, "Left Hand");
        atRightHand = player.CustomTags.Get(CustomTagType.AnimationTransform, "Right Hand");
        apBothHandsGrab = player.CustomTags.Get(CustomTagType.AnimationPoint, "AP - Both Hands - Drag");
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

    public override void FixedUpdateActive()
    {
        // TODO
        if (state == State.Goto)
        {
            playerDragFromPos = currentGrab.ResolvePosition();
        }
        else if (state == State.Drag)
        {
            targetGrabToPos = player.transform.position + Vector3.up * 0.5f;
        }
    }

    public void DrawGizmosActive()
    {
        if (state == State.Goto)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(playerDragFromPos, 0.05f);
        }
        else if (state == State.Drag)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(targetGrabFromPos, 0.03f);

            Gizmos.color = Color.green;
            Gizmos.DrawSphere(targetGrabToPos, 0.05f);
        }
    }

    private readonly Player player;
    private Transform atLeftHand;
    private Transform atRightHand;
    private Transform apBothHandsGrab;

    private RaycastHit hoveredHit;
    private DraggableObject hoveredDraggable;
    private State state = State.Goto;
    private PlayerAnimator.AcControlHandle acHandle;
    private PlayerBuffHandle playerSlowHandle = null;
    private PlayerActionCancelCondition movementCancel;
    private DraggableObject currentDraggable;
    private DraggableObject.Grab currentGrab;
    private Vector3 playerDragFromPos;
    private Vector3 targetGrabToPos;
    private Vector3 targetGrabFromPos;

    protected override async Task Start(CancellationToken ct)
    {
        state = State.Goto;
        acHandle = player.Animator.TakeControl(ANIMATION_PRIORITY)
            ?? throw new OperationCanceledException("Failed to take control of animator.");

        // Find Where we will grab on the object
        currentDraggable = hoveredDraggable;
        hoveredDraggable = null;
        currentGrab = currentDraggable.GetGrab(hoveredHit);

        // Move to position within range of the target
        movementCancel.Active = true;
        playerDragFromPos = currentGrab.ResolvePosition();
        await AsyncUtil.While(() =>
        {
            player.Movement.SetMovementTarget(playerDragFromPos, DRAG_DIST);
            return player.Movement.HasReachedTarget;
        }, ct);

        // Begin dragging and slow down the player
        state = State.Drag;
        playerSlowHandle = player.Buffs.Apply(new PlayerBuffEffect
        {
            Stat = Stat.MovementSpeed,
            Multiplicative = -DRAG_SLOW
        });

        // Tell the hands to move to grab position but ignore for now
        _ = AnimationUtil.MoveTo(ct,
            atLeftHand, apBothHandsGrab.localPosition, Axis.Local,
            0.3f, Easing.EaseInQuad
        );
        _ = AnimationUtil.MoveTo(ct,
            atRightHand, apBothHandsGrab.localPosition, Axis.Local,
            0.3f, Easing.EaseInQuad
        );

        // Drag with the players movement
        movementCancel.Active = false;
        targetGrabToPos = player.transform.position;
        await AsyncUtil.While(() =>
        {
            targetGrabFromPos = currentGrab.ResolvePosition();
            player.Movement.SetFacingTarget(targetGrabFromPos, prioritise: true);
            return !currentDraggable.DragTo(currentGrab, targetGrabToPos, 1.0f, 1.0f);
        }, ct);
    }

    protected override Task Stop(CancellationToken ct)
    {
        playerSlowHandle?.Dispose();
        playerSlowHandle = null;

        player.Movement.SetFacingTarget(null);
        currentDraggable?.OnHoverExit();
        acHandle?.Release();
        return Task.CompletedTask;
    }

    private static readonly int ANIMATION_PRIORITY = 0;
    private static readonly float DRAG_SLOW = 0.4f;
    private static readonly float DRAG_DIST = 0.5f;

    private enum State
    { Goto, Drag };
}
