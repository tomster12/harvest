using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerAnimator
{
    public bool IsControlled => currentHandle != null;
    public PlayerAnimation CurrentAnimation => currentAnimation;
    public CustomTagRegistry CustomTags => player.CustomTags;

    public void Init(Player player)
    {
        this.player = player;
    }

    public void UpdateCurrentAnimation()
    {
        currentAnimation?.Update();
    }

    public bool CanControl(int priority = 0)
    {
        if (currentHandle == null) return true;
        else return priority > currentHandle.Priority;
    }

    public AcControlHandle TakeControl(int priority = 0)
    {
        if (currentHandle != null)
        {
            if (priority <= currentHandle.Priority)
            {
                Debug.LogWarning($"Attempted to lock animator with <= priority ({priority}) than current handle ({currentHandle.Priority}).");
                return null;
            }
            currentHandle.Cancel();
        }
        currentHandle = new AcControlHandle(this, priority);
        return currentHandle;
    }

    private Player player;
    private AcControlHandle currentHandle;
    private PlayerAnimation currentAnimation;

    private void StartAnimation(PlayerAnimation animation)
    {
        currentAnimation?.Cancel();
        currentAnimation = animation;
        currentAnimation.OnFinished += OnCurrentAnimationFinished;
        currentAnimation.Start();
    }

    private void CancelAnimation()
    {
        if (currentAnimation == null) return;
        currentAnimation?.Cancel();
        currentAnimation = null;
    }

    private void OnCurrentAnimationFinished()
    {
        currentAnimation.OnFinished -= OnCurrentAnimationFinished;
        currentAnimation = null;
    }

    private void Release()
    {
        Debug.Assert(currentHandle != null, "No animation handle to release");
        currentHandle = null;
        CancelAnimation();
    }

    public class AcControlHandle
    {
        public Action OnCancelled;
        public int Priority { get; private set; }
        public bool IsActive => animator.currentHandle == this;

        public AcControlHandle(PlayerAnimator animator, int priority)
        {
            this.animator = animator;
            Priority = priority;
            animator.currentHandle = this;
        }

        public void Release()
        {
            if (!IsActive)
            {
                Debug.LogWarning("Attempted to release an invalid animation handle.");
                return;
            }
            animator.Release();
        }

        public void Cancel()
        {
            if (!IsActive)
            {
                Debug.LogWarning("Attempted to cancel an inactive animation handle.");
                return;
            }
            OnCancelled?.Invoke();
            animator.Release();
        }

        public void StartAnimation(PlayerAnimation animation)
        {
            if (!IsActive)
            {
                Debug.LogWarning("Attempted to play an animation with an invalid handle.");
                return;
            }
            animator.StartAnimation(animation);
        }

        public void CancelAnimation()
        {
            if (!IsActive)
            {
                Debug.LogWarning("Attempted to cancel an animation with an invalid handle.");
                return;
            }
            animator.CancelAnimation();
        }

        private readonly PlayerAnimator animator;
    }
}
