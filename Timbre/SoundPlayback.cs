using Cerneala.Timbre.Catalog;
using Cerneala.Timbre.Dsp;
using Cerneala.Timbre.Engine;

namespace Cerneala.Timbre;

public sealed class SoundPlayback
{
    private readonly SoundRuntime runtime;
    private readonly SoundScope scope;
    private readonly TaskCompletionSource<SoundPlaybackResult> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource released = new(TaskCreationOptions.RunContinuationsAsynchronously);

    // Control state, guarded by runtime.Sync.
    private SoundPlaybackState state = SoundPlaybackState.Pending;
    private bool started;
    private bool paused;
    private float volume;
    private readonly float[] values;
    private int controlVersion;
    private int seekGeneration;
    private long seekTarget;
    private TaskCompletionSource? seekCompletion;
    private SoundFeed? feed;
    private bool releaseRequested;
    private int outstandingResources;

    // Audio Motion: per-slot count of manual writes (slot 0 = Volume, slot
    // i + 1 = parameter i) and of animated publications, guarded by Sync.
    private readonly int[] manualWrites;
    private long animatedPublications;

    // Published by the mixer.
    private long positionFrames;
    private long lengthFrames = -1;
    private long firstQueuedTimestamp;

    internal SoundPlayback(SoundRuntime runtime, SoundScope scope, SoundClip clip, SoundStartOptions options)
    {
        this.runtime = runtime;
        this.scope = scope;
        Clip = clip;
        Loop = options.LoopValue;
        volume = options.VolumeValue;
        values = options.Values;
        manualWrites = new int[values.Length + 1];
        Render = new SoundVoice(volume, (float[])values.Clone(), SoundDspChain.Create(clip));
    }

    // Descriptor of the intrinsic Volume of every playback, so Set and Motion
    // address it like a declared parameter. It is never declared by a clip.
    public static SoundParameter<float> VolumeParameter { get; } = new("Volume", 1f);

    public SoundClip Clip { get; }

    public SoundPlaybackState State
    {
        get
        {
            lock (runtime.Sync)
            {
                return state;
            }
        }
    }

    public float Volume
    {
        get
        {
            lock (runtime.Sync)
            {
                return volume;
            }
        }
        set
        {
            SoundClip.ValidateVolume(value, nameof(value));
            lock (runtime.Sync)
            {
                ThrowIfTerminalLocked();
                volume = value;
                manualWrites[0]++;
                controlVersion++;
                runtime.CountParameterPublication();
            }

            runtime.SignalMixer();
        }
    }

    public bool Loop { get; }

    public TimeSpan Position => SoundTime.FromFrames(Volatile.Read(ref positionFrames));

    public TimeSpan? Duration
    {
        get
        {
            long length = Volatile.Read(ref lengthFrames);
            return length < 0 ? null : SoundTime.FromFrames(length);
        }
    }

    public Task<SoundPlaybackResult> Completion => completion.Task;

    internal Task WhenReady => ready.Task;

    internal Task WhenReleased => released.Task;

    // Mixer-owned render state; touched only on the mixer thread or under runtime.Sync.
    internal SoundVoice Render { get; }

    internal bool IsTerminal => state is SoundPlaybackState.Completed or SoundPlaybackState.Canceled or SoundPlaybackState.Failed;

    internal SoundPlaybackState StateLocked => state;

    internal bool IsPausedLocked => paused;

    internal SoundFeed? FeedLocked => feed;

    public void Set<T>(SoundParameter<T> parameter, T value)
        where T : struct
    {
        if (ReferenceEquals(parameter, VolumeParameter))
        {
            Volume = SoundParameter<T>.ToFloat(value);
            return;
        }

        int index = Clip.GetParameterIndex(parameter, nameof(parameter));
        float number = SoundParameter<T>.ToFloat(value);
        lock (runtime.Sync)
        {
            ThrowIfTerminalLocked();
            Clip.ValidateParameterValue(index, number, nameof(value));
            values[index] = number;
            manualWrites[index + 1]++;
            controlVersion++;
            runtime.CountParameterPublication();
        }

        runtime.SignalMixer();
    }

    public void Cancel()
    {
        lock (runtime.Sync)
        {
            CancelLocked();
        }

        runtime.SignalMixer();
    }

    public void Pause()
    {
        lock (runtime.Sync)
        {
            ThrowIfTerminalLocked();
            PauseLocked();
        }

        runtime.SignalMixer();
    }

    public void Resume()
    {
        lock (runtime.Sync)
        {
            ThrowIfTerminalLocked();
            ResumeLocked();
        }

        runtime.SignalMixer();
    }

    public Task SeekAsync(TimeSpan position) => SeekCore(position, throwIfTerminal: true)!;

