using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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

public abstract class PlayerAction
{
    public PlayerBlockFlags PlayerBlockFlags { get; private set; } = PlayerBlockFlags.None;
    public List<PlayerActionCancelCondition> CancelConditions { get; private set; } = new();
    public bool Cancellable { get; private set; } = true;

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

    public abstract Task RunAsync(Player player, CancellationToken token);
}
