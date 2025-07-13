using System.Threading;
using System;
using System.Threading.Tasks;

public static class AsyncUtil
{
    public static async Task WaitUntil(Func<bool> condition, CancellationToken ct)
    {
        while (!condition())
        {
            ct.ThrowIfCancellationRequested();
            await Task.Yield();
        }
    }
}
