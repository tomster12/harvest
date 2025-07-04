using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class ChopTreeAction : PlayerAction
{
    public ChopTreeAction(RaycastHit hit, Vector3 chopFromPos)
    {
        this.hit = hit;
        this.chopFromPos = chopFromPos;

        AddPlayerBlock(PlayerBlockFlags.Movement);
        AddCancelCondition(new CancelOnMovementInput());
        AddCancelCondition(new CancelOnMouseRelease());
        SetCancellable(true);
    }

    public override async Task RunAsync(CancellationToken ct, Player player)
    {
        player.Movement.MoveTowardsPosition(chopFromPos, 0.04f);
        while (!player.Movement.HasReachedTarget)
        {
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
        }

        player.Movement.FaceTowardsPoint(hit.point);
        while (!player.Movement.IsFacingTarget)
        {
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
        }
    }

    private readonly RaycastHit hit;
    private readonly Vector3 chopFromPos;
}
