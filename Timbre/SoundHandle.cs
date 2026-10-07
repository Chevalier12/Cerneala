namespace Cerneala.Timbre;

public sealed class SoundHandle
{
    private readonly SoundRuntime runtime;
    private SoundPlayback? occupant;

    internal SoundHandle(SoundScope scope)
    {
        Scope = scope;
        runtime = scope.Runtime;
    }

    public SoundScope Scope { get; }

    public SoundPlayback? Current
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
    internal SoundPlayback? OccupantLocked
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
