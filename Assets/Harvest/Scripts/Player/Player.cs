using System;
using UnityEngine;

[Flags]
public enum PlayerBlockFlags
{
    None = 0,
    Movement = 1 << 0,
    Inventory = 1 << 1,
    Input = 1 << 2
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

    public PlayerBlockFlags BlockFlags = PlayerBlockFlags.None;

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
    }

    public void Block(PlayerBlockFlags flags)
    {
        BlockFlags |= flags;
    }

    public void Unblock(PlayerBlockFlags flags)
    {
        BlockFlags &= ~flags;
    }

    public bool IsBlocked(PlayerBlockFlags flags) => (BlockFlags & flags) != PlayerBlockFlags.None;

    private void Update()
    {
        Input.ReceiveInput();
        Camera.UpdateCamera();
        ToolHandler.UpdateTool();
        Interactor.HandleInteractingItemContainers();
        Interactor.HandleInteractingWorld();
        Actions.UpdateActions();
        Animator.UpdateAnimations();
    }

    private void FixedUpdate()
    {
        if (!IsBlocked(PlayerBlockFlags.Movement) && Input.IsInputtingMovement)
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
