using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static TreeEditor.TreeGroup;
using Unity.VisualScripting.Antlr3.Runtime;

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

    public async Task RunAsyncWrapper(CancellationToken ct, Player player)
    {
        if (IsRunning) throw new InvalidOperationException("Action already running");
        IsRunning = true;
        OnStarted?.Invoke(this);

        try
        {
            await RunAsync(ct, player);
            OnCompleted?.Invoke(this);
        }
        catch (OperationCanceledException)
        {
            // Just consume and ignore cancellation exceptions
            OnCancelled?.Invoke(this);
        }
        finally
        {
            await CancelAsync(ct, player);
            IsRunning = false;
        }
    }

    public abstract Task RunAsync(CancellationToken ct, Player player);

    public virtual Task CancelAsync(CancellationToken ct, Player player)
    {
        return Task.CompletedTask;
    }
}
