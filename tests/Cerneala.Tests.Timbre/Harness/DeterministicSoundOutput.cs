using Cerneala.Timbre;

namespace Cerneala.Tests.Timbre.Harness;

// Deterministic test sink for the final mix. It never mixes or processes: it
// records submitted PCM, exposes queue depth, and consumes only when a test
// releases frames, notifying the runtime the way a device callback would.
internal sealed class DeterministicSoundOutput : ISoundOutput
{
    private readonly object gate = new();
    private readonly List<float> samples = [];
    private readonly List<(long Frames, TaskCompletionSource Signal)> submittedWaiters = [];
    private ISoundOutputClient? client;
    private long consumedFrames;
    private long submittedFrames;
    private bool held;

    public Exception? OpenFailure { get; set; }

    // Free-running stress mode: every submission is consumed at once.
    public bool AutoConsume { get; init; }

    // Stress runs can drop PCM and keep only the frame accounting.
    public bool RecordSamples { get; init; } = true;

    public int OpenCount { get; private set; }

    public int CloseCount { get; private set; }

    public int SubmitCount { get; private set; }

    public int MaxQueuedFrames { get; private set; }

    public bool IsOpen { get; private set; }

    // While held the sink reports a full software queue, so a test can set up
    // several playbacks before the runtime produces the first block.
    public int QueuedFrames
    {
        get
        {
            lock (gate)
            {
                int queued = (int)(SubmittedFramesUnsafe - consumedFrames);
                return held ? Math.Max(queued, HeldQueueFrames) : queued;
            }
        }
    }

    public const int HeldQueueFrames = 1920;

    public void Hold()
    {
        lock (gate)
        {
            held = true;
        }
    }

    public void Release()
    {
        ISoundOutputClient? notify;
        lock (gate)
        {
            held = false;
            notify = client;
        }

        notify?.NotifyCapacityAvailable();
    }

    public long SubmittedFrames
    {
        get
        {
            lock (gate)
            {
                return SubmittedFramesUnsafe;
            }
        }
    }

    public long ConsumedFrames
    {
        get
        {
            lock (gate)
            {
                return consumedFrames;
            }
        }
    }

    private long SubmittedFramesUnsafe => submittedFrames;

    public void Open(ISoundOutputClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        lock (gate)
        {
            OpenCount++;
            if (OpenFailure is not null)
            {
                throw OpenFailure;
            }

            this.client = client;
            IsOpen = true;
        }
    }

    public void Submit(ReadOnlySpan<float> interleaved)
    {
        if (interleaved.Length % SoundRuntime.ChannelCount != 0)
        {
            throw new ArgumentException("Output submissions must contain complete stereo frames.", nameof(interleaved));
        }

        List<TaskCompletionSource>? ready = null;
        ISoundOutputClient? notify = null;
        lock (gate)
        {
            if (!IsOpen)
            {
                throw new InvalidOperationException("The output is not open.");
            }

            if (RecordSamples)
            {
                for (int index = 0; index < interleaved.Length; index++)
                {
                    samples.Add(interleaved[index]);
                }
            }

            submittedFrames += interleaved.Length / SoundRuntime.ChannelCount;

            SubmitCount++;
            MaxQueuedFrames = Math.Max(MaxQueuedFrames, (int)(SubmittedFramesUnsafe - consumedFrames));
            for (int index = submittedWaiters.Count - 1; index >= 0; index--)
            {
                if (submittedWaiters[index].Frames <= SubmittedFramesUnsafe)
                {
                    (ready ??= []).Add(submittedWaiters[index].Signal);
                    submittedWaiters.RemoveAt(index);
                }
            }

            if (AutoConsume)
            {
                consumedFrames = submittedFrames;
                notify = client;
            }
        }

        ready?.ForEach(signal => signal.TrySetResult());
        notify?.NotifyCapacityAvailable();
    }

    public void Close()
    {
        lock (gate)
        {
            // Closing a device discards PCM it had not consumed yet.
            CloseCount++;
            IsOpen = false;
            client = null;
            consumedFrames = SubmittedFramesUnsafe;
        }
    }

    public void Consume(int frames)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(frames);
        ISoundOutputClient? notify;
        lock (gate)
        {
            consumedFrames = Math.Min(consumedFrames + frames, SubmittedFramesUnsafe);
            notify = client;
        }

        notify?.NotifyCapacityAvailable();
    }

    public void ConsumeAll() => Consume(int.MaxValue);

    public void LoseDevice(Exception? error)
    {
        ISoundOutputClient? notify;
        lock (gate)
        {
            notify = client;
        }

        notify?.NotifyDeviceLost(error);
    }

    public float[] Read(long startFrame, int frames)
    {
        lock (gate)
        {
            long available = Math.Max(0, SubmittedFramesUnsafe - startFrame);
            int count = (int)Math.Min(frames, available);
            float[] result = new float[frames * SoundRuntime.ChannelCount];
            samples.CopyTo((int)(startFrame * SoundRuntime.ChannelCount), result, 0, count * SoundRuntime.ChannelCount);
            return result;
        }
    }

    public float[] ReadAll()
    {
        lock (gate)
        {
            return samples.ToArray();
        }
    }

    public Task WaitForSubmittedFramesAsync(long frames, TimeSpan? timeout = null)
    {
        TaskCompletionSource signal = new(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (gate)
        {
            if (SubmittedFramesUnsafe >= frames)
            {
                return Task.CompletedTask;
            }

            submittedWaiters.Add((frames, signal));
        }

        return HarnessWait.WithTimeout(signal.Task, timeout, $"Output did not receive {frames} submitted frames.");
    }
}
