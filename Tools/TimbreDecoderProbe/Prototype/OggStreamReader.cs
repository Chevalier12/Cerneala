using System.Buffers.Binary;

namespace Cerneala.Timbre.Decoding;

// Bounded demultiplexer for one Ogg logical stream (RFC 3533). It keeps one
// page and one packet in memory and seeks by bisection over byte offsets, so
// no page index grows with the stream. Multiplexed or chained streams are
// rejected as unsupported; CRC, sequence and truncation errors are invalid data.
internal sealed class OggStreamReader
{
    internal const int MaxPageBytes = 27 + 255 + (255 * 255);
    private const int BisectionLinearBytes = 64 * 1024;
    private const int TailScanBytes = MaxPageBytes;
    private static readonly uint[] CrcTable = BuildCrcTable();

    private readonly Stream stream;
    private readonly string sourceName;
    private readonly byte[] page = new byte[MaxPageBytes];
    private byte[] packet = new byte[4096];

    // Current page.
    private long pageOffset = -1;
    private int pageLength;
    private int segmentCount;
    private int segmentIndex;
    private int bodyCursor;
    private long pageGranule;
    private byte pageFlags;
    private uint pageSequence;

    private long nextPageOffset;
    private int packetLength;
    private bool havePartial;
    private uint? expectedSequence;

    internal OggStreamReader(Stream stream, string sourceName)
    {
        this.stream = stream;
        this.sourceName = sourceName;
        StreamLength = stream.Length;
        if (!TryLoadPage(0, requireSerial: false) || (pageFlags & 2) == 0)
        {
            throw new SoundException(SoundErrorKind.UnsupportedFormat, $"Sound source '{sourceName}' does not start with an Ogg stream.");
        }

        Serial = BinaryPrimitives.ReadInt32LittleEndian(page.AsSpan(14));
        nextPageOffset = 0;
        pageOffset = -1;
        expectedSequence = null;
    }

    internal int Serial { get; }

    internal long StreamLength { get; }

    // Offset of the page after the one holding the last packet read.
    internal long NextPageOffset => nextPageOffset;

    // Granule position of the final page, and whether it carries the EOS flag.
    internal (long Granule, bool EndOfStream) ReadFinalPage()
    {
        int window = (int)Math.Min(TailScanBytes, StreamLength);
        byte[] tail = new byte[window];
        stream.Position = StreamLength - window;
        stream.ReadExactly(tail);
        for (int index = window - 27; index >= 0; index--)
        {
            if (!tail.AsSpan(index, 4).SequenceEqual("OggS"u8))
            {
                continue;
            }

            int length = PageLength(tail.AsSpan(index));
            if (length <= 0 || index + length != window || !CrcMatches(tail.AsSpan(index, length)))
            {
                continue;
            }

            if (BinaryPrimitives.ReadInt32LittleEndian(tail.AsSpan(index + 14)) != Serial)
            {
                throw Unsupported("the stream is chained or multiplexed with another logical stream.");
            }

            return (BinaryPrimitives.ReadInt64LittleEndian(tail.AsSpan(index + 6)), (tail[index + 5] & 4) != 0);
        }

        throw Invalid("the data does not end with a complete Ogg page (truncated or corrupt).");
    }

    // Next complete packet; the span is valid until the next call. Granule is
    // the page granule when this packet is the last one completed on its page.
    internal bool TryReadPacket(out ReadOnlySpan<byte> data, out long granule, out bool endOfStream)
    {
        while (true)
        {
            if (pageOffset < 0 || segmentIndex == segmentCount)
            {
                if (!LoadNextPage())
                {
                    if (havePartial)
                    {
                        throw Invalid("the last Ogg packet is truncated.");
                    }

                    data = default;
                    granule = -1;
                    endOfStream = true;
                    return false;
                }

                continue;
            }

            int lacing = page[27 + segmentIndex];
            AppendToPacket(page.AsSpan(bodyCursor, lacing));
            bodyCursor += lacing;
            segmentIndex++;
            if (lacing == 255)
            {
                havePartial = true;
                continue;
            }

            havePartial = false;
            bool lastOnPage = !HasCompletedPacketAfter(segmentIndex);
            data = packet.AsSpan(0, packetLength);
            packetLength = 0;
            granule = lastOnPage ? pageGranule : -1;
            endOfStream = lastOnPage && (pageFlags & 4) != 0;
            return true;
        }
    }

    // Restarts packet reading at the page at `offset`, dropping a leading
    // continued fragment.
    internal void Restart(long offset)
    {
        nextPageOffset = offset;
        pageOffset = -1;
        packetLength = 0;
        havePartial = false;
        expectedSequence = null;
        dropContinuation = true;
    }

