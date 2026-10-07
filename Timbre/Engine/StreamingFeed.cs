using System.Collections.Concurrent;

namespace Cerneala.Timbre.Engine;

// Incremental source reading into a fixed ring of PCM frames. A pump on the
// thread pool reads directly into free ring space and publishes one segment
// per read; the mixer consumes segments in order. Capacity is bounded in
// frames, independent of read sizes and of the source duration.
internal sealed class StreamingFeed : SoundFeed
{
    internal const int RingFrames = 8192;
    internal const int MaxReadFrames = 2048;
    internal const int MaxSegments = 256;
    internal const long BufferBytes = RingFrames * 2L * sizeof(float);

    private readonly SoundReader reader;
    private readonly string sourceName;
    private readonly bool loop;
    private readonly Action<SoundException> fail;
    private readonly Action signalMixer;
    private readonly Action onReaderReleased;
    private readonly PumpDispatcher dispatcher;
    private readonly float[] ring = new float[RingFrames * 2];
    private readonly ConcurrentQueue<Segment> segments = new();
    private readonly AsyncAutoResetSignal pumpWake = new();
    private readonly CancellationTokenSource cancellation = new();
    private readonly object settleGate = new();
    private readonly TaskCompletionSource stopped = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private List<TaskCompletionSource>? settleWaiters;
    private bool pumpIdle;
    private volatile bool hasData;
    private int wakePending;
    private int stopPending;

    // Ring frames published by the pump and released by the mixer.
    private long writtenFrames;
    private long releasedFrames;

    // Seek request (mixer → pump) and outcome (pump → mixer).
    private long requestedTarget;
    private int requestedGeneration;
    private int outcomeGeneration;
    private Exception? outcomeError;

    // Mixer-owned consumption state.
    private Segment current;
    private bool hasCurrent;
    private int currentOffset;
    private int activeGeneration;

    internal StreamingFeed(
        SoundReader reader,
        string sourceName,
        bool loop,
        Action<SoundException> fail,
        Action signalMixer,
        Action onReaderReleased,
        PumpDispatcher dispatcher)
    {
        this.dispatcher = dispatcher;
        this.reader = reader;
        this.sourceName = sourceName;
        this.loop = loop;
        this.fail = fail;
        this.signalMixer = signalMixer;
        this.onReaderReleased = onReaderReleased;
        LengthFrames = reader.LengthFrames;
    }

    internal override long? LengthFrames { get; }

    internal override bool HasData => hasData;

    internal override Task Stopped => stopped.Task;

    internal void Start()
    {
        dispatcher.Register(this);
        _ = Task.Run(PumpAsync);
    }

    internal override int Read(Span<float> destination, int frames)
    {
        int written = 0;
        while (written < frames)
        {
            if (!hasCurrent)
            {
                if (!segments.TryDequeue(out current))
                {
                    break;
                }

                hasCurrent = true;
                currentOffset = 0;
            }

            if (current.Generation != activeGeneration)
            {
                ReleaseRing(current.Frames - currentOffset);
                hasCurrent = false;
                continue;
            }

            int count = Math.Min(frames - written, current.Frames - currentOffset);
            int ringFrame = (int)((current.RingStart + currentOffset) % RingFrames);
            ring.AsSpan(ringFrame * 2, count * 2).CopyTo(destination[(written * 2)..]);
            written += count;
            currentOffset += count;
            ReleaseRing(count);
            Position = current.SourceStart + currentOffset;
            if (currentOffset == current.Frames)
            {
                hasCurrent = false;
                if (current.WrapsAfter)
                {
                    LoopWraps++;
                }

                if (current.EndOfSource)
                {
                    Ended = true;
                    break;
                }
            }
        }

        return written;
    }

    internal override void RequestSeek(long frame, int generation)
    {
        Volatile.Write(ref requestedTarget, frame);
        Volatile.Write(ref requestedGeneration, generation);
        RequestWake();
    }

    internal override bool TryTakeSeekOutcome(int generation, out Exception? error)
    {
        error = null;
        if (Volatile.Read(ref outcomeGeneration) != generation)
        {
            return false;
        }

        error = Volatile.Read(ref outcomeError);
        if (error is null)
        {
            activeGeneration = generation;
            Position = Volatile.Read(ref requestedTarget);
            Ended = false;
        }

        return true;
    }

    internal override Task WhenSettledAsync()
    {
        lock (settleGate)
        {
            if (pumpIdle || stopped.Task.IsCompleted)
            {
                return Task.CompletedTask;
            }

            TaskCompletionSource waiter = new(TaskCreationOptions.RunContinuationsAsynchronously);
            (settleWaiters ??= []).Add(waiter);
            return waiter.Task;
        }
    }

    // May run on the mixer thread: cancellation itself runs on the dispatcher.
    internal override void Stop()
    {
        Interlocked.Exchange(ref stopPending, 1);
        dispatcher.Signal(this);
    }

