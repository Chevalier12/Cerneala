namespace Cerneala.Timbre.Decoding;

// Codec-level PCM at the source's own rate and channel count (1 or 2),
// interleaved float. Trimming (gapless metadata, Opus pre-skip/end trim) is
// already applied: frame 0 is the first audible frame of the source.
internal abstract class DecodedSource : IDisposable
{
    internal const int MinSampleRate = 8000;
    internal const int MaxSampleRate = 192000;

    internal abstract string Codec { get; }

    internal abstract int SampleRate { get; }

    internal abstract int Channels { get; }

    // Total frames after trimming, or null while unknown.
    internal abstract long? LengthFrames { get; }

    // Reads up to destination.Length / Channels frames; 0 only at the end.
    internal abstract int Read(Span<float> destination, CancellationToken cancellationToken);

    // Positions the next read exactly at `frame` (decoded-frame precision).
    // Throws ArgumentOutOfRangeException when `frame` is past the end; the
    // position is then unchanged.
    internal abstract void Seek(long frame, CancellationToken cancellationToken);

    public abstract void Dispose();

    protected static void ValidateFormat(string codec, string sourceName, int sampleRate, int channels)
    {
        if (channels is < 1 or > 2)
        {
            throw new TimbreException(
                TimbreErrorKind.UnsupportedFormat,
                $"{codec} source '{sourceName}' has {channels} channels; only mono and stereo are supported.");
        }

        if (sampleRate is < MinSampleRate or > MaxSampleRate)
        {
            throw new TimbreException(
                TimbreErrorKind.UnsupportedFormat,
                $"{codec} source '{sourceName}' has a {sampleRate} Hz sample rate; 8–192 kHz is supported.");
        }
    }

    protected static TimbreException Invalid(string sourceName, string detail, Exception? inner = null) =>
        new(TimbreErrorKind.InvalidData, $"Timbre source '{sourceName}' is invalid: {detail}", inner);

    protected static TimbreException Unsupported(string sourceName, string detail) =>
        new(TimbreErrorKind.UnsupportedFormat, $"Timbre source '{sourceName}' is not supported: {detail}");
}
