namespace Cerneala.UI.Relay;

internal sealed class UiRelaySynchronizationContext : SynchronizationContext
{
    private readonly UiRelay relay;

    public UiRelaySynchronizationContext(UiRelay relay)
    {
        this.relay = relay;
    }

    public override void Post(SendOrPostCallback callback, object? state)
    {
        ArgumentNullException.ThrowIfNull(callback);
        relay.Post(() => callback(state));
    }

    public override void Send(SendOrPostCallback callback, object? state)
    {
        ArgumentNullException.ThrowIfNull(callback);
        if (!relay.CheckAccess())
        {
            throw new InvalidOperationException(
                "Synchronous Relay dispatch is only available on the owning UI thread. Use InvokeAsync from another thread.");
        }

        callback(state);
    }

    internal Scope Enter()
    {
        SynchronizationContext? previous = Current;
        if (ReferenceEquals(previous, this))
        {
            return default;
        }

        SetSynchronizationContext(this);
        return new Scope(previous, restoresPrevious: true);
    }

    internal readonly struct Scope : IDisposable
    {
        private readonly SynchronizationContext? previous;
        private readonly bool restoresPrevious;

        internal Scope(SynchronizationContext? previous, bool restoresPrevious)
        {
            this.previous = previous;
            this.restoresPrevious = restoresPrevious;
        }

        public void Dispose()
        {
            if (restoresPrevious)
            {
                SetSynchronizationContext(previous);
            }
        }
    }
}
