using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
public class PlayerActionHandler
{
    public bool IsBusy => currentAction != null;

    public void Init(Player player)
    {
        this.player = player;
    }

    public async void StartAction(PlayerAction action)
    {
        // Try to cancel and wait for any current action
        if (currentAction != null)
        {
            if (!currentAction.Cancellable)
            {
                Debug.LogWarning($"Action '{currentAction.GetType().Name}' is currently running and cannot be cancelled.");
                return;
            }
            CancelCurrentAction();
            if (currentActionTask != null) await currentActionTask;
        }

        // Setup and start the new action task
        cts = new CancellationTokenSource();
        currentAction = action;
        player.Block(action.PlayerBlockFlags);
        currentActionTask = RunActionTask();
    }

    public void UpdateActions()
    {
        // Check cancel conditions for the current action
        if (currentAction != null)
        {
            foreach (var condition in currentAction.CancelConditions)
            {
                if (condition.Evaluate(player))
                {
                    CancelCurrentAction();
                    break;
                }
            }
        }
    }

    public void CancelCurrentAction()
    {
        cts?.Cancel();
    }

    private Player player;
    private CancellationTokenSource cts;
    private PlayerAction currentAction;
    private Task currentActionTask;

    private async Task RunActionTask()
    {
        // Start, wait for cancellation, and cleanup an action
        try
        {
            currentActionTask = currentAction.RunAsync(player, cts.Token);
            await currentActionTask;
        }
        catch (OperationCanceledException)
        {
            Debug.Log($"Action '{currentAction.GetType().Name}' was cancelled.");
        }
        finally
        {
            Debug.Assert(currentAction != null, "Cleanup called but no current action is set.");
            player.Unblock(currentAction.PlayerBlockFlags);
            currentAction = null;
            cts.Dispose();
            cts = null;
        }
    }
}
