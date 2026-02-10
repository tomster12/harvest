using UnityEngine;

public class Player : MonoBehaviour
{
    public PlayerPersistent Persistent;
    public PlayerCamera Camera;
    public PlayerInput Input;
    public PlayerMovement Movement;
    public PlayerInteractor Interactor;
    public PlayerTools Tools;
    public PlayerActionHandler Actions;
    public PlayerAnimator Animator;

    public void Init(PlayerPersistent persistent)
    {
        Persistent = persistent;

        Input.Init(this);
        Camera.Init(this);
        Tools.Init(this);
        Interactor.Init(this);
        Movement.Init(this);
        Actions.Init(this);
        Animator.Init(this);

        defaultIdleAnimation = new PlayerIdleAnimation(Animator);
        defaultWalkingAnimation = new PlayerWalkingAnimation(Animator);
    }

    private static int DEFAULT_ANIMATION_PRIORITY = -1;
    private PlayerAnimator.AcControlHandle defaultAcHandle;
    private PlayerIdleAnimation defaultIdleAnimation;
    private PlayerWalkingAnimation defaultWalkingAnimation;

    private void Update()
    {
        Input.ReceiveInput();

        Camera.UpdateCamera();

        Interactor.HandleInteractingContainers();

        Actions.Update();

        TryDefaultAnimations();

        Animator.UpdateCurrentAnimation();
    }

    private void FixedUpdate()
    {
        if (Input.IsInputtingMovement)
        {
            Movement.MoveInDirection(Input.InputMovement);
        }

        Actions.FixedUpdate();

        Movement.FixedUpdateMovement();

        Camera.FollowPlayerPosition();
    }

    private void LateUpdate()
    {
        Movement.LateUpdateMovement();
    }

    private void TryDefaultAnimations()
    {
        // Always try and take control if possible
        if (!Animator.IsControlled)
        {
            defaultAcHandle = Animator.TakeControl(DEFAULT_ANIMATION_PRIORITY);
            defaultAcHandle.OnCancelled = () => defaultAcHandle = null;
        }

        // We have control so idle or walk
        if (defaultAcHandle != null)
        {
            if (Movement.IsMoving && Animator.CurrentAnimation != defaultWalkingAnimation)
            {
                defaultAcHandle.StartAnimation(defaultWalkingAnimation);
            }
            else if (!Movement.IsMoving && Animator.CurrentAnimation != defaultIdleAnimation)
            {
                defaultAcHandle.StartAnimation(defaultIdleAnimation);
            }
        }
    }
}
