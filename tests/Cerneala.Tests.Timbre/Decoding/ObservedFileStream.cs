using Cerneala.Timbre;

namespace Cerneala.Tests.Timbre.Decoding;

// A read-only file stream that a test observes and steers: it counts bytes
// and seeks, records the threads that read, blocks reads that reach a byte
// barrier until released (signalling that it is blocked), and can fail a
// read at a byte offset. Decoders read synchronously, so a blocked read holds
// the source pump exactly like slow storage would.
internal sealed class ObservedFileStream : Stream
{
    private readonly FileStream inner;
    private readonly ManualResetEventSlim open = new(initialState: true);
    private readonly object gate = new();
    private TaskCompletionSource blocked = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private long barrierFrom = long.MaxValue;
    private long barrierTo = long.MaxValue;
    private long barrierAfterBytes;
    private long bytesRead;
    private long furthest;
    private int disposed;

    public ObservedFileStream(string path)
    {
        inner = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096);
    }

    public long BytesRead => Interlocked.Read(ref bytesRead);

    public long FurthestByte => Interlocked.Read(ref furthest);

    public long FailAtOffset { get; set; } = -1;

    public bool IsDisposed => Volatile.Read(ref disposed) != 0;

    public List<string?> ReaderThreads { get; } = [];

    public HashSet<int> ReaderThreadIds { get; } = [];

    public override bool CanRead => true;

    public override bool CanSeek => true;

    public override bool CanWrite => false;

    public override long Length => inner.Length;

    public override long Position
    {
        get => inner.Position;
        set => inner.Position = value;
    }

    // Reads that touch bytes in [from, to), once `afterBytes` bytes have been
    // read in total, wait until Release().
    public void BlockAt(long from, long to = long.MaxValue, long afterBytes = 0)
    {
        lock (gate)
        {
            barrierFrom = from;
            barrierTo = to;
            barrierAfterBytes = afterBytes;
            blocked = new(TaskCreationOptions.RunContinuationsAsynchronously);
            open.Reset();
        }
    }

    public void Release()
    {
        lock (gate)
        {
            barrierFrom = long.MaxValue;
            barrierTo = long.MaxValue;
            open.Set();
        }
    }

    // Completes once a read is waiting at the barrier.
    public Task WhenBlocked
    {
        get
        {
            lock (gate)
            {
                return blocked.Task;
            }
        }
    }

    public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

    public override int Read(Span<byte> buffer)
    {
        lock (ReaderThreads)
        {
            ReaderThreads.Add(Thread.CurrentThread.Name);
            ReaderThreadIds.Add(Environment.CurrentManagedThreadId);
        }

        long start = inner.Position;
        TaskCompletionSource? signal = null;
        lock (gate)
        {
            if (start + buffer.Length > barrierFrom && start < barrierTo && BytesRead >= barrierAfterBytes)
            {
                signal = blocked;
            }
        }

        if (signal is not null)
        {
            signal.TrySetResult();
            open.Wait();
        }

        if (FailAtOffset >= 0 && start + buffer.Length > FailAtOffset)
        {
            throw new IOException($"Injected read failure at byte {FailAtOffset}.");
        }

        int read = inner.Read(buffer);
        Interlocked.Add(ref bytesRead, read);
        long end = start + read;
        long current;
        while ((current = Interlocked.Read(ref furthest)) < end && Interlocked.CompareExchange(ref furthest, end, current) != current)
        {
        }

        return read;
    }

    public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);

    public override void Flush()
    {
    }

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing && Interlocked.Exchange(ref disposed, 1) == 0)
        {
            open.Set();
            inner.Dispose();
        }

        base.Dispose(disposing);
    }
}

// A streaming clip over a corpus file whose every playback stream is observed.
internal sealed class ObservedSource
{
    private readonly List<ObservedFileStream> streams = [];
    private readonly TaskCompletionSource opened = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public ObservedSource(string path, bool loop = false, TimbreLoading loading = TimbreLoading.Streaming)
    {
        Path = path;
        Clip = new TimbreSound(
            TimbreSource.FromStream(_ =>
            {
                ObservedFileStream stream = new(path);
                Configure?.Invoke(stream);
                lock (streams)
                {
                    streams.Add(stream);
                }

                opened.TrySetResult();

                return stream;
            }, System.IO.Path.GetFileName(path)),
            loop: loop,
            loading: loading);
    }

    public string Path { get; }

    // Completes when the first playback stream has been created.
    public Task Opened => opened.Task;

    public TimbreSound Clip { get; }

    // Applied to each new stream before the decoder sees it.
    public Action<ObservedFileStream>? Configure { get; set; }

    public IReadOnlyList<ObservedFileStream> Streams
    {
        get
        {
            lock (streams)
            {
                return streams.ToArray();
            }
        }
    }

    public ObservedFileStream Single => Streams.Single();

    public float[] Decode()
    {
        using TimbreReader reader = DecodingCorpus.Open(Path);
        return DecodingCorpus.DecodeAll(reader);
    }
}
