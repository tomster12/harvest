using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
public class PlayerActionHandler
{
    public bool IsActing { get; private set; }

    public void Init(Player player)
    {
        this.player = player;
    }

    public void Register(PlayerAction action)
    {
        if (!registeredActions.Contains(action))
        {
            registeredActions.Add(action);
        }
    }

    public void Unregister(PlayerAction action)
    {
        registeredActions.Remove(action);
    }

    public void Update()
    {
        // Handle cancellation cleanup here from CancelTask()
        if (isCancelling && currentActionRunTask == null)
        {
            isCancelling = false;
        }

        if (!isCancelling)
        {
            // Check cancel conditions for running action
            if (IsActing)
            {
                currentAction.UpdateActive();

                foreach (var condition in currentAction.CancelConditions)
                {
                    if (condition.Evaluate(player))
                    {
                        _ = Cancel();
                        break;
                    }
                }
            }

            // Not runnning so check available actions triggers
            else if (!IsActing)
            {
                PlayerAction runnableAction = null;
                foreach (var action in registeredActions)
                {
                    if (action.IsAvailable)
                    {
                        action.UpdateAvailable();

                        if (action.IsRunnable)
                        {
                            runnableAction ??= action;
                        }
                    }
                }

                // Try run the runnable action on click
                if (runnableAction != null && player.Input.IsMousePressed)
                {
                    player.Input.IsMousePressed = false;
                    currentActionRunTask = Run(runnableAction);
                }
            }
        }
    }

    public void FixedUpdateActions()
    {
        if (!isCancelling && IsActing)
        {
            currentAction.FixedUpdateActive();
        }
    }

    private Player player;
    private bool isCancelling;
    private readonly List<PlayerAction> registeredActions = new();
    private PlayerAction currentAction;
    private Task currentActionRunTask;
    private CancellationTokenSource currentActionCts;

    private async Task Run(PlayerAction action)
    {
        Debug.Assert(currentAction == null);

        IsActing = true;
        currentAction = action;

        currentActionCts = new CancellationTokenSource();
        await currentAction.Run(currentActionCts.Token);

        currentActionCts.Dispose();
        currentActionCts = null;
        currentAction = null;
        IsActing = false;
    }

    private async Task Cancel()
    {
        Debug.Assert(currentAction != null);
        Debug.Assert(!isCancelling);

        isCancelling = true;
        currentActionCts?.Cancel();
        try
        {
            await currentActionRunTask;
        }
        finally
        {
            // We reset isCancelling in Update()
            currentActionRunTask = null;
        }
    }
}
