using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public enum PlayerAttachmentSlot
{ Hand }

public enum PlayerAnimationTarget
{ Hand }

[Serializable]
public class PlayerAnimator
{
    public void Init(Player player)
    {
        this.player = player;
        idleAnimation = new IdleAnimation();
        walkingAnimation = new WalkingAnimation();
    }

    public void UpdateAnimations()
    {
        // If we are not doing some custom animation then just play idle or walking animation based on movement state
        if (currentAnimation == null || currentAnimation == idleAnimation || currentAnimation == walkingAnimation)
        {
            if (player.Movement.IsMoving && currentAnimation != walkingAnimation) PlayAnimation(walkingAnimation);
            else if (!player.Movement.IsMoving && currentAnimation != idleAnimation) PlayAnimation(idleAnimation);
        }

        currentAnimation?.Update(Time.deltaTime);
    }

    public void PlayAnimation(PlayerAnimation animation)
    {
        currentAnimation?.Cancel();
        currentAnimation = animation;
        currentAnimation.OnFinished += OnCurrentAnimationFinished;
        currentAnimation.Start(this);
    }

    public void CancelAnimation()
    {
        currentAnimation?.Cancel();
        currentAnimation = null;
    }

    public Transform GetAttachmentSlot(PlayerAttachmentSlot slot) => slot switch
    {
        PlayerAttachmentSlot.Hand => handTransform,
        _ => throw new ArgumentOutOfRangeException()
    };

    public Transform GetAnimationTarget(PlayerAnimationTarget target) => target switch
    {
        PlayerAnimationTarget.Hand => handTransform,
        _ => throw new ArgumentOutOfRangeException()
    };

    [Header("References")]
    [SerializeField] private Transform handTransform;

    private Player player;
    private PlayerAnimation currentAnimation;
    private IdleAnimation idleAnimation;
    private WalkingAnimation walkingAnimation;

    private void OnCurrentAnimationFinished()
    {
        currentAnimation.OnFinished -= OnCurrentAnimationFinished;
        currentAnimation = null;
    }
}
