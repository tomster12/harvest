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
        Debug.Assert(!isCancelling, "Cannot start a new action while cancelling an existing one");

        if (isRunning)
        {
            if (!currentAction.Cancellable)
            {
                Debug.LogWarning($"Action '{currentAction.GetType().Name}' is currently running and cannot be cancelled.");
                return;
            }
            await CancelCurrentAction();
        }

        currentActionTask = RunActionTask(action);
    }

    public void UpdateActions()
    {
        if (isRunning && !isCancelling)
        {
            foreach (var condition in currentAction.CancelConditions)
            {
                if (condition.Evaluate(player))
                {
                    _ = CancelCurrentAction();
                    break;
                }
            }
        }
    }

    public async Task CancelCurrentAction()
    {
        Debug.Assert(currentAction != null, "Cannot cancel action when currentAction is null");
        Debug.Assert(!isCancelling, "Already cancelling an action");

        isCancelling = true;
        cts?.Cancel();
        await currentActionTask;
        isCancelling = false;
    }

    private Player player;
    private CancellationTokenSource cts;
    private PlayerAction currentAction;
    private Task currentActionTask;
    private bool isRunning;
    private bool isCancelling;

    private async Task RunActionTask(PlayerAction action)
    {
        Debug.Assert(currentAction == null, "Cannot run action task with null currentAction");

        cts = new CancellationTokenSource();
        currentAction = action;
        player.Block(action.PlayerBlockFlags);

        // Cancellations are caught and handled inside this wrapper so no errors should bubble up
        isRunning = true;
        currentActionTask = currentAction.RunAsyncWrapper(cts.Token, player);
        await currentActionTask;

        player.Unblock(currentAction.PlayerBlockFlags);
        currentAction = null;
        currentActionTask = null;
        cts.Dispose();
        cts = null;
        isRunning = false;
    }
}
