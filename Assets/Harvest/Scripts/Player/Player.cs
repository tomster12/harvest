using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Player : MonoBehaviour
{
    public PlayerPersistent Persistent => persistent;
    public PlayerCamera Camera => camera;
    public PlayerInput Input => input;
    public PlayerMovement Movement => movement;
    public PlayerInteractor Interactor => interactor;
    public PlayerGear Gear => gear;
    public PlayerActionHandler Actions => actions;
    public PlayerAnimator Animator => animator;
    public PlayerBuffs Buffs => buffs;
    public List<BaseStat> BaseStats => baseStats;
    public IStatProvider Stats => stats;
    public CustomTagRegistry CustomTags => customTags;

    public void Init(PlayerPersistent persistent)
    {
        this.persistent = persistent;

        Animator.Init(this);
        Input.Init(this);
        Camera.Init(this);
        Gear.Init(this);
        Interactor.Init(this);
        Movement.Init(this);
        Actions.Init(this);

        defaultIdleAnimation = new PlayerIdleAnimation(Animator);
        defaultWalkingAnimation = new PlayerWalkingAnimation(Animator);

        stats = new(this);
    }

    [Header("References")]
    [SerializeField] private CustomTagRegistry customTags;

    [Header("Components")]
    [SerializeField] private PlayerPersistent persistent;
    [SerializeField] private new PlayerCamera camera;
    [SerializeField] private PlayerInput input;
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerInteractor interactor;
    [SerializeField] private PlayerGear gear;
    [SerializeField] private PlayerActionHandler actions;
    [SerializeField] private PlayerAnimator animator;
    [SerializeField] private PlayerBuffs buffs;

    [Header("Stats")]
    [SerializeField] private List<BaseStat> baseStats;

    private static int DEFAULT_ANIMATION_PRIORITY = -1;
    private PlayerAnimator.AcControlHandle defaultAcHandle;
    private PlayerIdleAnimation defaultIdleAnimation;
    private PlayerWalkingAnimation defaultWalkingAnimation;
    private PlayerStatProvider stats;

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
            Movement.SetMovementInput(Input.InputMovement);
        }

        Actions.FixedUpdateActions();

        Movement.FixedApplyMovement();

        Camera.FixedFollowPlayer();
    }

    private void LateUpdate()
    {
        Movement.LateClearInputs();
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

    private void OnDrawGizmos()
    {
        Interactor.OnDrawGizmos();
    }
}
