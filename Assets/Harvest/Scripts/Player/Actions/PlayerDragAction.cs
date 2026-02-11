using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.VisualScripting;
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
        this.movementCancel = new CancelOnMovementInput();

        Configure(new()
        {
            movementCancel,
            new CancelOnMouseRelease()
        });

        // Get animation transforms
        leftHand = player.Animator.GetAnimationTransform("Left Hand");
        rightHand = player.Animator.GetAnimationTransform("Right Hand");
        handGrabPos = player.Animator.GetAnimationPoint("Hands - Drag");

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
    private Transform leftHand;
    private Transform rightHand;
    private Transform handGrabPos;

    private RaycastHit hoveredHit;
    private DraggableObject hoveredDraggable;

    // Active
    private State state = State.Goto;
    private PlayerAnimator.AcControlHandle acHandle;
    private PlayerActionCancelCondition movementCancel;
    private DraggableObject currentDraggable;
    private DraggableObject.Grab currentGrab;
    private Vector3 playerDragFromPos;
    private Vector3 targetGrabToPos;
    private Vector3 targetGrabFromPos;

    protected override async Task Start(CancellationToken ct)
    {
        state = State.Goto;
        acHandle = player.Animator.TakeControl(ANIMATION_PRIORITY);
        if (acHandle == null) throw new OperationCanceledException("Failed to take control of animator.");

        // Find Where we will grab on the object
        currentDraggable = hoveredDraggable;
        hoveredDraggable = null;
        currentGrab = currentDraggable.GetGrab(hoveredHit);

        // Move to position within range of the target, and get hands ready
        movementCancel.Active = true;
        playerDragFromPos = currentGrab.ResolvePosition();
        await Task.WhenAll(
            AsyncUtil.While(() =>
            {
                player.Movement.SetMovementTarget(playerDragFromPos, 0.5f);
                return player.Movement.HasReachedTarget;
            }, ct),
            AnimationUtil.MoveTo(ct,
                leftHand, handGrabPos.localPosition, Axis.Local,
                0.3f, Easing.EaseInQuad
            ),
            AnimationUtil.MoveTo(ct,
                rightHand, handGrabPos.localPosition, Axis.Local,
                0.3f, Easing.EaseInQuad
            )
        );

        state = State.Drag;

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
        player.Movement.SetFacingTarget(null);
        currentDraggable?.OnHoverExit();
        acHandle?.Release();
        return Task.CompletedTask;
    }

    private static readonly int ANIMATION_PRIORITY = 0;


    private enum State
    { Goto, Drag };
}
