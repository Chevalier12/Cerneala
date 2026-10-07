namespace Cerneala.Timbre.Probe;

// Observes every I/O a decoder performs: read calls, bytes, seeks, the
// furthest byte read and disposal.
internal sealed class CountingStream(Stream inner) : Stream
{
    public long ReadCalls { get; private set; }
    public long BytesRead { get; private set; }
    public long SeekCalls { get; private set; }
    public long FurthestByte { get; private set; }
    public bool Disposed { get; private set; }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length => inner.Length;

    public override long Position
    {
        get => inner.Position;
        set
        {
            SeekCalls++;
            inner.Position = value;
        }
    }

    public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

    public override int Read(Span<byte> buffer)
    {
        int read = inner.Read(buffer);
        ReadCalls++;
        BytesRead += read;
        FurthestByte = Math.Max(FurthestByte, inner.Position);
        return read;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        SeekCalls++;
        return inner.Seek(offset, origin);
    }

    public override void Flush()
    {
    }

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing && !Disposed)
        {
            Disposed = true;
            inner.Dispose();
        }

        base.Dispose(disposing);
    }
}
