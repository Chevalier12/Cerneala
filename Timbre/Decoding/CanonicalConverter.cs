using System.Runtime.ExceptionServices;
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
    private SpeexResampler? resampler;
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
    private ExceptionDispatchInfo? fault;

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
            resampler = CreateResampler(channels, source.SampleRate);
            latency = resampler.InputLatency;
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
        fault?.Throw();
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
        fault?.Throw();
        ArgumentOutOfRangeException.ThrowIfNegative(frame);
        if (LengthFrames is long length)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(frame, length);
        }

        long previous = position;
        long start = SourceStart(frame);
        // A source seek that throws leaves the source at its old position, so
        // the frames buffered here still continue it.
        source.Seek(start, cancellationToken);
        try
        {
            Restart(frame, start, cancellationToken);
        }
        catch
        {
            Return(previous, cancellationToken);
            throw;
        }
    }

    public void Dispose()
    {
        resampler?.Dispose();
        source.Dispose();
    }

    // After a restart failed part-way: seeks back to `previous`. If that fails
    // too, the state is unknown, so every later Read and Seek throws the error
    // that prevented the return.
    private void Return(long previous, CancellationToken cancellationToken)
    {
        try
        {
            long start = SourceStart(previous);
            source.Seek(start, cancellationToken);
            Restart(previous, start, cancellationToken);
        }
        catch (Exception exception)
        {
            fault = ExceptionDispatchInfo.Capture(exception);
        }
    }

    // The source frame a seek to `frame` restarts from.
    private long SourceStart(long frame)
    {
        if (resampler is null)
        {
            return frame;
        }

        long sourceTime = frame * stepIn / stepOut;
        long start = Math.Max(0, sourceTime - (2L * latency) - stepIn);
        return start - (start % stepIn);
    }

    // Restarts conversion at `frame` with the source positioned at `start`.
    private void Restart(long frame, long start, CancellationToken cancellationToken)
    {
        inputStart = inputEnd = outputStart = outputEnd = 0;
        sourceEnded = false;
        flushFramesFed = 0;
        sourceFramesRead = start;
        if (resampler is null)
        {
            position = frame;
            return;
        }

        // A new resampler, not ResetMem(): Concentus 2.2.2's ResetMem leaves
        // history behind in the second channel (stage-2 evidence).
        resampler.Dispose();
        resampler = CreateResampler(source.Channels, source.SampleRate);
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

    private static SpeexResampler CreateResampler(int channels, int sampleRate)
    {
#pragma warning disable CS0618 // The managed resampler is required: the factory may bind a native speexdsp.
        SpeexResampler created = new(channels, sampleRate, OutputRate, ResamplerQuality);
#pragma warning restore CS0618
        created.SkipZeroes();
        return created;
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
