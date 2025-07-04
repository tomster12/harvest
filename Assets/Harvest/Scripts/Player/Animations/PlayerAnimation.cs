using System;

public abstract class PlayerAnimation
{
    public Action OnFinished;

    public virtual void Start(PlayerAnimator animator)
    { }

    public virtual void Update(float dt)
    { }

    public virtual void Cancel()
    { }
};
