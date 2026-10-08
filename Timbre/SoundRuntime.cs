using Cerneala.Timbre.Catalog;
using Cerneala.Timbre.Engine;

namespace Cerneala.Timbre;

public sealed partial class SoundRuntime : IDisposable
{
    public const int SampleRate = TimbreCatalog.SampleRate;

    public const int ChannelCount = TimbreCatalog.ChannelCount;

    private readonly ISoundOutput? output;
    private readonly int maxVoices;
    private readonly long autoPreloadMaxBytes;
    private readonly long maxPreloadBytes;
    private readonly SoundMemoryPool memory;
    private readonly long tailCapFrames;
    private readonly string baseDirectory;
    private readonly SoundPayloadCache cache;
    private readonly OutputClient client;
    private readonly PumpDispatcher pumpDispatcher = new();
    private readonly AutoResetEvent wake = new(initialState: false);
    private readonly List<SoundPlayback> live = [];
    private readonly List<SoundPlayback> adopted = [];
    private readonly List<SoundScope> scopes = [];
    private readonly List<TaskCompletionSource> syncRequests = [];
    private Thread? mixerThread;
    private volatile bool disposed;
    private long streamingBytes;

    public SoundRuntime(SoundRuntimeOptions? options = null)
    {
        options ??= new SoundRuntimeOptions();
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.MaxVoices, nameof(options.MaxVoices));
        ArgumentOutOfRangeException.ThrowIfNegative(options.AutoPreloadMaxBytes, nameof(options.AutoPreloadMaxBytes));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.MaxPreloadBytes, nameof(options.MaxPreloadBytes));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.MaxCacheBytes, nameof(options.MaxCacheBytes));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.StreamingMemoryLimit, nameof(options.StreamingMemoryLimit));
        if (options.DelayTailCap < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(options.DelayTailCap), options.DelayTailCap, "The delay tail cap cannot be negative.");
        }

        output = options.Output;
        maxVoices = options.MaxVoices;
        autoPreloadMaxBytes = options.AutoPreloadMaxBytes;
        maxPreloadBytes = options.MaxPreloadBytes;
        memory = new SoundMemoryPool(options.StreamingMemoryLimit);
        tailCapFrames = SoundTime.ToFrames(options.DelayTailCap);
        baseDirectory = options.BaseDirectory ?? AppContext.BaseDirectory;
        cache = new SoundPayloadCache(options.MaxCacheBytes);
        client = new OutputClient(this);
    }

    public bool IsDisposed => disposed;

    internal object Sync { get; } = new();

    public SoundScope CreateScope()
    {
        lock (Sync)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            SoundScope scope = new(this);
            scopes.Add(scope);
            return scope;
        }
    }

    public Task PrepareAsync(SoundClip clip, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(clip);
        ObjectDisposedException.ThrowIf(disposed, this);
        return Task.Run(() => PrepareCoreAsync(clip, cancellationToken), cancellationToken);
    }

    public void Dispose()
    {
        Thread? thread;
        lock (Sync)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            foreach (SoundPlayback playback in live.ToArray())
            {
                playback.CancelLocked();
            }

            foreach (SoundScope scope in scopes.ToArray())
            {
                scope.DisposeLocked();
            }

            thread = mixerThread;
        }

        wake.Set();
        if (thread is not null && thread != Thread.CurrentThread)
        {
            thread.Join();
        }

        // After the mixer has posted every stop request.
        pumpDispatcher.Dispose();
        cache.Clear();
    }

    internal SoundRuntimeDiagnostics GetDiagnostics()
    {
        lock (Sync)
        {
            return new SoundRuntimeDiagnostics(
                playbacksStarted,
                playbacksCompleted,
                playbacksCanceled,
                playbacksFailed,
                pausesApplied,
                resumesApplied,
                seeksRequested,
                seeksCompleted,
                seeksSuperseded,
                Interlocked.Read(ref loopWraps),
                Interlocked.Read(ref blocksMixed),
                Interlocked.Read(ref framesSubmitted),
                Interlocked.Read(ref framesConsumed),
                Interlocked.Read(ref underrunFrames),
                Interlocked.Read(ref clippedSamples),
                parameterPublications,
                animatedPublications,
                motionSamplesRejected,
                live.Count,
                Volatile.Read(ref liveReaders),
                Volatile.Read(ref liveSourcePumps),
                Volatile.Read(ref outputOpenCount),
                outputOpen,
                cache.Bytes,
                cache.Count,
                Interlocked.Read(ref streamingBytes),
                memory.Reserved - Interlocked.Read(ref streamingBytes),
                Interlocked.Read(ref dspStateBytes),
                scopes.Count,
                Volatile.Read(ref pendingLoads));
        }
    }

    // Caller holds Sync; a disposed scope no longer belongs to the runtime.
    internal void RemoveScopeLocked(SoundScope scope) => scopes.Remove(scope);

    // Optional mixer-thread instrumentation for the cost gate; null in normal use.
    internal ISoundBlockObserver? BlockObserver { get; set; }

    // Optional test instrumentation: every playback accepted by Start, on the
    // starting thread after it is published; null in normal use.
    internal Action<SoundPlayback>? PlaybackAccepted { get; set; }

    // Completes once the mixer has applied everything published before the
    // call and reached a point where it would wait for new work.
    internal Task SyncAsync()
    {
        TaskCompletionSource request = new(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (Sync)
        {
            if (disposed || mixerThread is null)
            {
                return Task.CompletedTask;
            }

            syncRequests.Add(request);
        }

        wake.Set();
        return request.Task;
    }

    internal SoundPlayback Start(SoundScope scope, SoundClip clip, SoundStartOptions options, SoundHandle? handle)
    {
        object? key = TryGetCacheKey(clip.Source);
        SoundPlayback playback;
        bool needsLoad = false;
        lock (Sync)
        {
            scope.ThrowIfDisposed();
            SoundPlayback? previous = handle?.OccupantLocked;
            bool replacing = previous is { IsTerminal: false };
            if (live.Count - (replacing ? 1 : 0) + 1 > maxVoices)
            {
                throw new SoundException(
                    SoundErrorKind.VoiceLimitExceeded,
                    $"The sound runtime already has the maximum of {maxVoices} active playbacks.");
            }

            playback = new SoundPlayback(this, scope, clip, options);
            if (playback.Render.Chain is { } chain)
            {
                Interlocked.Add(ref dspStateBytes, chain.StateBytes);
            }

            live.Add(playback);
            scope.AddLocked(playback);
            playbacksStarted++;
            if (handle is not null)
            {
                handle.OccupantLocked = playback;
            }

            if (replacing)
            {
                previous!.CancelLocked();
            }

            if (clip.Loading != SoundLoading.Streaming && key is not null && cache.TryAcquire(key, out SoundPayloadCache.Entry? entry))
            {
                playback.TryAttachFeedLocked(new PreloadedFeed(cache, entry, playback.Loop), fromLoader: false);
            }
            else
            {
                playback.BeginLoadLocked();
                needsLoad = true;
            }

            adopted.Add(playback);
            EnsureMixerLocked();
        }

        if (needsLoad)
        {
            Interlocked.Increment(ref pendingLoads);
            _ = Task.Run(() => LoadAsync(playback, key));
        }

        SignalMixer();
        PlaybackAccepted?.Invoke(playback);
        return playback;
    }

    internal void SignalMixer() => wake.Set();

    // Caller holds Sync.
    internal void OnTerminalLocked(SoundPlayback playback, SoundPlaybackState terminal)
    {
        live.Remove(playback);
        switch (terminal)
        {
            case SoundPlaybackState.Completed:
                playbacksCompleted++;
                break;
            case SoundPlaybackState.Canceled:
                playbacksCanceled++;
                break;
            default:
                playbacksFailed++;
                break;
        }
    }

    internal static SoundException Classify(Exception exception, string sourceName) => exception as SoundException ??
        new SoundException(SoundErrorKind.SourceUnavailable, $"Sound source '{sourceName}' failed: {exception.Message}", exception);

    private object? TryGetCacheKey(SoundSource source)
    {
        try
        {
            return source.GetCacheKey(baseDirectory);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            // Opening reports the invalid path as a playback failure.
            return null;
        }
    }

    private void EnsureMixerLocked()
    {
        if (mixerThread is not null)
        {
            return;
        }

        mixerThread = new Thread(MixerLoop)
        {
            IsBackground = true,
            Name = "Timbre mixer",
            Priority = ThreadPriority.AboveNormal
        };
        mixerThread.Start();
    }

    private void FailLiveLocked(SoundException error)
    {
        foreach (SoundPlayback playback in live.ToArray())
        {
            playback.FailLocked(error);
        }
    }

    private void FailFromWorker(SoundPlayback playback, SoundException error)
    {
        lock (Sync)
        {
            playback.FailLocked(error);
        }

        SignalMixer();
    }

    private static void StopFeed(SoundPlayback playback, SoundFeed feed)
    {
        feed.Stop();
        feed.Stopped.ContinueWith(
            static (_, state) => ((SoundPlayback)state!).OnFeedStopped(),
            playback,
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private sealed class OutputClient(SoundRuntime runtime) : ISoundOutputClient
    {
        public void NotifyCapacityAvailable() => runtime.wake.Set();

        public void NotifyDeviceLost(Exception? error) => runtime.ReportDeviceLost(error);
    }
}
