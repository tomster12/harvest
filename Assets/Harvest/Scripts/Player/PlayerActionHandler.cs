using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
public class PlayerActionHandler
{
    public bool CanAct => !isRunning || IsCancellable;
    public bool IsRunning => isRunning;
    public bool IsCancellable => currentAction?.Cancellable ?? true;

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

        currentRunActionTask = RunAction(action);
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
        await currentRunActionTask;
        isCancelling = false;
    }

    private Player player;
    private CancellationTokenSource cts;
    private PlayerAction currentAction;
    private Task currentRunActionTask;
    private bool isRunning;
    private bool isCancelling;

    private async Task RunAction(PlayerAction action)
    {
        Debug.Assert(currentAction == null, "Cannot run action task with non null currentAction");

        isRunning = true;
        currentAction = action;
        player.Restrict(action.PlayerRestrictions);

        cts = new CancellationTokenSource();
        await currentAction.FullRun(cts.Token);

        player.Unrestrict(currentAction.PlayerRestrictions);
        cts.Dispose();
        cts = null;
        currentAction = null;
        isRunning = false;
    }
}
