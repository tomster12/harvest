using UnityEngine;

public class PlayerIdleAnimation : PlayerAnimation
{
    public PlayerIdleAnimation(PlayerAnimator animator) : base(animator)
    {
        leftHandTransform = animator.GetAnimationTransform("Left Hand");
        rightHandTransform = animator.GetAnimationTransform("Right Hand");
        leftBaseTransform = animator.GetAnimationPoint("Left Hand - Base Point");
        rightBaseTransform = animator.GetAnimationPoint("Right Hand - Base Point");
    }

    public override void Start()
    {
        leftHandTransform.localRotation = Quaternion.identity;
        rightHandTransform.localRotation = Quaternion.identity;
        baseTime = Time.time;
    }

    public override void Update()
    {
        float t = Time.time - baseTime;
        float offsetY = Mathf.Sin(t * 2f) * 0.05f;

        Vector3 leftTarget = leftBaseTransform.localPosition + new Vector3(0, offsetY, 0);
        if (t < TRANSITION_TIME) leftHandTransform.localPosition = Vector3.Lerp(leftHandTransform.localPosition, leftTarget, Mathf.Clamp01(t / TRANSITION_TIME));
        else leftHandTransform.localPosition = leftTarget;

        Vector3 rightTarget = rightBaseTransform.localPosition + new Vector3(0, offsetY, 0);
        if (t < TRANSITION_TIME) rightHandTransform.localPosition = Vector3.Lerp(rightHandTransform.localPosition, rightTarget, Mathf.Clamp01(t / TRANSITION_TIME));
        else rightHandTransform.localPosition = rightTarget;
    }

    public override void Cancel()
    {
        OnFinished?.Invoke();
    }

    private static readonly float TRANSITION_TIME = 0.5f;

    private readonly Transform leftHandTransform;
    private readonly Transform rightHandTransform;
    private readonly Transform leftBaseTransform;
    private readonly Transform rightBaseTransform;
    private float baseTime;
}
