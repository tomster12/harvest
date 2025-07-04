using UnityEngine;

public class WalkingAnimation : PlayerAnimation
{
    private Transform target;
    private Vector3 basePos;
    private float time;

    public override void Start(PlayerAnimator animator)
    {
        target = animator.GetAttachmentSlot(PlayerAttachmentSlot.Hand);
        basePos = target.localPosition;
        time = 0f;
    }

    public override void Update(float dt)
    {
        time += dt;
        float offsetZ = Mathf.Sin(time * 4f) * 0.05f;
        target.localPosition = basePos + new Vector3(0, 0, offsetZ);
    }

    public override void Cancel()
    {
        target.localPosition = basePos;
        OnFinished?.Invoke();
    }
}
