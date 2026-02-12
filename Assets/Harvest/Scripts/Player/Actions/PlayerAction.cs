using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public abstract class PlayerActionCancelCondition
{
    public bool Active { get; set; } = true;
    public abstract bool Evaluate(Player player);
}

public class CancelOnMovementInput : PlayerActionCancelCondition
{
    public override bool Evaluate(Player player) => Active && player.Input.InputMovement.sqrMagnitude > 0.01f;
}

public class CancelOnUnequip : PlayerActionCancelCondition
{
    public override bool Evaluate(Player player) => Active && player.Tools.CurrentTool == null;
}

public class CancelOnPickupItem : PlayerActionCancelCondition
{
    public override bool Evaluate(Player player) => Active && player.Interactor.HeldItemUI != null;
}

public class CancelOnMouseRelease : PlayerActionCancelCondition
{
    public override bool Evaluate(Player player) => Active && player.Input.IsMouseReleased;
}

[Serializable]
public abstract class PlayerAction
{
    public event Action<PlayerAction> OnStarted;
    public event Action<PlayerAction> OnCompleted;
    public event Action<PlayerAction> OnCancelled;

    public bool IsRunning { get; private set; }
    public virtual bool IsAvailable => true;
    public virtual bool IsRunnable => IsAvailable;

    public List<PlayerActionCancelCondition> CancelConditions { get; private set; } = new();

    public PlayerAction Configure(List<PlayerActionCancelCondition> cancelConditions)
    {
        CancelConditions.AddRange(cancelConditions);
        return this;
    }

    public virtual void UpdateAvailable() { }

    public virtual void UpdateActive() { }

    public virtual void FixedUpdateActive() { }

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
