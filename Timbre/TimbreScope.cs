namespace Cerneala.Timbre;

public sealed class TimbreScope : IDisposable
{
    private readonly List<TimbrePlayback> playbacks = [];
    private readonly List<TimbreHandle> handles = [];
    private volatile bool disposed;

    internal TimbreScope(TimbreRuntime runtime)
    {
        Runtime = runtime;
    }

    public TimbreRuntime Runtime { get; }

    public bool IsDisposed => disposed;

    public TimbrePlayback Play(TimbreSound sound, Action<TimbreStartOptions>? configure = null, TimbreHandle? handle = null)
    {
        ArgumentNullException.ThrowIfNull(sound);
        if (handle is not null && !ReferenceEquals(handle.Scope, this))
        {
            throw new ArgumentException("The sound handle belongs to another scope.", nameof(handle));
        }

        ThrowIfDisposed();
        TimbreStartOptions options = new(sound);
        try
        {
            configure?.Invoke(options);
        }
        finally
        {
            options.Seal();
        }

        return Runtime.Start(this, sound, options, handle);
    }

    public TimbreHandle CreateHandle()
    {
        lock (Runtime.Sync)
        {
            ThrowIfDisposed();
            TimbreHandle handle = new(this);
            handles.Add(handle);
            return handle;
        }
    }

    public void Dispose()
    {
        lock (Runtime.Sync)
        {
            DisposeLocked();
        }

        Runtime.SignalMixer();
    }

    // Caller holds runtime.Sync.
    internal void DisposeLocked()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        foreach (TimbrePlayback playback in playbacks.ToArray())
        {
            playback.CancelLocked();
        }

        foreach (TimbreHandle handle in handles)
        {
            handle.OccupantLocked = null;
        }

        playbacks.Clear();
        handles.Clear();
        Runtime.RemoveScopeLocked(this);
    }

    internal void AddLocked(TimbrePlayback playback) => playbacks.Add(playback);

    internal void RemoveLocked(TimbrePlayback playback) => playbacks.Remove(playback);

    internal void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ObjectDisposedException.ThrowIf(Runtime.IsDisposed, Runtime);
    }
}
