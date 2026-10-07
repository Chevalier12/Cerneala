namespace Cerneala.Timbre;

public sealed class SoundScope : IDisposable
{
    private readonly List<SoundPlayback> playbacks = [];
    private readonly List<SoundHandle> handles = [];
    private volatile bool disposed;

    internal SoundScope(SoundRuntime runtime)
    {
        Runtime = runtime;
    }

    public SoundRuntime Runtime { get; }

    public bool IsDisposed => disposed;

    public SoundPlayback Play(SoundClip clip, Action<SoundStartOptions>? configure = null, SoundHandle? handle = null)
    {
        ArgumentNullException.ThrowIfNull(clip);
        if (handle is not null && !ReferenceEquals(handle.Scope, this))
        {
            throw new ArgumentException("The sound handle belongs to another scope.", nameof(handle));
        }

        ThrowIfDisposed();
        SoundStartOptions options = new(clip);
        try
        {
            configure?.Invoke(options);
        }
        finally
        {
            options.Seal();
        }

        return Runtime.Start(this, clip, options, handle);
    }

    public SoundHandle CreateHandle()
    {
        lock (Runtime.Sync)
        {
            ThrowIfDisposed();
            SoundHandle handle = new(this);
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
        foreach (SoundPlayback playback in playbacks.ToArray())
        {
            playback.CancelLocked();
        }

        foreach (SoundHandle handle in handles)
        {
            handle.OccupantLocked = null;
        }

        playbacks.Clear();
        handles.Clear();
        Runtime.RemoveScopeLocked(this);
    }

    internal void AddLocked(SoundPlayback playback) => playbacks.Add(playback);

    internal void RemoveLocked(SoundPlayback playback) => playbacks.Remove(playback);

    internal void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ObjectDisposedException.ThrowIf(Runtime.IsDisposed, Runtime);
    }
}
