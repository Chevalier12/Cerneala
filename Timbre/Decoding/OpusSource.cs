using System.Buffers.Binary;
using Concentus.Structs;

namespace Cerneala.Timbre.Decoding;

// Ogg Opus (RFC 7845, channel mapping family 0) through Concentus' managed
// decoder. Pre-skip, end trimming, output gain and the 80 ms seek pre-roll
// are applied here; output is always 48 kHz.
internal sealed class OpusSource : DecodedSource
{
    private const int Rate = 48000;
    private const int SeekPrerollSamples = 3840;
    private const int MaxPacketSamples = 5760;

    private readonly Stream stream;
    private readonly string sourceName;
    private readonly OggStreamReader ogg;
    private readonly float[] decoded;
    private readonly long dataStart;
    private readonly long startGranule;
    private readonly long origin;
    private readonly long finalGranule;
    private readonly float gain;
    private OpusDecoder decoder;
    private long nextGranule;
    private long discardBefore;
    private int pendingStart;
    private int pendingEnd;
    private long position;

    internal OpusSource(Stream stream, string sourceName)
    {
        this.stream = stream;
        this.sourceName = sourceName;
        ogg = new OggStreamReader(stream, sourceName);
        if (!ogg.TryReadPacket(out ReadOnlySpan<byte> head, out _, out _) || head.Length < 19 || !head[..8].SequenceEqual("OpusHead"u8))
        {
            throw Unsupported(sourceName, "the Ogg stream does not carry Opus.");
        }

        if (head[8] >> 4 != 0)
        {
            throw Unsupported(sourceName, $"Opus header version {head[8]}.");
        }

        int channels = head[9];
        int preSkip = BinaryPrimitives.ReadUInt16LittleEndian(head[10..]);
        short outputGain = BinaryPrimitives.ReadInt16LittleEndian(head[16..]);
        int mappingFamily = head[18];
        if (mappingFamily != 0)
        {
            throw Unsupported(sourceName, $"Opus channel mapping family {mappingFamily} with {channels} channels (only mono/stereo family 0).");
        }

        ValidateFormat("Opus", sourceName, Rate, channels);
        if (!ogg.TryReadPacket(out ReadOnlySpan<byte> tags, out _, out _) || tags.Length < 8 || !tags[..8].SequenceEqual("OpusTags"u8))
        {
            throw Invalid(sourceName, "missing OpusTags header.");
        }

        dataStart = ogg.NextPageOffset;
        (finalGranule, _) = ogg.ReadFinalPage();
        Channels = channels;
        gain = outputGain == 0 ? 1f : MathF.Pow(10f, outputGain / (20f * 256f));
        decoded = new float[MaxPacketSamples * channels];
        decoder = CreateDecoder();

        // The first audio page's granule minus its packets' durations is the
        // granule of the first sample (normally 0).
        ogg.Restart(dataStart);
        long duration = 0;
        long firstPageGranule = -1;
        while (firstPageGranule < 0 && ogg.TryReadPacket(out ReadOnlySpan<byte> audio, out long granule, out _))
        {
            duration += PacketSamples(audio);
            firstPageGranule = granule;
        }

        startGranule = firstPageGranule < 0 ? 0 : firstPageGranule - duration;
        if (startGranule < 0)
        {
            throw Invalid(sourceName, "the first Opus page ends before its packets' duration.");
        }

        origin = Math.Max(preSkip, startGranule);
        LengthFrames = Math.Max(0, finalGranule - origin);
        Restart();
    }

    internal override string Codec => "Opus";

    internal override int SampleRate => Rate;

    internal override int Channels { get; }

    internal override long? LengthFrames { get; }

    internal override int Read(Span<float> destination, CancellationToken cancellationToken)
    {
        ThrowIfFaulted();
        int capacity = destination.Length / Channels;
        int written = 0;
        while (written < capacity)
        {
            if (pendingStart == pendingEnd)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (position >= LengthFrames || !DecodeNextPacket())
                {
                    break;
                }

                continue;
            }

            int count = Math.Min(capacity - written, pendingEnd - pendingStart);
            decoded.AsSpan(pendingStart * Channels, count * Channels).CopyTo(destination[(written * Channels)..]);
            pendingStart += count;
            written += count;
            position += count;
        }

        return written;
    }

    internal override void Seek(long frame, CancellationToken cancellationToken)
    {
        ThrowIfFaulted();
        ArgumentOutOfRangeException.ThrowIfNegative(frame);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(frame, LengthFrames!.Value);
        long previous = position;
        try
        {
            SeekCore(frame, cancellationToken);
        }
        catch
        {
            Return(SeekCore, previous, cancellationToken);
            throw;
        }
    }

    public override void Dispose() => stream.Dispose();

    private void SeekCore(long frame, CancellationToken cancellationToken)
    {
        long target = frame + origin;
        long preroll = target - SeekPrerollSamples;
        long page = preroll > startGranule ? ogg.FindPageAtOrBefore(preroll, dataStart, cancellationToken) : -1;
        if (page < 0)
        {
            Restart();
        }
        else
        {
            long granule = ogg.GranuleAt(page);
            ogg.RestartAfterPage(page);
            decoder = CreateDecoder();
            nextGranule = granule;
            pendingStart = pendingEnd = 0;
        }

        discardBefore = target;
        position = frame;
        while (nextGranule <= target && nextGranule < finalGranule)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!DecodeNextPacket())
            {
                break;
            }
        }
    }

    private void Restart()
    {
        ogg.Restart(dataStart);
        decoder = CreateDecoder();
        nextGranule = startGranule;
        discardBefore = origin;
        pendingStart = pendingEnd = 0;
        position = 0;
    }

    private bool DecodeNextPacket()
    {
        if (!ogg.TryReadPacket(out ReadOnlySpan<byte> packet, out long granule, out bool endOfStream))
        {
            return false;
        }

        int samples;
        try
        {
            samples = decoder.Decode(packet, decoded, MaxPacketSamples, false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw Invalid(sourceName, $"an Opus packet at granule {nextGranule} could not be decoded.", exception);
        }

        long packetStart = nextGranule;
        nextGranule += samples;
        if (granule >= 0 && !endOfStream && granule != nextGranule)
        {
            throw Invalid(sourceName, $"Opus page granule {granule} disagrees with the decoded position {nextGranule}.");
        }

        long keepFrom = Math.Max(discardBefore, origin);
        pendingStart = (int)Math.Clamp(keepFrom - packetStart, 0, samples);
        pendingEnd = (int)Math.Clamp(finalGranule - packetStart, 0, samples);
        if (pendingEnd < pendingStart)
        {
            pendingEnd = pendingStart;
        }

        if (gain != 1f)
        {
            foreach (ref float sample in decoded.AsSpan(pendingStart * Channels, (pendingEnd - pendingStart) * Channels))
            {
                sample *= gain;
            }
        }

        return true;
    }

    private int PacketSamples(ReadOnlySpan<byte> packet)
    {
        try
        {
            return OpusPacketInfo.GetNumSamples(decoder, packet);
        }
        catch (Exception exception)
        {
            throw Invalid(sourceName, "an Opus packet has an invalid table of contents.", exception);
        }
    }

#pragma warning disable CS0618 // The managed decoder is required: the factory may bind a native libopus.
    private OpusDecoder CreateDecoder() => new(Rate, Channels);
#pragma warning restore CS0618
}
