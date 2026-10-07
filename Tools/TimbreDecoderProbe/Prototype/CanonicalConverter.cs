using Concentus.Common;

namespace Cerneala.Timbre.Decoding;

// Converts a DecodedSource to Timbre's canonical PCM: interleaved stereo
// float32 at 48 kHz. Mono is copied to both channels (no downmix exists:
// sources are mono or stereo). Other rates go through the Speex resampler,
// aligned so that output frame j is source time j·rate/48000 and the length
// is ceil(sourceFrames·48000/rate). A seek restarts the resampler at a
// source frame on the rate's rational period, early enough that the filter
// history is real data, then discards to the exact target.
internal sealed class CanonicalConverter : IDisposable
{
    internal const int OutputRate = 48000;
    internal const int ResamplerQuality = 5;
    private const int SourceBlockFrames = 1024;

    private readonly DecodedSource source;
    private readonly SpeexResampler? resampler;
    private readonly int stepIn;
    private readonly int stepOut;
    private readonly int latency;
    private readonly float[] input;
    private readonly float[] output;
    private int inputStart;
    private int inputEnd;
    private int outputStart;
    private int outputEnd;
    private long sourceFramesRead;
    private long flushFramesFed;
    private bool sourceEnded;
    private long position;

    internal CanonicalConverter(DecodedSource source)
    {
        this.source = source;
        int channels = source.Channels;
        input = new float[SourceBlockFrames * channels];
        if (source.SampleRate != OutputRate)
        {
            int divisor = Gcd(source.SampleRate, OutputRate);
            stepIn = source.SampleRate / divisor;
            stepOut = OutputRate / divisor;
#pragma warning disable CS0618 // The managed resampler is required: the factory may bind a native speexdsp.
            resampler = new SpeexResampler(channels, source.SampleRate, OutputRate, ResamplerQuality);
#pragma warning restore CS0618
            latency = resampler.InputLatency;
            resampler.SkipZeroes();
            int outputCapacity = (int)(((long)SourceBlockFrames * OutputRate / source.SampleRate) + 16);
            output = new float[outputCapacity * channels];
        }
        else
        {
            output = input;
        }

        LengthFrames = source.LengthFrames is long frames ? OutputFrames(frames) : null;
    }

    internal long? LengthFrames { get; private set; }

    internal long Position => position;

    // Reads up to destination.Length / 2 canonical frames; 0 only at the end.
    internal int Read(Span<float> destination, CancellationToken cancellationToken)
    {
        int capacity = destination.Length / 2;
        int written = 0;
        while (written < capacity)
        {
            if (outputStart == outputEnd && !Produce(cancellationToken))
            {
                break;
            }

            int count = Math.Min(capacity - written, outputEnd - outputStart);
            if (LengthFrames is long length)
            {
                count = (int)Math.Min(count, length - position);
                if (count <= 0)
                {
                    break;
                }
            }

            Expand(output.AsSpan(outputStart * source.Channels, count * source.Channels), destination.Slice(written * 2, count * 2));
            outputStart += count;
            written += count;
            position += count;
        }

        return written;
    }

    internal void Seek(long frame, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(frame);
        if (LengthFrames is long length)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(frame, length);
        }

        long previous = position;
        try
        {
            SeekCore(frame, cancellationToken);
        }
        catch (ArgumentOutOfRangeException)
        {
            SeekCore(previous, cancellationToken);
            throw;
        }
    }

    public void Dispose()
    {
        resampler?.Dispose();
        source.Dispose();
    }

    private void SeekCore(long frame, CancellationToken cancellationToken)
    {
        inputStart = inputEnd = outputStart = outputEnd = 0;
        sourceEnded = false;
        flushFramesFed = 0;
        if (resampler is null)
        {
            source.Seek(frame, cancellationToken);
            sourceFramesRead = frame;
            position = frame;
            return;
        }

        long sourceTime = frame * stepIn / stepOut;
        long start = Math.Max(0, sourceTime - (2L * latency) - stepIn);
        start -= start % stepIn;
        source.Seek(start, cancellationToken);
        sourceFramesRead = start;
        resampler.ResetMem();
        resampler.SkipZeroes();
        position = start / stepIn * stepOut;
        while (position < frame)
        {
            if (outputStart == outputEnd && !Produce(cancellationToken))
            {
                throw new ArgumentOutOfRangeException(nameof(frame), frame, "The seek target is past the end of the source.");
            }

            int skip = (int)Math.Min(outputEnd - outputStart, frame - position);
            outputStart += skip;
            position += skip;
        }
    }

    // Refills `output`; false at the end.
    private bool Produce(CancellationToken cancellationToken)
    {
        int channels = source.Channels;
        if (resampler is null)
        {
            int frames = source.Read(input, cancellationToken);
            sourceFramesRead += frames;
            if (frames == 0)
            {
                LengthFrames ??= sourceFramesRead;
            }

            outputStart = 0;
            outputEnd = frames;
            return frames > 0;
        }

        while (true)
        {
            if (inputStart == inputEnd)
            {
                inputStart = 0;
                if (!sourceEnded)
                {
                    inputEnd = source.Read(input, cancellationToken);
                    sourceFramesRead += inputEnd;
                    if (inputEnd == 0)
                    {
                        sourceEnded = true;
                        LengthFrames ??= OutputFrames(sourceFramesRead);
                    }
                }

                if (sourceEnded)
                {
                    // Flush the filter with silence until the exact length is out.
                    if (position + (outputEnd - outputStart) >= LengthFrames || flushFramesFed > 2L * latency)
                    {
                        return outputEnd > outputStart;
                    }

                    inputEnd = Math.Min(SourceBlockFrames, latency);
                    input.AsSpan(0, inputEnd * channels).Clear();
                    flushFramesFed += inputEnd;
                }
            }

            int inLength = inputEnd - inputStart;
            int outLength = output.Length / channels;
            resampler.ProcessInterleaved(input.AsSpan(inputStart * channels, inLength * channels), ref inLength, output.AsSpan(), ref outLength);
            inputStart += inLength;
            outputStart = 0;
            outputEnd = outLength;
            if (outLength > 0)
            {
                return true;
            }
        }
    }

    private void Expand(ReadOnlySpan<float> samples, Span<float> destination)
    {
        if (source.Channels == 2)
        {
            samples.CopyTo(destination);
            return;
        }

        for (int index = 0; index < samples.Length; index++)
        {
            destination[index * 2] = samples[index];
            destination[(index * 2) + 1] = samples[index];
        }
    }

    private long OutputFrames(long sourceFrames) =>
        resampler is null ? sourceFrames : ((sourceFrames * stepOut) + stepIn - 1) / stepIn;

    private static int Gcd(int a, int b)
    {
        while (b != 0)
        {
            (a, b) = (b, a % b);
        }

        return a;
    }
}
