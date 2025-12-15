using System;
using UnityEngine;


public class Player : MonoBehaviour
{
    [Flags]
    public enum ActionRestriction
    {
        None = 0,
        Movement = 1 << 0,
        InteractContainers = 1 << 1,
        InteractLooseItems = 1 << 2,
        InteractLargeObjects = 1 << 3,
        Action = 1 << 4
    }

    public PlayerPersistent Persistent;
    public PlayerCamera Camera;
    public PlayerInput Input;
    public PlayerMovement Movement;
    public PlayerInteraction Interaction;
    public PlayerToolHandler Tools;
    public PlayerActionHandler Actions;
    public PlayerAnimator Animator;

    public ActionRestriction ActionRestrictions = ActionRestriction.None;

    public void Init(PlayerPersistent persistent)
    {
        Persistent = persistent;

        Input.Init(this);
        Camera.Init(this);
        Tools.Init(this);
        Interaction.Init(this);
        Movement.Init(this);
        Actions.Init(this);
        Animator.Init(this);

        idleAnimation = new PlayerIdleAnimation(Animator);
        walkingAnimation = new PlayerWalkingAnimation(Animator);
    }

    public void RestrictActions(ActionRestriction flags)
    {
        ActionRestrictions |= flags;
    }

    public void UnrestrictActions(ActionRestriction flags)
    {
        ActionRestrictions &= ~flags;
    }

    public bool IsRestricted(ActionRestriction flags) => (ActionRestrictions & flags) != ActionRestriction.None;

    private PlayerAnimator.ControlHandle animatorControlHandle;
    private PlayerIdleAnimation idleAnimation;
    private PlayerWalkingAnimation walkingAnimation;

    private void Update()
    {
        Input.ReceiveInput();

        Camera.UpdateCamera();

        Tools.UpdateCurrentTool();

        Interaction.HandleInteractingContainers();

        Interaction.HandleInteractingWorld();

        Actions.UpdateActions();

        ApplyBaseAnimations();

        Animator.UpdateAnimations();
    }

    private void FixedUpdate()
    {
        if (!IsRestricted(ActionRestriction.Movement) && Input.IsInputtingMovement)
        {
            Movement.MoveInDirection(Input.InputMovement);
        }

        Movement.FixedUpdateMovement();

        Camera.FollowPlayerPosition();
    }

    private void LateUpdate()
    {
        Movement.LateUpdateMovement();
    }

    private void OnDrawGizmos()
    {
        Tools.DebugCurrentToolGizmos();
    }

    private void ApplyBaseAnimations()
    {
        // Always try and take control if possible
        if (!Animator.IsControlled)
        {
            animatorControlHandle = Animator.TakeControl(-1);
            animatorControlHandle.OnCancelled = () => animatorControlHandle = null;
        }

        // We have control so idle or walk
        if (animatorControlHandle != null)
        {
            if (Movement.IsMoving && Animator.CurrentAnimation != walkingAnimation)
            {
                animatorControlHandle.StartAnimation(walkingAnimation);
            }
            else if (!Movement.IsMoving && Animator.CurrentAnimation != idleAnimation)
            {
                animatorControlHandle.StartAnimation(idleAnimation);
            }
        }
    }
}
