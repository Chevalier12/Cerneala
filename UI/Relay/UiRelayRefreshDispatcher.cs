namespace Cerneala.UI.Relay;

internal sealed class UiRelayRefreshDispatcher
{
    private readonly Func<UiRelay?> resolveRelay;
    private readonly Action refresh;
    private readonly string description;
    private UiRelay? relay;
    private int ownerThreadId;
    private int generation;
    private int active;
    private int enqueuedGeneration;
    private long requestedVersion;
    private long processedVersion;

    public UiRelayRefreshDispatcher(Func<UiRelay?> resolveRelay, Action refresh, string description)
    {
        this.resolveRelay = resolveRelay ?? throw new ArgumentNullException(nameof(resolveRelay));
        this.refresh = refresh ?? throw new ArgumentNullException(nameof(refresh));
        this.description = string.IsNullOrWhiteSpace(description) ? "reactive refresh" : description;
    }

    public Func<bool> Activate()
    {
        relay = resolveRelay();
        ownerThreadId = Environment.CurrentManagedThreadId;
        relay?.VerifyAccess();
        Interlocked.Exchange(ref active, 1);
        int currentGeneration = Interlocked.Increment(ref generation);
        Interlocked.Exchange(ref enqueuedGeneration, 0);
        Volatile.Write(ref processedVersion, Volatile.Read(ref requestedVersion));
        return () => ShouldProcessSynchronously(currentGeneration);
    }

    public void Deactivate()
    {
        Interlocked.Exchange(ref active, 0);
        Interlocked.Increment(ref generation);
        relay = null;
    }

    private bool ShouldProcessSynchronously(int callbackGeneration)
    {
        if (!IsCurrent(callbackGeneration))
        {
            return false;
        }

        UiRelay? currentRelay = relay;
        if (currentRelay?.CheckAccess() == true ||
            (currentRelay is null && Environment.CurrentManagedThreadId == ownerThreadId))
        {
            return true;
        }

        if (currentRelay is null)
        {
            throw new InvalidOperationException(
                $"'{description}' received a source notification on thread {Environment.CurrentManagedThreadId}, " +
                $"but no UI Relay is available for owner thread {ownerThreadId}. Attach the target or supply a Relay, " +
                "then use Relay.Post or await Relay.InvokeAsync.");
        }

        Interlocked.Increment(ref requestedVersion);
        EnsureEnqueued(currentRelay, callbackGeneration);
        return false;
    }

    private void EnsureEnqueued(UiRelay currentRelay, int callbackGeneration)
    {
        while (IsCurrent(callbackGeneration))
        {
            int enqueued = Volatile.Read(ref enqueuedGeneration);
            if (enqueued == callbackGeneration)
            {
                return;
            }

            if (enqueued != 0)
            {
                _ = Interlocked.CompareExchange(ref enqueuedGeneration, 0, enqueued);
                continue;
            }

            if (Interlocked.CompareExchange(ref enqueuedGeneration, callbackGeneration, 0) != 0)
            {
                continue;
            }

            if (!IsCurrent(callbackGeneration))
            {
                _ = Interlocked.CompareExchange(ref enqueuedGeneration, 0, callbackGeneration);
                return;
            }

            WeakReference<UiRelayRefreshDispatcher> weak = new(this);
            currentRelay.Post(() =>
            {
                if (weak.TryGetTarget(out UiRelayRefreshDispatcher? dispatcher))
                {
                    dispatcher.ProcessPending(callbackGeneration);
                }
            });
            return;
        }
    }

    private void ProcessPending(int callbackGeneration)
    {
        if (!IsCurrent(callbackGeneration))
        {
            _ = Interlocked.CompareExchange(ref enqueuedGeneration, 0, callbackGeneration);
            return;
        }

        relay!.VerifyAccess();
        _ = Interlocked.CompareExchange(ref enqueuedGeneration, 0, callbackGeneration);
        long targetVersion = Volatile.Read(ref requestedVersion);
        refresh();
        Volatile.Write(ref processedVersion, targetVersion);
        if (Volatile.Read(ref requestedVersion) != Volatile.Read(ref processedVersion))
        {
            EnsureEnqueued(relay, callbackGeneration);
        }
    }

    private bool IsCurrent(int callbackGeneration)
    {
        return Volatile.Read(ref active) != 0 && Volatile.Read(ref generation) == callbackGeneration;
    }
}
