using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerAnimator
{
    public bool IsControlled => currentHandle != null;
    public PlayerAnimation CurrentAnimation => currentAnimation;

    public void Init(Player player)
    {
        // Setup internal variables
        Array.ForEach(player.GetComponentsInChildren<CustomTag>(), t =>
        {
            if (t.HasTag(CustomTagType.AttachmentSlot)) attachmentSlots[t.name] = t.transform;
            if (t.HasTag(CustomTagType.AnimationTransform)) animationTransforms[t.name] = t.transform;
            if (t.HasTag(CustomTagType.AnimationPoint)) animationPoints[t.name] = t.transform;
        });
    }

    public void UpdateCurrentAnimation()
    {
        currentAnimation?.Update();
    }

    public bool CanTakeControl(int priority = 0)
    {
        if (currentHandle == null) return true;
        else return priority > currentHandle.Priority;
    }

    public ControlHandle TakeControl(int priority = 0)
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
        currentHandle = new ControlHandle(this, priority);
        return currentHandle;
    }

    public Transform GetAttachmentSlot(string name)
    {
        attachmentSlots.TryGetValue(name, out var slot);
        Debug.Assert(slot != null, $"'{name}' not found in attachment slots");
        return slot;
    }

    public Transform GetAnimationTransform(string name)
    {
        animationTransforms.TryGetValue(name, out var transform);
        Debug.Assert(transform != null, $"'{name}' not found in animation transforms");
        return transform;
    }

    public Transform GetAnimationPoint(string name)
    {
        animationPoints.TryGetValue(name, out var point);
        Debug.Assert(point != null, $"'{name}' not found in animation points");
        return point;
    }

    public class ControlHandle
    {
        public Action OnCancelled;
        public int Priority { get; private set; }
        public bool IsActive => animator.currentHandle == this;

        public ControlHandle(PlayerAnimator animator, int priority)
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

    private readonly Dictionary<string, Transform> attachmentSlots = new();
    private readonly Dictionary<string, Transform> animationTransforms = new();
    private readonly Dictionary<string, Transform> animationPoints = new();

    private ControlHandle currentHandle;
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
}