    // Markup handle commands: a playback that became terminal concurrently is
    // an empty slot, so these return false/null instead of throwing.
    internal bool TryPause()
    {
        lock (runtime.Sync)
        {
            if (IsTerminal)
            {
                return false;
            }

            PauseLocked();
        }

        runtime.SignalMixer();
        return true;
    }

    internal bool TryResume()
    {
        lock (runtime.Sync)
        {
            if (IsTerminal)
            {
                return false;
            }

            ResumeLocked();
        }

        runtime.SignalMixer();
        return true;
    }

    internal Task? TrySeekAsync(TimeSpan position) => SeekCore(position, throwIfTerminal: false);

    private void PauseLocked()
    {
        if (paused)
        {
            return;
        }

        paused = true;
        state = SoundPlaybackState.Paused;
        runtime.CountPause();
    }

    private void ResumeLocked()
    {
        if (!paused)
        {
            return;
        }

        paused = false;
        state = started ? SoundPlaybackState.Playing : SoundPlaybackState.Pending;
        runtime.CountResume();
    }

    private Task? SeekCore(TimeSpan position, bool throwIfTerminal)
    {
        if (position < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(position), position, "A seek target cannot be negative.");
        }

        long frame = SoundTime.ToFrames(position);
        Task task;
        lock (runtime.Sync)
        {
            if (IsTerminal && !throwIfTerminal)
            {
                return null;
            }

            ThrowIfTerminalLocked();
            long length = Volatile.Read(ref lengthFrames);
            if (length >= 0 && frame > length)
            {
                throw new ArgumentOutOfRangeException(nameof(position), position, "The seek target is beyond the duration of the source.");
            }

            if (seekCompletion is not null && seekCompletion.TrySetCanceled())
            {
                runtime.CountSeekSuperseded();
            }

            seekCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            task = seekCompletion.Task;
            seekGeneration++;
            seekTarget = frame;
            runtime.CountSeekRequested();
        }

