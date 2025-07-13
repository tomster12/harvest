using System;

public abstract class PlayerAnimation
{
    public Action OnFinished;

    public PlayerAnimation(PlayerAnimator animator)
    {
        this.animator = animator;
    }

    public virtual void Start()
    { }

    public virtual void Update()
    { }

    public virtual void Cancel()
    { }

    protected PlayerAnimator animator;
};
