using System.Buffers.Binary;
using Cerneala.Timbre;

namespace Cerneala.Tests.Timbre.Decoding;

// RFC 7845 section 4: when the first audio page is also the end-of-stream
// page, its granule may be smaller than its packets' duration (end trimming);
// the initial granule is then 0. Streams are written from the committed
// corpus' Opus packets with a hand-written Ogg page writer.
public sealed class TimbreOpusEndTrimTests : IDisposable
{
    private const int PacketCount = 3;

    private const int SamplesPerPacket = 960;

    private readonly string folder = Path.Combine(Path.GetTempPath(), "cerneala-timbre-opus-eos", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(500)]
    [InlineData(1000)]
    public void AnOpusStreamWhoseOnlyAudioPageIsTheEndOfStreamPageIsDecodedWithEndTrimming(int trimmed)
    {
        (byte[] head, _, _, _) = ReadCorpusPackets();
        int preSkip = BinaryPrimitives.ReadUInt16LittleEndian(head.AsSpan(10));
        long granule = (PacketCount * SamplesPerPacket) - trimmed;
        string path = WriteSinglePageStream($"single-eos-page-{trimmed}.opus", granule, endOfStream: true);

        long? declared = null;
        float[] pcm = [];
        Exception? failure = Record.Exception(() =>
        {
            using TimbreReader reader = DecodingCorpus.Open(path);
            declared = reader.LengthFrames;
            pcm = DecodingCorpus.DecodeAll(reader);
        });

        Assert.Null(failure);
        Assert.Equal(granule - preSkip, declared);
        Assert.Equal(granule - preSkip, pcm.Length / 2);
    }

    [Fact]
    public void AFirstAudioPageThatEndsBeforeItsPacketsDurationIsInvalidWhenItIsNotTheEndOfStreamPage()
    {
        string path = WriteSinglePageStream("single-page-trimmed-without-eos.opus", (PacketCount * SamplesPerPacket) - 500, endOfStream: false);

        TimbreException failure = Assert.Throws<TimbreException>(() => DecodingCorpus.Open(path).Dispose());

        Assert.Equal(TimbreErrorKind.InvalidData, failure.Kind);
    }

    [Fact]
    public void AnEndOfStreamFirstAudioPageWhoseGranuleIsBelowThePreSkipIsInvalid()
    {
        (byte[] head, _, _, _) = ReadCorpusPackets();
        int preSkip = BinaryPrimitives.ReadUInt16LittleEndian(head.AsSpan(10));
        string path = WriteSinglePageStream("single-eos-page-below-pre-skip.opus", preSkip - 1, endOfStream: true);

        TimbreException failure = Assert.Throws<TimbreException>(() => DecodingCorpus.Open(path).Dispose());

        Assert.Equal(TimbreErrorKind.InvalidData, failure.Kind);
    }

    private string WriteSinglePageStream(string name, long granule, bool endOfStream)
    {
        (byte[] head, byte[] tags, List<byte[]> packets, uint serial) = ReadCorpusPackets();
        string path = Path.Combine(folder, name);
        Directory.CreateDirectory(folder);
        using FileStream file = File.Create(path);
        file.Write(Page([head], 0, flags: 0x02, serial, sequence: 0));
        file.Write(Page([tags], 0, flags: 0, serial, sequence: 1));
        file.Write(Page(packets.Take(PacketCount).ToList(), granule, flags: endOfStream ? (byte)0x04 : (byte)0, serial, sequence: 2));
        return path;
    }

    // OpusHead, OpusTags and the first audio packets of a committed corpus file.
    private static (byte[] Head, byte[] Tags, List<byte[]> Packets, uint Serial) ReadCorpusPackets()
    {
        byte[] data = File.ReadAllBytes(DecodingCorpus.PathOf("opus-stereo-48000-96k.opus"));
        List<byte[]> packets = [];
        List<byte> partial = [];
        uint serial = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(14));
        int offset = 0;
        while (offset < data.Length && packets.Count < PacketCount + 2)
        {
            Assert.True(data.AsSpan(offset, 4).SequenceEqual("OggS"u8), "The corpus file is not an Ogg stream.");
            int segments = data[offset + 26];
            int body = offset + 27 + segments;
            for (int index = 0; index < segments; index++)
            {
                int lacing = data[offset + 27 + index];
                partial.AddRange(data.AsSpan(body, lacing).ToArray());
                body += lacing;
                if (lacing < 255)
                {
                    packets.Add([.. partial]);
                    partial.Clear();
                }
            }

            offset = body;
        }

        Assert.True(packets.Count >= PacketCount + 2, "The corpus file has too few packets.");
        return (packets[0], packets[1], packets.Skip(2).ToList(), serial);
    }

    private static byte[] Page(List<byte[]> packets, long granule, byte flags, uint serial, uint sequence)
    {
        List<byte> segments = [];
        List<byte> body = [];
        foreach (byte[] packet in packets)
        {
            int remaining = packet.Length;
            while (remaining >= 255)
            {
                segments.Add(255);
                remaining -= 255;
            }

            segments.Add((byte)remaining);
            body.AddRange(packet);
        }

        Assert.True(segments.Count <= 255, "The page needs more than 255 segments.");
        byte[] page = new byte[27 + segments.Count + body.Count];
        "OggS"u8.CopyTo(page);
        page[5] = flags;
        BinaryPrimitives.WriteInt64LittleEndian(page.AsSpan(6), granule);
        BinaryPrimitives.WriteUInt32LittleEndian(page.AsSpan(14), serial);
        BinaryPrimitives.WriteUInt32LittleEndian(page.AsSpan(18), sequence);
        page[26] = (byte)segments.Count;
        segments.CopyTo(page, 27);
        body.CopyTo(page, 27 + segments.Count);
        BinaryPrimitives.WriteUInt32LittleEndian(page.AsSpan(22), Crc(page));
        return page;
    }

    // Ogg CRC-32: polynomial 0x04C11DB7, initial value 0, no reflection,
    // computed with the checksum field zeroed.
    private static uint Crc(byte[] page)
    {
        uint crc = 0;
        foreach (byte value in page)
        {
            crc ^= (uint)value << 24;
            for (int bit = 0; bit < 8; bit++)
            {
                crc = (crc & 0x80000000) != 0 ? (crc << 1) ^ 0x04C11DB7 : crc << 1;
            }
        }

        return crc;
    }
}