    // Positions packet reading just after the last packet completed on the
    // page at `offset`: the next packet starts at that page's granule.
    internal void RestartAfterPage(long offset)
    {
        Restart(offset);
        if (!LoadNextPage())
        {
            throw Invalid("a seek landed outside the Ogg stream.");
        }

        while (HasCompletedPacketAfter(segmentIndex))
        {
            int lacing = page[27 + segmentIndex];
            bodyCursor += lacing;
            segmentIndex++;
        }

        // Remaining segments form the start of a packet continued on the next page.
        packetLength = 0;
        while (segmentIndex < segmentCount)
        {
            int lacing = page[27 + segmentIndex];
            AppendToPacket(page.AsSpan(bodyCursor, lacing));
            bodyCursor += lacing;
            segmentIndex++;
            havePartial = true;
        }
    }

    // Offset of the last page of this stream whose granule is in [0, target],
    // or -1 when no such page precedes `target`. Reads O(log n) pages.
    internal long FindPageAtOrBefore(long target, long dataStart, CancellationToken cancellationToken)
    {
        long low = dataStart;
        long high = StreamLength;
        long best = -1;
        while (high - low > BisectionLinearBytes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            long middle = low + ((high - low) / 2);
            if (!TryFindGranulePage(middle, high, out long offset, out long granule))
            {
                high = middle;
                continue;
            }

            if (granule <= target)
            {
                // The page starts at or after `middle`, so the range shrinks.
                best = offset;
                low = offset;
            }
            else
            {
                high = middle;
            }
        }

        long cursor = best >= 0 ? best : low;
        while (cursor < StreamLength)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!TryLoadPage(cursor, requireSerial: true))
            {
                break;
            }

            if (pageGranule >= 0)
            {
                if (pageGranule > target)
                {
                    break;
                }

                best = pageOffset;
            }

