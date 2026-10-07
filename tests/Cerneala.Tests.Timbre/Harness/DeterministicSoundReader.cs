using Cerneala.Timbre;

namespace Cerneala.Tests.Timbre.Harness;

// Canonical PCM reader with a closed-form signal, so every frame of the mix can
// be predicted independently of the engine. Optional gates and faults model
// slow or failing I/O without timing assumptions.
internal sealed class DeterministicSoundReader : SoundReader
{
    private readonly DeterministicSoundSourceFactory? owner;
    private readonly Func<long, int, float> signal;
    private readonly long lengthFrames;
    private readonly bool reportLength;
    private long position;
    private int disposed;

    public DeterministicSoundReader(
        long lengthFrames,
        Func<long, int, float> signal,
        bool reportLength = true,
        int maxFramesPerRead = int.MaxValue,
        DeterministicSoundSourceFactory? owner = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(lengthFrames);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxFramesPerRead);
        this.lengthFrames = lengthFrames;
        this.signal = signal ?? throw new ArgumentNullException(nameof(signal));
        this.reportLength = reportLength;
        MaxFramesPerRead = maxFramesPerRead;
        this.owner = owner;
    }

    public int MaxFramesPerRead { get; }

    public long FailAtFrame { get; set; } = -1;

    public Exception? Failure { get; set; }

    public Func<CancellationToken, Task>? ReadGate { get; set; }

    private readonly List<(int Count, TaskCompletionSource Signal)> readWaiters = [];
    private int readCount;

    public int ReadCount => Volatile.Read(ref readCount);

    // Completes once ReadAsync has been entered `count` times (before any gate).
    public Task WaitForReadCountAsync(int count, TimeSpan? timeout = null)
    {
        TaskCompletionSource signal = new(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (readWaiters)
        {
            if (ReadCount >= count)
            {
                return Task.CompletedTask;
            }

            readWaiters.Add((count, signal));
        }

        return HarnessWait.WithTimeout(signal.Task, timeout, $"Reader was not read {count} times.");
    }

    // Synchronous zero-frame results without end of source, for contract tests.
    public bool ViolateReadinessContract { get; set; }

    public int SeekCount { get; private set; }

    public long FramesRead { get; private set; }

    public List<long> SeekTargets { get; } = [];

    public long Position => Interlocked.Read(ref position);

    public bool IsDisposed => Volatile.Read(ref disposed) != 0;

    public override long? LengthFrames => reportLength ? lengthFrames : null;

    public static float DefaultSignal(long frame, int channel) =>
        channel == 0 ? (frame % 97) / 194f : -((frame % 89) / 178f);

    public override async ValueTask<SoundReadResult> ReadAsync(Memory<float> destination, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (destination.Length % SoundRuntime.ChannelCount != 0)
        {
            throw new ArgumentException("Destination must hold complete stereo frames.", nameof(destination));
        }

        int count = Interlocked.Increment(ref readCount);
        List<TaskCompletionSource>? ready = null;
        lock (readWaiters)
        {
            for (int index = readWaiters.Count - 1; index >= 0; index--)
            {
                if (readWaiters[index].Count <= count)
                {
                    (ready ??= []).Add(readWaiters[index].Signal);
                    readWaiters.RemoveAt(index);
                }
            }
        }

        ready?.ForEach(signal => signal.TrySetResult());
        if (ViolateReadinessContract)
        {
            return new SoundReadResult(0, endOfSource: false);
        }

        if (ReadGate is not null)
        {
            await ReadGate(cancellationToken).ConfigureAwait(false);
        }

        cancellationToken.ThrowIfCancellationRequested();
        long start = Position;
        int frames = (int)Math.Min(Math.Min(destination.Length / SoundRuntime.ChannelCount, MaxFramesPerRead), lengthFrames - start);
        if (FailAtFrame >= 0 && start + frames > FailAtFrame)
        {
            throw Failure ?? new IOException($"Injected read failure at frame {FailAtFrame}.");
        }

        Fill(destination.Span, start, frames);
        Interlocked.Add(ref position, frames);
        FramesRead += frames;
        return new SoundReadResult(frames, start + frames >= lengthFrames);
    }

    private void Fill(Span<float> destination, long start, int frames)
    {
        for (int frame = 0; frame < frames; frame++)
        {
            destination[frame * 2] = signal(start + frame, 0);
            destination[(frame * 2) + 1] = signal(start + frame, 1);
        }
    }

    public override ValueTask SeekAsync(long frame, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        cancellationToken.ThrowIfCancellationRequested();
        if (frame < 0 || frame > lengthFrames)
        {
            throw new ArgumentOutOfRangeException(nameof(frame), frame, "Seek target is outside the source.");
        }

        SeekCount++;
        SeekTargets.Add(frame);
        Interlocked.Exchange(ref position, frame);
        return ValueTask.CompletedTask;
    }

    protected override void Dispose(bool disposing)
    {
        if (Interlocked.Exchange(ref disposed, 1) == 0)
        {
            owner?.OnReaderDisposed(this);
        }
    }
}

// Factory behind SoundSource.FromReader: one independent reader per open, with
// counters for opens and live readers.
internal sealed class DeterministicSoundSourceFactory
{
    private readonly object gate = new();
    private readonly List<DeterministicSoundReader> readers = [];
    private readonly Func<DeterministicSoundSourceFactory, DeterministicSoundReader> create;
    private int liveReaders;

    public DeterministicSoundSourceFactory(
        long lengthFrames,
        Func<long, int, float>? signal = null,
        bool reportLength = true,
        int maxFramesPerRead = int.MaxValue)
    {
        create = owner => new DeterministicSoundReader(lengthFrames, signal ?? DeterministicSoundReader.DefaultSignal, reportLength, maxFramesPerRead, owner);
    }

    public Action<DeterministicSoundReader>? Configure { get; set; }

    public int OpenCount
    {
        get
        {
            lock (gate)
            {
                return readers.Count;
            }
        }
    }

    public int LiveReaders => Volatile.Read(ref liveReaders);

    public IReadOnlyList<DeterministicSoundReader> Readers
    {
        get
        {
            lock (gate)
            {
                return readers.ToArray();
            }
        }
    }

    public SoundReader Open()
    {
        DeterministicSoundReader reader = create(this);
        Configure?.Invoke(reader);
        lock (gate)
        {
            readers.Add(reader);
        }

        Interlocked.Increment(ref liveReaders);
        return reader;
    }

    internal void OnReaderDisposed(DeterministicSoundReader reader) => Interlocked.Decrement(ref liveReaders);
}
