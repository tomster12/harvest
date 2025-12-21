using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
public class PlayerActions
{
    public void Init(Player player)
    {
        this.player = player;
    }

    public void Register(PlayerAction action)
    {
        if (!registered.Contains(action))
        {
            registered.Add(action);
        }
    }

    public void Unregister(PlayerAction action)
    {
        registered.Remove(action);
    }

    public void Update()
    {
        // Only catch cancelled actions here to prevent race conditions
        if (isCancelling && currentActionRunTask == null)
        {
            isCancelling = false;
        }

        // Check cancel conditions for current running action
        if (isRunning && !isCancelling)
        {
            foreach (var condition in currentAction.CancelConditions)
            {
                if (condition.Evaluate(player))
                {
                    _ = CancelTask();
                    break;
                }
            }
        }

        // Preview and trigger any actions
        else if (!isRunning && !isCancelling)
        {
            PlayerAction runnableAction = null;
            foreach (var action in registered)
            {
                if (action.IsAvailable)
                {
                    action.Preview();
                    if (action.IsRunnable) runnableAction ??= action;
                }
            }

            if (runnableAction != null && player.Input.IsMousePressed)
            {
                player.Input.IsMousePressed = false;
                Debug.Assert(!isCancelling, "Cannot start a new action while cancelling an existing one");

                if (isRunning)
                {
                    if (!currentAction.IsCancellable)
                    {
                        Debug.LogWarning($"Action '{currentAction.GetType().Name}' is currently running and cannot be cancelled.");
                        return;
                    }
                    _ = CancelTask();
                }

                currentActionRunTask = RunTask(runnableAction);
            }
        }
    }

    private Player player;
    private readonly List<PlayerAction> registered = new();
    private PlayerAction currentAction;
    private Task currentActionRunTask;
    private CancellationTokenSource currentActionCts;
    private bool isRunning;
    private bool isCancelling;

    private async Task RunTask(PlayerAction action)
    {
        Debug.Assert(currentAction == null, "Cannot run action task with non null currentAction");

        isRunning = true;
        currentAction = action;
        player.Restrict(action.PlayerRestrictions);

        currentActionCts = new CancellationTokenSource();
        await currentAction.Run(currentActionCts.Token);

        player.Unrestrict(currentAction.PlayerRestrictions);
        currentActionCts.Dispose();
        currentActionCts = null;
        currentAction = null;
        isRunning = false;
    }

    private async Task CancelTask()
    {
        Debug.Assert(currentAction != null, "Cannot cancel action when currentAction is null");
        Debug.Assert(!isCancelling, "Already cancelling an action");

        isCancelling = true;
        currentActionCts?.Cancel();
        try
        {
            await currentActionRunTask;
        }
        finally
        {
            currentActionRunTask = null;
            // We will reset isCancelling in Update()
        }
    }
}
