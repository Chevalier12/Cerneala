using Cerneala.Timbre.Decoding;
using Concentus.Oggfile;
using Concentus.Structs;
using NAudio.Wave;
using NLayer;
using NVorbis;

namespace Cerneala.Timbre.Probe;

// The libraries' own whole-container paths, behind the same DecodedSource
// shape as the Timbre prototypes so both are measured identically.

internal sealed class NAudioWavCandidate : DecodedSource
{
    private readonly WaveFileReader reader;
    private readonly ISampleProvider samples;
    private float[] buffer = [];

    internal NAudioWavCandidate(Stream stream)
    {
        reader = new WaveFileReader(stream);
        samples = reader.ToSampleProvider();
    }

    internal override string Codec => "WAV";
    internal override int SampleRate => reader.WaveFormat.SampleRate;
    internal override int Channels => reader.WaveFormat.Channels;
    internal override long? LengthFrames => reader.SampleCount;

    internal override int Read(Span<float> destination, CancellationToken cancellationToken)
    {
        if (buffer.Length < destination.Length)
        {
            buffer = new float[destination.Length];
        }

        int read = samples.Read(buffer, 0, destination.Length - (destination.Length % Channels));
        buffer.AsSpan(0, read).CopyTo(destination);
        return read / Channels;
    }

    internal override void Seek(long frame, CancellationToken cancellationToken) =>
        reader.Position = frame * reader.WaveFormat.BlockAlign;

    public override void Dispose() => reader.Dispose();
}

internal sealed class NLayerMpegFileCandidate(Stream stream) : DecodedSource
{
    private readonly MpegFile file = new(stream);
    private float[] buffer = [];

    internal override string Codec => "MP3";
    internal override int SampleRate => file.SampleRate;
    internal override int Channels => file.Channels;
    internal override long? LengthFrames => file.Length / sizeof(float) / file.Channels;

    internal override int Read(Span<float> destination, CancellationToken cancellationToken)
    {
        if (buffer.Length < destination.Length)
        {
            buffer = new float[destination.Length];
        }

        int read = file.ReadSamples(buffer, 0, destination.Length - (destination.Length % Channels));
        buffer.AsSpan(0, read).CopyTo(destination);
        return read / Channels;
    }

    internal override void Seek(long frame, CancellationToken cancellationToken) =>
        file.Position = frame * sizeof(float) * file.Channels;

    public override void Dispose() => file.Dispose();
}

internal sealed class NVorbisReaderCandidate(Stream stream) : DecodedSource
{
    private readonly VorbisReader reader = new(stream, closeOnDispose: true) { ClipSamples = false };
    private float[] buffer = [];

    internal override string Codec => "Vorbis";
    internal override int SampleRate => reader.SampleRate;
    internal override int Channels => reader.Channels;
    internal override long? LengthFrames => reader.TotalSamples;

    internal override int Read(Span<float> destination, CancellationToken cancellationToken)
    {
        if (buffer.Length < destination.Length)
        {
            buffer = new float[destination.Length];
        }

        int read = reader.ReadSamples(buffer, 0, destination.Length - (destination.Length % Channels));
        buffer.AsSpan(0, read).CopyTo(destination);
        return read / Channels;
    }

    internal override void Seek(long frame, CancellationToken cancellationToken) => reader.SeekTo(frame);

    public override void Dispose() => reader.Dispose();
}

internal sealed class ConcentusOggFileCandidate : DecodedSource
{
    private readonly Stream stream;
    private readonly OpusOggReadStream reader;
    private readonly int channels;
    private short[] pending = [];
    private int pendingOffset;

    internal ConcentusOggFileCandidate(Stream stream, int channels)
    {
        this.stream = stream;
        this.channels = channels;
#pragma warning disable CS0618
        reader = new OpusOggReadStream(new OpusDecoder(48000, channels), stream);
#pragma warning restore CS0618
    }

    internal override string Codec => "Opus";
    internal override int SampleRate => 48000;
    internal override int Channels => channels;
    internal override long? LengthFrames => null;

    internal override int Read(Span<float> destination, CancellationToken cancellationToken)
    {
        int written = 0;
        int capacity = destination.Length - (destination.Length % channels);
        while (written < capacity)
        {
            if (pendingOffset == pending.Length)
            {
                if (!reader.HasNextPacket)
                {
                    break;
                }

                pending = reader.DecodeNextPacket() ?? [];
                pendingOffset = 0;
                continue;
            }

            int count = Math.Min(capacity - written, pending.Length - pendingOffset);
            for (int index = 0; index < count; index++)
            {
                destination[written + index] = pending[pendingOffset + index] / 32768f;
            }

            written += count;
            pendingOffset += count;
        }

        return written / channels;
    }

    internal override void Seek(long frame, CancellationToken cancellationToken)
    {
        reader.SeekTo(TimeSpan.FromSeconds(frame / 48000.0));
        pending = [];
        pendingOffset = 0;
    }

    public override void Dispose() => stream.Dispose();
}
