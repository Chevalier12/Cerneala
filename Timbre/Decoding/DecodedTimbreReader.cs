namespace Cerneala.Timbre.Decoding;

// The TimbreReader the runtime sees for a decoded file or stream: one per
// playback, called sequentially on a worker. Decoding is synchronous and
// cancellation is checked between frames/packets.
internal sealed class DecodedTimbreReader(CanonicalConverter converter) : TimbreReader
{
    // Null until known; a source without a declared length reports it once
    // its end has been decoded.
    public override long? LengthFrames => converter.LengthFrames;

    public override ValueTask<TimbreReadResult> ReadAsync(Memory<float> destination, CancellationToken cancellationToken)
    {
        int frames = converter.Read(destination.Span, cancellationToken);
        bool end = frames == 0 || converter.LengthFrames is long length && converter.Position >= length;
        return ValueTask.FromResult(new TimbreReadResult(frames, end));
    }

    public override ValueTask SeekAsync(long frame, CancellationToken cancellationToken)
    {
        converter.Seek(frame, cancellationToken);
        return ValueTask.CompletedTask;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            converter.Dispose();
        }
    }
}
