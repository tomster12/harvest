using System;
using UnityEngine;

[Flags]
public enum PlayerRestrictionFlag
{
    None = 0,
    DoMovement = 1 << 0,
    InteractInventory = 1 << 1,
    DoAction = 1 << 2
}

public class Player : MonoBehaviour
{
    public PlayerPersistent Persistent;
    public PlayerInput Input;
    public PlayerCamera Camera;
    public PlayerToolHandler ToolHandler;
    public PlayerInteractor Interactor;
    public PlayerMovement Movement;
    public PlayerActionHandler Actions;
    public PlayerAnimator Animator;

    public PlayerRestrictionFlag ActionBlocks = PlayerRestrictionFlag.None;

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

    public void Restrict(PlayerRestrictionFlag flags)
    {
        ActionBlocks |= flags;
    }

    public void Unrestrict(PlayerRestrictionFlag flags)
    {
        ActionBlocks &= ~flags;
    }

    public bool IsRestricted(PlayerRestrictionFlag flags) => (ActionBlocks & flags) != PlayerRestrictionFlag.None;

    private PlayerAnimator.ControlHandle animatorControl;
    private IdleAnimation idleAnimation;
    private WalkingAnimation walkingAnimation;

    private void Update()
    {
        Input.ReceiveInput();

        Camera.UpdateCamera();

        ToolHandler.UpdateTool();

        Interactor.HandleInteractingItemContainers();
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
            if (Movement.IsMoving && Animator.CurrentAnimation != walkingAnimation) animatorControl.PlayAnimation(walkingAnimation);
            else if (!Movement.IsMoving && Animator.CurrentAnimation != idleAnimation) animatorControl.PlayAnimation(idleAnimation);
        }
    }

    private void FixedUpdate()
    {
        if (!IsRestricted(PlayerRestrictionFlag.DoMovement) && Input.IsInputtingMovement)
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
