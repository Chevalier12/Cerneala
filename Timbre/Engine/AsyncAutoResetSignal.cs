using System.Threading.Tasks.Sources;

namespace Cerneala.Timbre.Engine;

// Single-waiter auto-reset signal. Set() is allocation-free and never blocks,
// so the mixer thread can wake a worker from the realtime path: the awaiting
// async method's own state machine is queued as the continuation.
internal sealed class AsyncAutoResetSignal : IValueTaskSource
{
    private const int Idle = 0;
    private const int Waiting = 1;
    private const int Signaled = 2;

    private ManualResetValueTaskSourceCore<bool> core = new() { RunContinuationsAsynchronously = true };
    private int state;

    // Only one caller may wait at a time.
    internal ValueTask WaitAsync()
    {
        if (Interlocked.CompareExchange(ref state, Idle, Signaled) == Signaled)
        {
            return ValueTask.CompletedTask;
        }

        core.Reset();
        if (Interlocked.CompareExchange(ref state, Waiting, Idle) != Idle)
        {
            // Set() raced in between: consume it without suspending.
            Volatile.Write(ref state, Idle);
            return ValueTask.CompletedTask;
        }

        return new ValueTask(this, core.Version);
    }

    internal void Set()
    {
        while (true)
        {
            int current = Volatile.Read(ref state);
            if (current == Signaled)
            {
                return;
            }

            if (current == Waiting)
            {
                if (Interlocked.CompareExchange(ref state, Idle, Waiting) == Waiting)
                {
                    core.SetResult(true);
                    return;
                }
            }
            else if (Interlocked.CompareExchange(ref state, Signaled, Idle) == Idle)
            {
                return;
            }
        }
    }

    void IValueTaskSource.GetResult(short token) => core.GetResult(token);

    ValueTaskSourceStatus IValueTaskSource.GetStatus(short token) => core.GetStatus(token);

    void IValueTaskSource.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) =>
        core.OnCompleted(continuation, state, token, flags);
}
