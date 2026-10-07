using Cerneala.Timbre.Engine;

namespace Cerneala.Timbre.Decoding;

// The content-detected formats registered with SoundDecoders. Each reserves
// the live memory of its reader (demultiplexer, decoder and canonical
// converter) before building it. The reservations are at least twice the
// largest live heap measured per reader in the stage-0 footprint probe
// (benchmarks/Cerneala.Benchmarks/results/2026-10-07-timbre-decoding-stage0).
internal static class SoundFormats
{
    internal const long WavReaderBytes = 256 * 1024;
    internal const long Mp3ReaderBytes = 512 * 1024;
    internal const long OpusReaderBytes = 512 * 1024;
    internal const long VorbisReaderBytes = 2 * 1024 * 1024;

    internal static readonly ISoundDecoder[] All = [new Wav(), new Ogg(), new Mp3()];

    private static SoundReader Build(SoundMemoryBudget budget, long bytes, Func<DecodedSource> create)
    {
        budget.Reserve(bytes);
        return new DecodedSoundReader(new CanonicalConverter(create()));
    }

    private sealed class Wav : ISoundDecoder
    {
        public int HeaderLength => 4;

        // RIFX/RF64/BW64 are recognized here and refused by WavSource.
        public bool CanDecode(ReadOnlySpan<byte> header) =>
            header.SequenceEqual("RIFF"u8) || header.SequenceEqual("RIFX"u8) || header.SequenceEqual("RF64"u8) || header.SequenceEqual("BW64"u8);

        public SoundReader Create(Stream stream, string sourceName, SoundMemoryBudget budget) =>
            Build(budget, WavReaderBytes, () => new WavSource(stream, sourceName));
    }

    private sealed class Ogg : ISoundDecoder
    {
        public int HeaderLength => 27 + 255 + 8;

        public bool CanDecode(ReadOnlySpan<byte> header) => header.Length >= 27 && header[..4].SequenceEqual("OggS"u8);

        // The first packet of the first page names the codec.
        public SoundReader Create(Stream stream, string sourceName, SoundMemoryBudget budget)
        {
            byte[] header = new byte[HeaderLength];
            stream.Position = 0;
            int length = stream.ReadAtLeast(header, header.Length, throwOnEndOfStream: false);
            stream.Position = 0;
            int packet = 27 + header[26];
            ReadOnlySpan<byte> first = packet < length ? header.AsSpan(packet, length - packet) : default;
            if (first.Length >= 7 && first[0] == 1 && first[1..7].SequenceEqual("vorbis"u8))
            {
                budget.Reserve(VorbisReaderBytes);
                return new DecodedSoundReader(new CanonicalConverter(new VorbisSource(stream, sourceName, budget, VorbisReaderBytes)));
            }

            if (first.Length >= 8 && first[..8].SequenceEqual("OpusHead"u8))
            {
                return Build(budget, OpusReaderBytes, () => new OpusSource(stream, sourceName));
            }

            throw new SoundException(
                SoundErrorKind.UnsupportedFormat,
                $"Sound source '{sourceName}' is not supported: the Ogg stream carries neither Vorbis nor Opus.");
        }
    }

    private sealed class Mp3 : ISoundDecoder
    {
        public int HeaderLength => 4;

        // An ID3v2 tag, or an MPEG audio frame sync; Mp3Source validates the
        // frame sequence and refuses Layer I/II.
        public bool CanDecode(ReadOnlySpan<byte> header) =>
            header.Length >= 3 && header[..3].SequenceEqual("ID3"u8) ||
            header.Length >= 2 && header[0] == 0xFF && (header[1] & 0xE0) == 0xE0;

        public SoundReader Create(Stream stream, string sourceName, SoundMemoryBudget budget) =>
            Build(budget, Mp3ReaderBytes, () => new Mp3Source(stream, sourceName));
    }
}
