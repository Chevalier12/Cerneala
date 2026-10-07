namespace Cerneala.Timbre.Corpus;

// Minimal Ogg page writer (RFC 3533) for the generator: one logical stream
// per instance, explicit page flushes, CRC-32 over the whole page.
public sealed class OggWriter(Stream output, int serial)
{
    private const int MaxPageBody = 4096;
    private static readonly uint[] CrcTable = BuildCrcTable();

    private readonly List<byte> body = [];
    private readonly List<byte> lacing = [];
    private int sequence;
    private bool begun;
    private bool continued;
    private long pageGranule = -1;

    public int Serial => serial;

    public void WritePacket(ReadOnlySpan<byte> packet, long granule, bool flushAfter = false, bool endOfStream = false)
    {
        int offset = 0;
        while (true)
        {
            int remaining = packet.Length - offset;
            int chunk = Math.Min(remaining, 255);
            if (lacing.Count == 255 || body.Count + chunk > MaxPageBody && lacing.Count > 0)
            {
                Flush(endOfStream: false, packetContinues: offset > 0);
            }

            body.AddRange(packet.Slice(offset, chunk).ToArray());
            lacing.Add((byte)chunk);
            offset += chunk;
            if (chunk < 255)
            {
                break;
            }
        }

        pageGranule = granule;
        if (flushAfter || endOfStream)
        {
            Flush(endOfStream, packetContinues: false);
        }
    }

    public void Flush(bool endOfStream, bool packetContinues)
    {
        if (lacing.Count == 0 && !endOfStream)
        {
            return;
        }

        byte[] header = new byte[27 + lacing.Count];
        "OggS"u8.CopyTo(header);
        header[4] = 0;
        header[5] = (byte)((continued ? 1 : 0) | (begun ? 0 : 2) | (endOfStream ? 4 : 0));
        long granule = packetContinues && pageGranule == -1 ? -1 : pageGranule;
        BitConverter.TryWriteBytes(header.AsSpan(6), granule);
        BitConverter.TryWriteBytes(header.AsSpan(14), serial);
        BitConverter.TryWriteBytes(header.AsSpan(18), sequence++);
        header[26] = (byte)lacing.Count;
        lacing.CopyTo(header, 27);
        byte[] page = [.. header, .. body];
        BitConverter.TryWriteBytes(page.AsSpan(22), Crc(page));
        output.Write(page);
        begun = true;
        continued = packetContinues;
        body.Clear();
        lacing.Clear();
        pageGranule = -1;
    }

    public static uint Crc(ReadOnlySpan<byte> data)
    {
        uint crc = 0;
        foreach (byte value in data)
        {
            crc = (crc << 8) ^ CrcTable[(crc >> 24) ^ value];
        }

        return crc;
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
}