    // Dispatcher thread only.
    internal void Dispatch()
    {
        bool stopping = Interlocked.Exchange(ref stopPending, 0) != 0;
        if (stopping)
        {
            try
            {
                cancellation.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        if (Interlocked.Exchange(ref wakePending, 0) != 0 || stopping)
        {
            pumpWake.Set();
        }
    }

    private void RequestWake()
    {
        if (Interlocked.Exchange(ref wakePending, 1) == 0)
        {
            dispatcher.Signal(this);
        }
    }

    private void ReleaseRing(int frames)
    {
        if (frames <= 0)
        {
            return;
        }

        Volatile.Write(ref releasedFrames, releasedFrames + frames);
        RequestWake();
    }

    private async Task PumpAsync()
    {
        CancellationToken token = cancellation.Token;
        int handledGeneration = 0;
        int dataGeneration = 0;
        long readerPosition = 0;
        bool endOfSource = false;
        bool previousWasEmpty = false;
        try
        {
            while (!token.IsCancellationRequested)
            {
                int generation = Volatile.Read(ref requestedGeneration);
                if (generation != handledGeneration)
                {
                    long target = Volatile.Read(ref requestedTarget);
                    handledGeneration = generation;
                    try
                    {
                        await reader.SeekAsync(target, token).ConfigureAwait(false);
                        readerPosition = target;
                        dataGeneration = generation;
                        endOfSource = false;
                        previousWasEmpty = false;
                        Volatile.Write(ref outcomeError, null);
                    }
                    catch (Exception exception) when (exception is not OperationCanceledException || !token.IsCancellationRequested)
                    {
                        Volatile.Write(ref outcomeError, exception);
                    }

                    Volatile.Write(ref outcomeGeneration, generation);
                    signalMixer();
                    continue;
                }

                int space = FreeContiguousFrames();
                if (endOfSource || space == 0 || segments.Count >= MaxSegments)
                {
                    // Released ring space, a seek, and Stop() all set the signal;
                    // a signal set before this wait is never lost.
                    SetIdle(true);
                    await pumpWake.WaitAsync().ConfigureAwait(false);
                    SetIdle(false);
                    token.ThrowIfCancellationRequested();
                    continue;
                }

                int ringFrame = (int)(writtenFrames % RingFrames);
                Memory<float> destination = ring.AsMemory(ringFrame * 2, space * 2);
                ValueTask<SoundReadResult> pending = reader.ReadAsync(destination, token);
                bool completedSynchronously = pending.IsCompleted;
                SoundReadResult result = await pending.ConfigureAwait(false);
                Validate(destination.Span, space, result);
                if (result.Frames == 0 && !result.EndOfSource)
                {
                    if (previousWasEmpty && completedSynchronously)
                    {
                        throw new SoundException(
                            SoundErrorKind.InvalidData,
                            $"Reader for '{sourceName}' returned no data again synchronously instead of waiting.");
                    }

                    previousWasEmpty = true;
                    continue;
                }

                previousWasEmpty = false;
                Segment segment = new(writtenFrames, result.Frames, readerPosition, dataGeneration);
                readerPosition += result.Frames;
                if (result.EndOfSource)
                {
                    if (loop && readerPosition > 0)
                    {
                        await reader.SeekAsync(0, token).ConfigureAwait(false);
                        readerPosition = 0;
                        segment.WrapsAfter = true;
                    }
                    else
                    {
                        segment.EndOfSource = true;
                        endOfSource = true;
                    }
                }

                writtenFrames += result.Frames;
                segments.Enqueue(segment);
                hasData = true;
                signalMixer();
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            fail(SoundRuntime.Classify(exception, sourceName));
        }
        finally
        {
            try
            {
                reader.Dispose();
            }
            catch (Exception)
            {
                // Disposal failures cannot be reported to a terminal playback.
            }

            onReaderReleased();
            dispatcher.Unregister(this);
            cancellation.Dispose();
            SetIdle(true);
            stopped.TrySetResult();
            signalMixer();
        }
    }

    private int FreeContiguousFrames()
    {
        long free = RingFrames - (writtenFrames - Volatile.Read(ref releasedFrames));
        int toRingEnd = RingFrames - (int)(writtenFrames % RingFrames);
        return (int)Math.Min(Math.Min(free, toRingEnd), MaxReadFrames);
    }

    private void Validate(Span<float> destination, int requested, SoundReadResult result)
    {
        if (result.Frames > requested)
        {
            throw new SoundException(SoundErrorKind.InvalidData, $"Reader for '{sourceName}' returned more frames than requested.");
        }

        foreach (float sample in destination[..(result.Frames * 2)])
        {
            if (!float.IsFinite(sample))
            {
                throw new SoundException(SoundErrorKind.InvalidData, $"Reader for '{sourceName}' produced a non-finite sample.");
            }
        }
    }

    private void SetIdle(bool idle)
    {
        List<TaskCompletionSource>? waiters = null;
        lock (settleGate)
        {
            pumpIdle = idle;
            if (idle)
            {
                waiters = settleWaiters;
                settleWaiters = null;
            }
        }

        waiters?.ForEach(waiter => waiter.TrySetResult());
    }

    private struct Segment(long ringStart, int frames, long sourceStart, int generation)
    {
        internal long RingStart = ringStart;
        internal int Frames = frames;
        internal long SourceStart = sourceStart;
        internal int Generation = generation;
        internal bool EndOfSource;
        internal bool WrapsAfter;
    }
}
