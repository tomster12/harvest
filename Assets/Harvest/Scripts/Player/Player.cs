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
        InteractWorld = 1 << 3,
        Action = 1 << 4
    }

    public PlayerPersistent Persistent;
    public PlayerInput Input;
    public PlayerCamera Camera;
    public PlayerToolHandler ToolHandler;
    public PlayerInteractor Interactor;
    public PlayerMovement Movement;
    public PlayerActionHandler Actions;
    public PlayerAnimator Animator;

    public ActionRestriction ActionRestrictions = ActionRestriction.None;

    public void Init(PlayerPersistent persistent)
    {
        Persistent = persistent;

        Input.Init(this);
        Camera.Init(this);
        ToolHandler.Init(this);
        Interactor.Init(this);
        Movement.Init(this);
        Actions.Init(this);
        Animator.Init(this);

        idleAnimation = new IdleAnimation(Animator);
        walkingAnimation = new WalkingAnimation(Animator);
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

    private PlayerAnimator.ControlHandle animatorControl;
    private IdleAnimation idleAnimation;
    private WalkingAnimation walkingAnimation;

    private void Update()
    {
        Input.ReceiveInput();

        Camera.UpdateCamera();

        ToolHandler.UpdateTool();

        Interactor.HandleInteractingContainers();
        Interactor.HandleInteractingWorld();

        Actions.UpdateActions();

        UpdateBaseAnimations();
        Animator.UpdateAnimations();
    }

    private void UpdateBaseAnimations()
    {
        // Always try and take control if possible
        if (!Animator.IsControlled)
        {
            animatorControl = Animator.TakeControl(-1);
            animatorControl.OnCancelled = () => animatorControl = null;
        }

        // We have control so idle or walk
        if (animatorControl != null)
        {
            if (Movement.IsMoving && Animator.CurrentAnimation != walkingAnimation) animatorControl.StartAnimation(walkingAnimation);
            else if (!Movement.IsMoving && Animator.CurrentAnimation != idleAnimation) animatorControl.StartAnimation(idleAnimation);
        }
    }

    private void FixedUpdate()
    {
        if (!IsRestricted(ActionRestriction.Movement) && Input.IsInputtingMovement)
        {
            Movement.MoveInDirection(Input.InputMovement);
        }
        Movement.FixedUpdate();
        Camera.FollowPlayerPosition();
    }

    private void LateUpdate()
    {
        Movement.LateUpdate();
    }

    private void OnDrawGizmos()
    {
        ToolHandler.DebugGizmos();
    }
}
