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
    public override bool Evaluate(Player player) => player.ToolHandler.CurrentTool == null;
}

public class CancelOnPickupItem : PlayerActionCancelCondition
{
    public override bool Evaluate(Player player) => player.Interactor.HeldItemUI != null;
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

    public PlayerBlockFlags PlayerBlockFlags { get; private set; } = PlayerBlockFlags.None;
    public List<PlayerActionCancelCondition> CancelConditions { get; private set; } = new();
    public bool Cancellable { get; private set; } = true;
    public bool IsRunning { get; private set; }

    public PlayerAction(Player player)
    {
        this.player = player;
    }

    public PlayerAction AddPlayerBlock(PlayerBlockFlags flags)
    {
        PlayerBlockFlags |= flags;
        return this;
    }

    public PlayerAction AddCancelCondition(PlayerActionCancelCondition condition)
    {
        CancelConditions.Add(condition);
        return this;
    }

    public PlayerAction SetCancellable(bool cancellable)
    {
        Cancellable = cancellable;
        return this;
    }

    public async Task FullRunAsync(CancellationToken ct)
    {
        if (IsRunning) throw new InvalidOperationException("Action already running");
        IsRunning = true;
        OnStarted?.Invoke(this);

        try
        {
            await RunAsync(ct);
            OnCompleted?.Invoke(this);
        }
        catch (OperationCanceledException)
        {
            await CancelAsync(ct);
            OnCancelled?.Invoke(this);
        }
        catch (Exception e)
        {
            Debug.LogError($"PlayerAction: Error in action {GetType().Name}: {e}");
            throw;
        }
        finally
        {
            await FinishAsync(ct);
            IsRunning = false;
        }
    }

    protected Player player;

    protected abstract Task RunAsync(CancellationToken ct);

    protected virtual Task CancelAsync(CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    protected virtual Task FinishAsync(CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}
