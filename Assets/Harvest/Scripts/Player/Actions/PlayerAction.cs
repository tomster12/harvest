using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public abstract class PlayerActionCancelCondition
{
    public abstract bool Evaluate(Player player);
}

public class CancelOnMovementInput : PlayerActionCancelCondition
{
    public override bool Evaluate(Player player) => player.Input.InputMovement.sqrMagnitude > 0.01f;
}

public class CancelOnUnequip : PlayerActionCancelCondition
{
    public override bool Evaluate(Player player) => player.Tools.CurrentTool == null;
}

public class CancelOnPickupItem : PlayerActionCancelCondition
{
    public override bool Evaluate(Player player) => player.Interaction.HeldItemUI != null;
}

public class CancelOnMouseRelease : PlayerActionCancelCondition
{
    public override bool Evaluate(Player player) => player.Input.IsMouseReleased;
}

public abstract class PlayerAction
{
    public event Action<PlayerAction> OnStarted;
    public event Action<PlayerAction> OnCompleted;
    public event Action<PlayerAction> OnCancelled;

    public Player.Restriction PlayerRestrictions { get; private set; } = Player.Restriction.None;
    public List<PlayerActionCancelCondition> CancelConditions { get; private set; } = new();

    public bool IsCancellable { get; private set; } = true;
    public bool IsRunning { get; private set; }
    public virtual bool IsAvailable => true;
    public virtual bool IsRunnable => IsAvailable;


    public PlayerAction ConfigActionPlayerRestriction(Player.Restriction flags)
    {
        PlayerRestrictions |= flags;
        return this;
    }

    public PlayerAction ConfigActionCancelCondition(PlayerActionCancelCondition condition)
    {
        CancelConditions.Add(condition);
        return this;
    }

    public PlayerAction ConfigActionCancellable(bool cancellable)
    {
        IsCancellable = cancellable;
        return this;
    }

    public virtual void Preview() { }

    public async Task Run(CancellationToken ct)
    {
        if (IsRunning) throw new InvalidOperationException("Action already running");
        IsRunning = true;
        OnStarted?.Invoke(this);

        try
        {
            await Start(ct);
            OnCompleted?.Invoke(this);
        }
        catch (OperationCanceledException)
        {
            await Cancel(ct);
            OnCancelled?.Invoke(this);
        }
        catch (Exception e)
        {
            Debug.LogError($"PlayerAction: Error in action {GetType().Name}: {e}");
            throw;
        }
        finally
        {
            await Stop(ct);
            IsRunning = false;
        }
    }

    protected abstract Task Start(CancellationToken ct);

    protected virtual Task Cancel(CancellationToken ct) => Task.CompletedTask;

    protected virtual Task Stop(CancellationToken ct) => Task.CompletedTask;
}
