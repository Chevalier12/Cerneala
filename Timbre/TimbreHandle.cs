namespace Cerneala.Timbre;

public sealed class TimbreHandle
{
    private readonly TimbreRuntime runtime;
    private TimbrePlayback? occupant;

    internal TimbreHandle(TimbreScope scope)
    {
        Scope = scope;
        runtime = scope.Runtime;
    }

    public TimbreScope Scope { get; }

    public TimbrePlayback? Current
    {
        get
        {
            lock (runtime.Sync)
            {
                return occupant is { IsTerminal: false } ? occupant : null;
            }
        }
    }

    // Caller holds runtime.Sync.
    internal TimbrePlayback? OccupantLocked
    {
        get => occupant;
        set => occupant = value;
    }

    public void Cancel()
    {
        lock (runtime.Sync)
        {
            occupant?.CancelLocked();
        }

        runtime.SignalMixer();
    }
}