            cursor = pageOffset + pageLength;
        }

        pageOffset = -1;
        return best;
    }

    // Granule of the page at `offset` (after a FindPageAtOrBefore result).
    internal long GranuleAt(long offset)
    {
        if (!TryLoadPage(offset, requireSerial: true))
        {
            throw Invalid($"no Ogg page at byte {offset}.");
        }

        long granule = pageGranule;
        pageOffset = -1;
        return granule;
    }

    private bool dropContinuation;

    private bool LoadNextPage()
    {
        if (nextPageOffset >= StreamLength)
        {
            return false;
        }

        if (!TryLoadPage(nextPageOffset, requireSerial: false))
        {
            throw Invalid($"expected an Ogg page at byte {nextPageOffset} (corrupt or truncated data).");
        }

        int serial = BinaryPrimitives.ReadInt32LittleEndian(page.AsSpan(14));
        if (serial != Serial)
        {
            throw Unsupported("the stream is chained or multiplexed with another logical stream.");
        }

        if (expectedSequence is uint expected && pageSequence != expected)
        {
            throw Invalid($"Ogg page sequence jumps from {expected - 1} to {pageSequence} (lost data).");
        }

        expectedSequence = pageSequence + 1;
        bool continued = (pageFlags & 1) != 0;
        if (dropContinuation)
        {
            dropContinuation = false;
            if (continued)
            {
                // Skip the tail of a packet that started before the restart point.
                while (segmentIndex < segmentCount)
                {
                    int lacing = page[27 + segmentIndex];
                    bodyCursor += lacing;
                    segmentIndex++;
                    if (lacing < 255)
                    {
                        break;
                    }
                }
            }
        }
        else if (continued != havePartial)
        {
            throw Invalid($"Ogg page at byte {pageOffset} breaks packet continuation.");
        }

        nextPageOffset = pageOffset + pageLength;
        return true;
    }

    private bool HasCompletedPacketAfter(int segment)
    {
        for (int index = segment; index < segmentCount; index++)
        {
            if (page[27 + index] < 255)
            {
                return true;
            }
        }

        return false;
    }

    private void AppendToPacket(ReadOnlySpan<byte> data)
    {
        if (packetLength + data.Length > packet.Length)
        {
            Array.Resize(ref packet, Math.Max(packet.Length * 2, packetLength + data.Length));
        }

        data.CopyTo(packet.AsSpan(packetLength));
        packetLength += data.Length;
    }

    // Loads and validates the page at `offset` into `page`.
    private bool TryLoadPage(long offset, bool requireSerial)
    {
        if (offset + 27 > StreamLength)
        {
            return false;
        }

        stream.Position = offset;
        if (stream.ReadAtLeast(page.AsSpan(0, 27), 27, throwOnEndOfStream: false) < 27 || !page.AsSpan(0, 4).SequenceEqual("OggS"u8) || page[4] != 0)
        {
            return false;
        }

        int segments = page[26];
        if (stream.ReadAtLeast(page.AsSpan(27, segments), segments, throwOnEndOfStream: false) < segments)
        {
            return false;
        }

        int bodyLength = 0;
        for (int index = 0; index < segments; index++)
        {
            bodyLength += page[27 + index];
        }

        int length = 27 + segments + bodyLength;
        if (offset + length > StreamLength)
        {
            throw Invalid($"the Ogg page at byte {offset} is truncated.");
        }

        stream.ReadExactly(page.AsSpan(27 + segments, bodyLength));
        if (!CrcMatches(page.AsSpan(0, length)))
        {
            throw Invalid($"the Ogg page at byte {offset} fails its CRC check.");
        }

        if (requireSerial && BinaryPrimitives.ReadInt32LittleEndian(page.AsSpan(14)) != Serial)
        {
            throw Unsupported("the stream is chained or multiplexed with another logical stream.");
        }

        pageOffset = offset;
        pageLength = length;
        segmentCount = segments;
        segmentIndex = 0;
        bodyCursor = 27 + segments;
        pageGranule = BinaryPrimitives.ReadInt64LittleEndian(page.AsSpan(6));
        pageFlags = page[5];
        pageSequence = BinaryPrimitives.ReadUInt32LittleEndian(page.AsSpan(18));
        return true;
    }

    // Scans forward from `from` for the next valid page of this stream with a
    // granule position, before `limit`.
    private bool TryFindGranulePage(long from, long limit, out long offset, out long granule)
    {
        byte[] window = new byte[8192];
        long cursor = from;
        while (cursor < limit)
        {
            stream.Position = cursor;
            int read = stream.ReadAtLeast(window, window.Length, throwOnEndOfStream: false);
            if (read < 27)
            {
                break;
            }

            for (int index = 0; index + 4 <= read; index++)
            {
                if (window[index] != (byte)'O' || !window.AsSpan(index, 4).SequenceEqual("OggS"u8))
                {
                    continue;
                }

                long candidate = cursor + index;
                if (candidate >= limit)
                {
                    break;
                }

                bool valid;
                try
                {
                    valid = TryLoadPage(candidate, requireSerial: false);
                }
                catch (SoundException)
                {
                    // A capture pattern inside page data, not a page boundary.
                    valid = false;
                }

                if (valid && BinaryPrimitives.ReadInt32LittleEndian(page.AsSpan(14)) == Serial && pageGranule >= 0)
                {
                    offset = candidate;
                    granule = pageGranule;
                    pageOffset = -1;
                    return true;
                }

                if (valid)
                {
                    return TryFindGranulePage(candidate + pageLength, limit, out offset, out granule);
                }
            }

            cursor += read - 3;
        }

        offset = -1;
        granule = -1;
        pageOffset = -1;
        return false;
    }

    private static int PageLength(ReadOnlySpan<byte> data)
    {
        if (data.Length < 27)
        {
            return -1;
        }

        int segments = data[26];
        if (data.Length < 27 + segments)
        {
            return -1;
        }

        int length = 27 + segments;
        for (int index = 0; index < segments; index++)
        {
            length += data[27 + index];
        }

        return length;
    }

    private static bool CrcMatches(ReadOnlySpan<byte> page)
    {
        uint stored = BinaryPrimitives.ReadUInt32LittleEndian(page[22..]);
        uint crc = 0;
        for (int index = 0; index < page.Length; index++)
        {
            byte value = index is >= 22 and < 26 ? (byte)0 : page[index];
            crc = (crc << 8) ^ CrcTable[(crc >> 24) ^ value];
        }

        return crc == stored;
    }

    private static uint[] BuildCrcTable()
    {
        uint[] table = new uint[256];
        for (uint index = 0; index < 256; index++)
        {
            uint value = index << 24;
            for (int bit = 0; bit < 8; bit++)
            {
                value = (value & 0x80000000) != 0 ? (value << 1) ^ 0x04C11DB7 : value << 1;
            }

            table[index] = value;
        }

        return table;
    }

    private SoundException Invalid(string detail) =>
        new(SoundErrorKind.InvalidData, $"Sound source '{sourceName}' is invalid: {detail}");

    private SoundException Unsupported(string detail) =>
        new(SoundErrorKind.UnsupportedFormat, $"Sound source '{sourceName}' is not supported: {detail}");
}