        runtime.SignalMixer();
        return task;
    }

    internal Task WhenSettledAsync()
    {
        SoundFeed? current;
        lock (runtime.Sync)
        {
            if (IsTerminal)
            {
                return Task.CompletedTask;
            }

            current = feed;
        }

        return current?.WhenSettledAsync() ?? WhenReady.ContinueWith(_ => WhenSettledAsync(), TaskScheduler.Default).Unwrap();
    }

    // ---- Locked transitions (caller holds runtime.Sync) ----

    internal void CancelLocked() => CompleteLocked(SoundPlaybackState.Canceled, null, tailTruncated: false);

    internal void FailLocked(SoundException error) => CompleteLocked(SoundPlaybackState.Failed, error, tailTruncated: false);

    internal void CompleteLocked(SoundPlaybackState terminal, SoundException? error, bool tailTruncated)
    {
        if (IsTerminal)
        {
            return;
        }

        state = terminal;
        paused = false;
        seekCompletion?.TrySetCanceled();
        seekCompletion = null;
        scope.RemoveLocked(this);
        runtime.OnTerminalLocked(this, terminal);
        completion.TrySetResult(new SoundPlaybackResult(terminal, error, tailTruncated));
        ready.TrySetResult();
    }

    internal void MarkStartedLocked()
    {
        started = true;
        if (state == SoundPlaybackState.Pending)
        {
            state = SoundPlaybackState.Playing;
        }
    }

    // Copies published controls into the render state; returns true for a
    // new seek request the mixer must forward to the feed.
    internal void ApplyControlsLocked(out bool seekRequested, out long target, out int generation)
    {
        if (Render.ControlVersion != controlVersion)
        {
            Render.ControlVersion = controlVersion;
            Render.Volume = volume;
            values.CopyTo(Render.Values, 0);
        }

        seekRequested = feed is not null && Render.AppliedSeekGeneration != seekGeneration;
        target = seekTarget;
        generation = seekGeneration;
        if (seekRequested)
        {
            Render.AppliedSeekGeneration = seekGeneration;
            Render.SeekPending = true;
        }
    }

    internal void CompleteSeekLocked(int generation, Exception? error)
    {
        if (generation != seekGeneration || seekCompletion is null)
        {
            return;
        }

        if (error is null)
        {
            seekCompletion.TrySetResult();
            runtime.CountSeekCompleted();
        }
        else
        {
            seekCompletion.TrySetException(error);
        }

        seekCompletion = null;
    }

    internal void PublishPosition(long frames) => Volatile.Write(ref positionFrames, frames);

    // Mixer thread: a length that a streaming source learned at its end.
    internal void PublishLength(long? frames)
    {
        if (frames is long known && Volatile.Read(ref lengthFrames) < 0)
        {
            Volatile.Write(ref lengthFrames, known);
        }
    }

    // Stopwatch timestamp of the first submitted block that contains this
    // playback (first PCM queued), or 0 before it.
    internal long FirstQueuedTimestamp => Volatile.Read(ref firstQueuedTimestamp);

    internal void StampFirstQueued(long timestamp)
    {
        if (firstQueuedTimestamp == 0)
        {
            Volatile.Write(ref firstQueuedTimestamp, timestamp);
        }
    }

    // ---- Audio Motion (UI adapter) ----

    internal SoundScope Scope => scope;

    internal int MotionSlotCount => manualWrites.Length;

    // Slot of a descriptor: 0 for VolumeParameter, i + 1 for clip parameter i.
    internal int GetMotionSlot(SoundParameter<float> parameter, string argumentName) =>
        ReferenceEquals(parameter, VolumeParameter) ? 0 : Clip.GetParameterIndex(parameter, argumentName) + 1;

    internal bool IsValidMotionValue(int slot, float value) =>
        slot == 0 ? TimbreCatalog.Volume.Contains(value) : Clip.IsParameterValueValid(slot - 1, value);

    internal void ValidateMotionValue(int slot, float value, string argumentName)
    {
        if (slot == 0)
        {
            SoundClip.ValidateVolume(value, argumentName);
        }
        else
        {
            Clip.ValidateParameterValue(slot - 1, value, argumentName);
        }
    }

    // One snapshot under Sync: the slot's current value and manual version.
    internal void ReadMotionSlot(int slot, out float value, out int manualVersion, out bool terminal)
    {
        lock (runtime.Sync)
        {
            value = slot == 0 ? volume : values[slot - 1];
            manualVersion = manualWrites[slot];
            terminal = IsTerminal;
        }
    }

    // One snapshot under Sync for a Motion sample: animation time runs only
    // while the playback produces PCM (started, not paused, no pending seek).
    internal bool ReadMotionState(Span<int> manualVersions, out bool clockRunning)
    {
        lock (runtime.Sync)
        {
            manualWrites.AsSpan().CopyTo(manualVersions);
            clockRunning = started && !paused && seekCompletion is null && !IsTerminal;
            return IsTerminal;
        }
    }

    // Publishes the dirty animated slots together, without counting them as
    // manual writes. Returns false when the playback is already terminal.
    internal bool TryPublishMotion(ReadOnlySpan<bool> dirty, ReadOnlySpan<float> samples)
    {
        lock (runtime.Sync)
        {
            if (IsTerminal)
            {
                return false;
            }

            for (int slot = 0; slot < dirty.Length; slot++)
            {
                if (!dirty[slot])
                {
                    continue;
                }

                if (slot == 0)
                {
                    volume = samples[slot];
                }
                else
                {
                    values[slot - 1] = samples[slot];
                }
            }

            controlVersion++;
            animatedPublications++;
            runtime.CountAnimatedPublication();
        }

        runtime.SignalMixer();
        return true;
    }

    internal void ReportRejectedMotionSample()
    {
        lock (runtime.Sync)
        {
            runtime.CountMotionSampleRejected();
        }
    }

    internal long AnimatedPublications
    {
        get
        {
            lock (runtime.Sync)
            {
                return animatedPublications;
            }
        }
    }

    internal float GetMotionSlotValue(int slot)
    {
        ReadMotionSlot(slot, out float value, out _, out _);
        return value;
    }

    // ---- Resource ownership ----

    internal void BeginLoadLocked() => outstandingResources++;

    // Returns false when the playback already ended: the caller then stops
    // the feed itself, and release completes once it has stopped.
    internal bool TryAttachFeedLocked(SoundFeed attached, bool fromLoader)
    {
        if (!fromLoader)
        {
            outstandingResources++;
        }

        if (attached.LengthFrames is long length)
        {
            Volatile.Write(ref lengthFrames, length);
        }

        if (IsTerminal || releaseRequested)
        {
            return false;
        }

        feed = attached;
        ready.TrySetResult();
        return true;
    }

    internal void LoaderFailedLocked() => ResourceReleasedLocked();

    // Mixer or disposal: returns the feed to stop, if any.
    internal SoundFeed? RequestReleaseLocked()
    {
        if (releaseRequested)
        {
            return null;
        }

        releaseRequested = true;
        if (outstandingResources == 0)
        {
            released.TrySetResult();
        }

        return feed;
    }

    internal void ResourceReleasedLocked()
    {
        if (--outstandingResources == 0 && (releaseRequested || IsTerminal))
        {
            released.TrySetResult();
        }
    }

    internal void OnFeedStopped()
    {
        lock (runtime.Sync)
        {
            ResourceReleasedLocked();
        }
    }

    private void ThrowIfTerminalLocked()
    {
        if (IsTerminal)
        {
            throw new InvalidOperationException($"The sound playback is {state}; start a new playback instead.");
        }
    }
}
