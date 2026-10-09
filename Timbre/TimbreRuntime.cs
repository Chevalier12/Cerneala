using Cerneala.Timbre.Catalog;
using Cerneala.Timbre.Engine;

namespace Cerneala.Timbre;

public sealed partial class TimbreRuntime : IDisposable
{
    public const int SampleRate = TimbreCatalog.SampleRate;

    public const int ChannelCount = TimbreCatalog.ChannelCount;

    private readonly ITimbreOutput? output;
    private readonly int maxVoices;
    private readonly long autoPreloadMaxBytes;
    private readonly long maxPreloadBytes;
    private readonly TimbreMemoryPool memory;
    private readonly long tailCapFrames;
    private readonly string baseDirectory;
    private readonly TimbrePayloadCache cache;
    private readonly OutputClient client;
    private readonly PumpDispatcher pumpDispatcher = new();
    private readonly AutoResetEvent wake = new(initialState: false);
    private readonly List<TimbrePlayback> live = [];
    private readonly List<TimbrePlayback> adopted = [];
    private readonly List<TimbreScope> scopes = [];
    private readonly List<TaskCompletionSource> syncRequests = [];
    private Thread? mixerThread;
    private volatile bool disposed;
    private long streamingBytes;

    public TimbreRuntime(TimbreRuntimeOptions? options = null)
    {
        options ??= new TimbreRuntimeOptions();
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
        memory = new TimbreMemoryPool(options.StreamingMemoryLimit);
        tailCapFrames = TimbreTime.ToFrames(options.DelayTailCap);
        baseDirectory = options.BaseDirectory ?? AppContext.BaseDirectory;
        cache = new TimbrePayloadCache(options.MaxCacheBytes);
        client = new OutputClient(this);
    }

    public bool IsDisposed => disposed;

    internal object Sync { get; } = new();

    public TimbreScope CreateScope()
    {
        lock (Sync)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            TimbreScope scope = new(this);
            scopes.Add(scope);
            return scope;
        }
    }

    public Task PrepareAsync(TimbreSound sound, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sound);
        ObjectDisposedException.ThrowIf(disposed, this);
        return Task.Run(() => PrepareCoreAsync(sound, cancellationToken), cancellationToken);
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
            foreach (TimbrePlayback playback in live.ToArray())
            {
                playback.CancelLocked();
            }

            foreach (TimbreScope scope in scopes.ToArray())
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

    internal TimbreRuntimeDiagnostics GetDiagnostics()
    {
        lock (Sync)
        {
            return new TimbreRuntimeDiagnostics(
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
    internal void RemoveScopeLocked(TimbreScope scope) => scopes.Remove(scope);

    // Optional mixer-thread instrumentation for the cost gate; null in normal use.
    internal ITimbreBlockObserver? BlockObserver { get; set; }

    // Optional test instrumentation: every playback accepted by Start, on the
    // starting thread after it is published; null in normal use.
    internal Action<TimbrePlayback>? PlaybackAccepted { get; set; }

    // Optional test instrumentation: fully constructed but not yet published,
    // on the starting thread; null in normal use.
    internal Action<TimbrePlayback>? PlaybackConstructed { get; set; }

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

    internal TimbrePlayback Start(TimbreScope scope, TimbreSound sound, TimbreStartOptions options, TimbreHandle? handle)
    {
        object? key = TryGetCacheKey(sound.Source);
        lock (Sync)
        {
            // Avoid allocating DSP state for an already inadmissible start.
            ValidateStartLocked(scope, handle);
        }

        // Playback/DSP construction can allocate large delay buffers. Keep it
        // off the lock the mixer needs every block; nothing is published yet.
        TimbrePlayback playback = new(this, scope, sound, options);
        bool needsLoad = false;
        lock (Sync)
        {
            // Disposal, capacity, and the handle occupant may have changed
            // during construction. Admission and replacement are atomic here.
            TimbrePlayback? previous = ValidateStartLocked(scope, handle);
            bool replacing = previous is { IsTerminal: false };
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
                // Render state is initialized atomically before mixer adoption.
                playback.Render.Silence();
                previous!.CancelLocked();
            }

            if (sound.Loading != TimbreLoading.Streaming && key is not null && cache.TryAcquire(key, out TimbrePayloadCache.Entry? entry))
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

    // Caller holds Sync. Does not reserve a voice or change the handle.
    private TimbrePlayback? ValidateStartLocked(TimbreScope scope, TimbreHandle? handle)
    {
        scope.ThrowIfDisposed();
        TimbrePlayback? previous = handle?.OccupantLocked;
        bool replacing = previous is { IsTerminal: false };
        if (live.Count - (replacing ? 1 : 0) + 1 > maxVoices)
        {
            throw new TimbreException(
                TimbreErrorKind.VoiceLimitExceeded,
                $"The sound runtime already has the maximum of {maxVoices} active playbacks.");
        }

        return previous;
    }

    internal void SignalMixer() => wake.Set();

    // Caller holds Sync.
    internal void OnTerminalLocked(TimbrePlayback playback, TimbrePlaybackState terminal)
    {
        live.Remove(playback);
        switch (terminal)
        {
            case TimbrePlaybackState.Completed:
                playbacksCompleted++;
                break;
            case TimbrePlaybackState.Canceled:
                playbacksCanceled++;
                break;
            default:
                playbacksFailed++;
                break;
        }
    }

    internal static TimbreException Classify(Exception exception, string sourceName) => exception as TimbreException ??
        new TimbreException(TimbreErrorKind.SourceUnavailable, $"Timbre source '{sourceName}' failed: {exception.Message}", exception);

    private object? TryGetCacheKey(TimbreSource source)
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

    private void FailLiveLocked(TimbreException error)
    {
        foreach (TimbrePlayback playback in live.ToArray())
        {
            playback.FailLocked(error);
        }
    }

    private void FailFromWorker(TimbrePlayback playback, TimbreException error)
    {
        lock (Sync)
        {
            playback.FailLocked(error);
        }

        SignalMixer();
    }

    private static void StopFeed(TimbrePlayback playback, TimbreFeed feed)
    {
        feed.Stop();
        feed.Stopped.ContinueWith(
            static (_, state) => ((TimbrePlayback)state!).OnFeedStopped(),
            playback,
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private sealed class OutputClient(TimbreRuntime runtime) : ITimbreOutputClient
    {
        public void NotifyCapacityAvailable() => runtime.wake.Set();

        public void NotifyDeviceLost(Exception? error) => runtime.ReportDeviceLost(error);
    }
}
