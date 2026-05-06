using UnityEngine;

public class PlayerWalkingAnimation : PlayerAnimation
{
    public PlayerWalkingAnimation(PlayerAnimator animator) : base(animator)
    {
        atLeftHand = animator.CustomTags.Get(CustomTagType.AnimationTransform, "Left Hand");
        atRightHand = animator.CustomTags.Get(CustomTagType.AnimationTransform, "Right Hand");
        apLeftHandBase = animator.CustomTags.Get(CustomTagType.AnimationPoint, "AP - Left Hand - Base");
        apRightHandeBase = animator.CustomTags.Get(CustomTagType.AnimationPoint, "AP - Right Hand - Base");
    }

    public override void Start()
    {
        atLeftHand.localRotation = Quaternion.identity;
        atRightHand.localRotation = Quaternion.identity;
        baseTime = Time.time;
    }

    public override void Update()
    {
        float t = Time.time - baseTime;
        float offsetZ = Mathf.Sin(t * 4f) * 0.05f;

        Vector3 leftTarget = apLeftHandBase.localPosition + new Vector3(0, 0, offsetZ);
        if (t < TRANSITION_TIME) atLeftHand.localPosition = Vector3.Lerp(atLeftHand.localPosition, leftTarget, Mathf.Clamp01(t / TRANSITION_TIME));
        else atLeftHand.localPosition = leftTarget;

        Vector3 rightTarget = apRightHandeBase.localPosition + new Vector3(0, 0, -offsetZ);
        if (t < TRANSITION_TIME) atRightHand.localPosition = Vector3.Lerp(atRightHand.localPosition, rightTarget, Mathf.Clamp01(t / TRANSITION_TIME));
        else atRightHand.localPosition = rightTarget;
    }

    public override void Cancel()
    {
        OnFinished?.Invoke();
    }

    private static readonly float TRANSITION_TIME = 0.5f;

    private readonly Transform atLeftHand;
    private readonly Transform atRightHand;
    private readonly Transform apLeftHandBase;
    private readonly Transform apRightHandeBase;
    private float baseTime;
}
