namespace Cerneala.Timbre.Engine;

// Source PCM for one playback. Read, RequestSeek, and TryTakeSeekOutcome run on
// the mixer thread only; workers publish through the feed's own signals.
internal abstract class SoundFeed
{
    internal abstract long? LengthFrames { get; }

    // Processed source position in frames, owned by the mixer.
    internal long Position { get; private protected set; }

    // The non-looping source ended and every frame was read.
    internal bool Ended { get; private protected set; }

    internal long LoopWraps { get; private protected set; }

    internal abstract bool HasData { get; }

    // Mixer thread: a whole block of source frames (or the rest of the source)
    // can be read now without padding.
    internal virtual bool HasBlock(int frames) => true;

    internal abstract int Read(Span<float> destination, int frames);

    internal abstract void RequestSeek(long frame, int generation);

    internal abstract bool TryTakeSeekOutcome(int generation, out Exception? error);

    internal abstract Task WhenSettledAsync();

    // Idempotent. Completes `stopped` once worker resources are released.
    internal abstract void Stop();

    internal abstract Task Stopped { get; }
}

internal sealed class PreloadedFeed : SoundFeed
{
    private readonly SoundPayloadCache cache;
    private readonly SoundPayloadCache.Entry payload;
    private readonly bool loop;
    private int seekGeneration;
    private Exception? seekError;
    private int stopped;

    internal PreloadedFeed(SoundPayloadCache cache, SoundPayloadCache.Entry payload, bool loop)
    {
        this.cache = cache;
        this.payload = payload;
        this.loop = loop;
    }

    internal override long? LengthFrames => payload.Frames;

    internal override bool HasData => true;

    internal override Task Stopped => Task.CompletedTask;

    internal override int Read(Span<float> destination, int frames)
    {
        float[] samples = payload.Samples;
        long length = payload.Frames;
        int written = 0;
        while (written < frames)
        {
            if (Position >= length)
            {
                if (!loop || length == 0)
                {
                    Ended = true;
                    break;
                }

                Position = 0;
                LoopWraps++;
            }

            int count = (int)Math.Min(frames - written, length - Position);
            samples.AsSpan((int)(Position * 2), count * 2).CopyTo(destination[(written * 2)..]);
            written += count;
            Position += count;
        }

        return written;
    }

    internal override void RequestSeek(long frame, int generation)
    {
        seekGeneration = generation;
        if (frame > payload.Frames)
        {
            seekError = new ArgumentOutOfRangeException(nameof(frame), frame, "The seek target is beyond the end of the source.");
            return;
        }

        seekError = null;
        Position = frame;
        Ended = false;
    }

    internal override bool TryTakeSeekOutcome(int generation, out Exception? error)
    {
        error = seekError;
        return seekGeneration == generation;
    }

    internal override Task WhenSettledAsync() => Task.CompletedTask;

    internal override void Stop()
    {
        if (Interlocked.Exchange(ref stopped, 1) == 0)
        {
            cache.Release(payload);
        }
    }
}
