namespace Cerneala.UI.Servo;

// Pulses once per rendered frame of the Servo context (Window.FrameRendered or UiHost.FrameUpdated)
// and fails the waiter when the context goes away (Window closed or UiHost root replaced).
// Pulses do not accumulate: a waiter that missed several frames wakes once.
internal sealed class ServoFrameSignal : IDisposable
{
    private readonly object sync = new();
    private readonly SemaphoreSlim pulse = new(0, 1);
    private readonly IDisposable subscription;
    private Exception? invalidation;
    private bool disposed;

    internal ServoFrameSignal(ServoContext context)
    {
        subscription = context.SubscribeToFrames(Signal, Invalidate);
    }

    internal async Task WaitAsync(CancellationToken cancellationToken)
    {
        ThrowIfInvalidated();
        await pulse.WaitAsync(cancellationToken).ConfigureAwait(false);
        ThrowIfInvalidated();
    }

    internal void ThrowIfInvalidated()
    {
        Exception? failure;
        lock (sync)
        {
            failure = invalidation;
        }

        if (failure is not null)
        {
            throw failure;
        }
    }

    public void Dispose()
    {
        lock (sync)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
        }

        subscription.Dispose();
        pulse.Dispose();
    }

    private void Signal()
    {
        lock (sync)
        {
            if (!disposed && pulse.CurrentCount == 0)
            {
                pulse.Release();
            }
        }
    }

    private void Invalidate(Exception exception)
    {
        lock (sync)
        {
            if (disposed)
            {
                return;
            }

            invalidation ??= exception;
            if (pulse.CurrentCount == 0)
            {
                pulse.Release();
            }
        }
    }
}
