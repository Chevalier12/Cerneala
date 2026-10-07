using NVorbis;
using NVorbis.Contracts;
using IPacketProvider = NVorbis.Contracts.IPacketProvider;

namespace Cerneala.Timbre.Decoding;

// Ogg Vorbis through NVorbis' StreamDecoder, fed by the bounded Ogg reader
// instead of NVorbis' own container reader (which indexes every page and
// reads the whole file to report the length).
internal sealed class VorbisSource : DecodedSource
{
    private readonly Stream stream;
    private readonly string sourceName;
    private readonly PacketProvider provider;
    private readonly StreamDecoder decoder;

    internal VorbisSource(Stream stream, string sourceName)
    {
        this.stream = stream;
        this.sourceName = sourceName;
        provider = new PacketProvider(new OggStreamReader(stream, sourceName), sourceName);
        try
        {
            decoder = new StreamDecoder(provider) { ClipSamples = false };
        }
        catch (Exception exception) when (exception is not SoundException and not OperationCanceledException)
        {
            throw Invalid(sourceName, "the Vorbis headers could not be decoded.", exception);
        }

        ValidateFormat("Vorbis", sourceName, decoder.SampleRate, decoder.Channels);
        SampleRate = decoder.SampleRate;
        Channels = decoder.Channels;
        LengthFrames = provider.GetGranuleCount();
    }

    internal override string Codec => "Vorbis";

    internal override int SampleRate { get; }

    internal override int Channels { get; }

    internal override long? LengthFrames { get; }

    internal override int Read(Span<float> destination, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        provider.CancellationToken = cancellationToken;
        int samples = destination.Length - (destination.Length % Channels);
        try
        {
            return decoder.Read(destination, 0, samples) / Channels;
        }
        catch (Exception exception) when (exception is not SoundException and not OperationCanceledException)
        {
            throw Invalid(sourceName, "a Vorbis packet could not be decoded.", exception);
        }
    }

    internal override void Seek(long frame, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(frame);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(frame, LengthFrames!.Value);
        provider.CancellationToken = cancellationToken;
        try
        {
            decoder.SeekTo(frame);
        }
        catch (Exception exception) when (exception is not SoundException and not OperationCanceledException)
        {
            throw Invalid(sourceName, $"seeking to frame {frame} failed.", exception);
        }
    }

    public override void Dispose() => stream.Dispose();

    private sealed class PacketProvider : IPacketProvider
    {
        private readonly OggStreamReader ogg;
        private readonly string sourceName;
        private readonly long dataStart;
        private readonly long finalGranule;
        private readonly int maxPacketSamples;
        private readonly Queue<Packet> queued = new();

        internal PacketProvider(OggStreamReader ogg, string sourceName)
        {
            this.ogg = ogg;
            this.sourceName = sourceName;
            if (!ogg.TryReadPacket(out ReadOnlySpan<byte> identification, out _, out _) ||
                identification.Length < 30 || identification[0] != 1 || !identification[1..7].SequenceEqual("vorbis"u8))
            {
                throw new SoundException(SoundErrorKind.UnsupportedFormat, $"Sound source '{sourceName}' is not supported: the Ogg stream does not carry Vorbis.");
            }

            maxPacketSamples = 1 << (identification[28] >> 4);
            queued.Enqueue(new Packet(identification, -1, false));
            for (int header = 0; header < 2; header++)
            {
                if (!ogg.TryReadPacket(out ReadOnlySpan<byte> data, out long granule, out bool end))
                {
                    throw new SoundException(SoundErrorKind.InvalidData, $"Sound source '{sourceName}' is invalid: missing Vorbis header packets.");
                }

                queued.Enqueue(new Packet(data, granule, end));
            }

            dataStart = ogg.NextPageOffset;
            (finalGranule, _) = ogg.ReadFinalPage();
            ogg.Restart(dataStart);
        }

        internal CancellationToken CancellationToken { get; set; }

        public int StreamSerial => ogg.Serial;

        public bool CanSeek => true;

        public long GetGranuleCount() => finalGranule;

        public IPacket? GetNextPacket() => queued.Count > 0 ? queued.Dequeue() : ReadPacket();

        public IPacket? PeekNextPacket()
        {
            if (queued.Count == 0 && ReadPacket() is Packet packet)
            {
                queued.Enqueue(packet);
            }

            return queued.Count > 0 ? queued.Peek() : null;
        }

        // Positions the provider so the next two packets are the pre-roll
        // packet and the packet whose output contains `granulePos`; returns
        // the granule at which that output starts.
        public long SeekTo(long granulePos, int preRoll, GetPacketGranuleCount getPacketGranuleCount)
        {
            queued.Clear();
            long page = granulePos - maxPacketSamples > 0
                ? ogg.FindPageAtOrBefore(granulePos - maxPacketSamples, dataStart, CancellationToken)
                : -1;
            Packet? previous;
            long previousGranule;
            if (page < 0)
            {
                ogg.Restart(dataStart);
                previous = ReadPacket();
                previousGranule = 0;
            }
            else
            {
                long pageGranule = ogg.GranuleAt(page);
                ogg.RestartAfterPage(page);
                previous = ReadPacket();
                previousGranule = previous is null ? pageGranule
                    : previous.GranulePosition is long known ? known
                    : pageGranule + Count(previous, getPacketGranuleCount);
            }

            if (previous is null || preRoll == 0 && granulePos == 0)
            {
                if (previous is not null)
                {
                    previous.Reset();
                    queued.Enqueue(previous);
                }

                return 0;
            }

            while (true)
            {
                CancellationToken.ThrowIfCancellationRequested();
                Packet? next = ReadPacket();
                if (next is null)
                {
                    previous.Reset();
                    queued.Enqueue(previous);
                    return previousGranule;
                }

                long nextGranule = next.GranulePosition is long known && known >= 0
                    ? known
                    : previousGranule + Count(next, getPacketGranuleCount);
                if (nextGranule > granulePos)
                {
                    previous.Reset();
                    next.Reset();
                    queued.Enqueue(previous);
                    queued.Enqueue(next);
                    return previousGranule;
                }

                previous = next;
                previousGranule = nextGranule;
            }
        }

        private static int Count(Packet packet, GetPacketGranuleCount getPacketGranuleCount)
        {
            int count = getPacketGranuleCount(packet);
            packet.Reset();
            return count;
        }

        private Packet? ReadPacket()
        {
            CancellationToken.ThrowIfCancellationRequested();
            return ogg.TryReadPacket(out ReadOnlySpan<byte> data, out long granule, out bool end)
                ? new Packet(data, granule, end)
                : null;
        }
    }

    private sealed class Packet : DataPacket
    {
        private readonly byte[] data;
        private int cursor;

        internal Packet(ReadOnlySpan<byte> data, long granule, bool endOfStream)
        {
            this.data = data.ToArray();
            GranulePosition = granule >= 0 ? granule : null;
            IsEndOfStream = endOfStream;
        }

        protected override int TotalBits => data.Length * 8;

        protected override int ReadNextByte() => cursor < data.Length ? data[cursor++] : -1;

        public override void Reset()
        {
            cursor = 0;
            base.Reset();
        }
    }
}
