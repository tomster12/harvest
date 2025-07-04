using UnityEngine;

public class IdleAnimation : PlayerAnimation
{
    private Transform target;
    private Vector3 basePos;
    private float time;

    public override void Start(PlayerAnimator animator)
    {
        target = animator.GetAnimationTarget(PlayerAnimationTarget.Hand);
        basePos = target.localPosition;
        time = 0f;
    }

    public override void Update(float dt)
    {
        time += dt;
        float offsetY = Mathf.Sin(time * 2f) * 0.05f;
        target.localPosition = basePos + new Vector3(0, offsetY, 0);
    }

    public override void Cancel()
    {
        target.localPosition = basePos;
        OnFinished?.Invoke();
    }
}
