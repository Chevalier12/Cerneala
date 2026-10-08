using System.Collections.Concurrent;
using Cerneala.Timbre.Catalog;

namespace Cerneala.Timbre.Engine;

// Incremental source reading into a fixed ring of PCM frames. A pump on the
// thread pool reads directly into free ring space and publishes one segment
// per read; the mixer consumes segments in order. Capacity is bounded in
// frames, independent of read sizes and of the source duration.
internal sealed class StreamingFeed : TimbreFeed
{
    internal const int RingFrames = 8192;
    internal const int MaxReadFrames = 2048;
    internal const int MaxSegments = 256;
    internal const long BufferBytes = RingFrames * 2L * sizeof(float);

    private readonly TimbreReader reader;
    private readonly string sourceName;
    private readonly bool loop;
    private readonly Action<TimbreException> fail;
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
    private volatile bool sourceEnded;
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

    // After a successful seek the voice waits for the first data of the new
    // generation instead of mixing an underrun while the pump refills.
    private bool awaitingSeekData;

    // Decoders publish packet-sized reads, and a starting voice is mixed into a
    // whole software queue back to back. The voice starts, and restarts after a
    // seek, only once that much is buffered or the rest of the source is, so the
    // start is never padded; later underruns stay counted padding.
    private bool primed;
    private long lengthFrames;

    // The reader's length when the feed started (-1 if unknown); pump-owned check.
    private readonly long declaredLength;

    internal StreamingFeed(
        TimbreReader reader,
        string sourceName,
        bool loop,
        Action<TimbreException> fail,
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
        lengthFrames = reader.LengthFrames ?? -1;
        declaredLength = lengthFrames;
    }

    // Learned by the mixer at the end of the source when the reader did not
    // know it when the feed started.
    internal override long? LengthFrames => lengthFrames >= 0 ? lengthFrames : null;

    // Mixer thread only.
    internal override bool HasData
    {
        get
        {
            if (!hasData)
            {
                return false;
            }

            if (awaitingSeekData)
            {
                DropStaleSegments();
                if (!hasCurrent && segments.IsEmpty)
                {
                    return false;
                }

                awaitingSeekData = false;
            }

            if (!primed)
            {
                // sourceEnded is published after the final segment.
                if (!sourceEnded && Volatile.Read(ref writtenFrames) - releasedFrames < TimbreCatalog.OutputQueueBudgetFrames)
                {
                    return false;
                }

                primed = true;
            }

            return true;
        }
    }

    internal override Task Stopped => stopped.Task;

    // sourceEnded is published after the final segment.
    internal override bool HasBlock(int frames) =>
        sourceEnded || Volatile.Read(ref writtenFrames) - releasedFrames >= frames;

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
                if (current.WrapsAfter || current.EndOfSource)
                {
                    lengthFrames = current.SourceStart + current.Frames;
                }

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
            awaitingSeekData = true;
            primed = false;
            DropStaleSegments();
        }

        return true;
    }

    // Mixer thread: releases the ring space of segments read before the active
    // seek, so the pump can refill it with the new generation at once.
    private void DropStaleSegments()
    {
        if (hasCurrent && current.Generation != activeGeneration)
        {
            ReleaseRing(current.Frames - currentOffset);
            hasCurrent = false;
        }

        while (!hasCurrent && segments.TryPeek(out Segment next) && next.Generation != activeGeneration && segments.TryDequeue(out Segment stale))
        {
            ReleaseRing(stale.Frames);
        }
    }

    internal override Task WhenSettledAsync()
    {
        lock (settleGate)
        {
            if (stopped.Task.IsCompleted || pumpIdle && NothingToDo())
            {
                return Task.CompletedTask;
            }

            TaskCompletionSource waiter = new(TaskCreationOptions.RunContinuationsAsynchronously);
            (settleWaiters ??= []).Add(waiter);
            return waiter.Task;
        }
    }

    // Settled means the pump is idle and could not do anything else: the ring
    // (or its segment list) is full or the source ended, and no seek waits.
    private bool NothingToDo() =>
        Volatile.Read(ref requestedGeneration) == Volatile.Read(ref outcomeGeneration) &&
        (sourceEnded || segments.Count >= MaxSegments || Volatile.Read(ref writtenFrames) - Volatile.Read(ref releasedFrames) >= RingFrames);

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
                        sourceEnded = false;
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
                ValueTask<TimbreReadResult> pending = reader.ReadAsync(destination, token);
                bool completedSynchronously = pending.IsCompleted;
                TimbreReadResult result = await pending.ConfigureAwait(false);
                Validate(destination.Span, space, result);
                if (result.Frames == 0 && !result.EndOfSource)
                {
                    if (previousWasEmpty && completedSynchronously)
                    {
                        throw new TimbreException(
                            TimbreErrorKind.InvalidData,
                            $"Reader for '{sourceName}' returned no data again synchronously instead of waiting.");
                    }

                    previousWasEmpty = true;
                    continue;
                }

                previousWasEmpty = false;
                Segment segment = new(writtenFrames, result.Frames, readerPosition, dataGeneration);
                readerPosition += result.Frames;
                if (declaredLength >= 0 && (readerPosition > declaredLength || result.EndOfSource && readerPosition < declaredLength))
                {
                    // Same rule as preloading: a source that ends early or runs past
                    // its declared length is invalid, not a successful end.
                    throw new TimbreException(
                        TimbreErrorKind.InvalidData,
                        $"Timbre source '{sourceName}' ended at frame {readerPosition} but declared {declaredLength} frames.");
                }

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

                Volatile.Write(ref writtenFrames, writtenFrames + result.Frames);
                segments.Enqueue(segment);
                if (segment.EndOfSource)
                {
                    sourceEnded = true;
                }

                hasData = true;
                signalMixer();
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            fail(TimbreRuntime.Classify(exception, sourceName));
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
            stopped.TrySetResult();
            SetIdle(true);
            signalMixer();
        }
    }

    private int FreeContiguousFrames()
    {
        long free = RingFrames - (writtenFrames - Volatile.Read(ref releasedFrames));
        int toRingEnd = RingFrames - (int)(writtenFrames % RingFrames);
        return (int)Math.Min(Math.Min(free, toRingEnd), MaxReadFrames);
    }

    private void Validate(Span<float> destination, int requested, TimbreReadResult result)
    {
        if (result.Frames > requested)
        {
            throw new TimbreException(TimbreErrorKind.InvalidData, $"Reader for '{sourceName}' returned more frames than requested.");
        }

        foreach (float sample in destination[..(result.Frames * 2)])
        {
            if (!float.IsFinite(sample))
            {
                throw new TimbreException(TimbreErrorKind.InvalidData, $"Reader for '{sourceName}' produced a non-finite sample.");
            }
        }
    }

    private void SetIdle(bool idle)
    {
        List<TaskCompletionSource>? waiters = null;
        lock (settleGate)
        {
            pumpIdle = idle;
            if (idle && (NothingToDo() || stopped.Task.IsCompleted))
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
