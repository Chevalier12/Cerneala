using System.Buffers.Binary;
using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.Checksum;
using ICSharpCode.SharpZipLib.Zip.Compression;

namespace Cerneala.Scene2D.Importers;

// Strict bounded framing around the established DEFLATE decoder. Unlike the
// framework's process-global strictness switch, completion is local and explicit.
internal static class TileDataDecoder
{
    internal static byte[] Decode(ImportContext context, byte[] encoded, string compression, int expectedBytes)
    {
        if (compression == "")
        {
            if (encoded.Length != expectedBytes) { context.Fail("SCN2D005", "Raw tile data must have exactly four bytes per cell."); }
            return encoded;
        }
        byte[] output = new byte[expectedBytes];
        int written = 0, position = 0;
        try
        {
            if (compression == "zlib")
            {
                if (encoded.Length < 2 || (encoded[0] >> 4) > 7) { context.Fail("SCN2D002", "Invalid zlib window header."); }
                Inflate(context, encoded, ref position, output, ref written, raw: false);
                if (position != encoded.Length) { context.Fail("SCN2D002", "Trailing bytes after the zlib stream."); }
            }
            else
            {
                if (encoded.Length == 0) { context.Fail("SCN2D002", "Missing gzip member."); }
                while (position < encoded.Length)
                {
                    GzipHeader(context, encoded, ref position);
                    int memberStart = written;
                    Inflate(context, encoded, ref position, output, ref written, raw: true);
                    Require(context, encoded, position, 8);
                    uint checksum = BinaryPrimitives.ReadUInt32LittleEndian(encoded.AsSpan(position, 4));
                    uint size = BinaryPrimitives.ReadUInt32LittleEndian(encoded.AsSpan(position + 4, 4));
                    if (checksum != Crc(output, memberStart, written - memberStart) || size != written - memberStart)
                    { context.Fail("SCN2D002", "The gzip member checksum or size is invalid."); }
                    position += 8;
                }
            }
        }
        catch (SharpZipBaseException) { context.Fail("SCN2D002", "Invalid compressed tile data."); }
        if (written != expectedBytes) { context.Fail("SCN2D005", "Decoded tile data must have exactly four bytes per cell."); }
        return output;
    }

    private static void Inflate(ImportContext context, byte[] encoded, ref int position, byte[] output, ref int written, bool raw)
    {
        Inflater inflater = new(raw);
        inflater.SetInput(encoded, position, encoded.Length - position);
        byte[] overflow = new byte[1];
        while (!inflater.IsFinished)
        {
            int read = written < output.Length ? inflater.Inflate(output, written, output.Length - written) : inflater.Inflate(overflow);
            if (written == output.Length && read != 0) { context.Fail("SCN2D005", "Decoded tile data exceeds the declared cell count."); }
            written += read;
            if (read == 0 && !inflater.IsFinished)
            {
                context.Fail(inflater.IsNeedingDictionary ? "SCN2D004" : "SCN2D002",
                    inflater.IsNeedingDictionary ? "Preset compression dictionaries are not supported." : "The compressed stream is incomplete.");
            }
        }
        position = encoded.Length - inflater.RemainingInput;
    }

    private static void GzipHeader(ImportContext context, byte[] encoded, ref int position)
    {
        int start = position;
        Require(context, encoded, position, 10);
        if (encoded[position] != 0x1f || encoded[position + 1] != 0x8b || encoded[position + 2] != 8 || (encoded[position + 3] & 0xe0) != 0)
        { context.Fail("SCN2D002", "Invalid gzip header."); }
        byte flags = encoded[position + 3];
        position += 10;
        if ((flags & 4) != 0)
        {
            Require(context, encoded, position, 2);
            int length = BinaryPrimitives.ReadUInt16LittleEndian(encoded.AsSpan(position, 2));
            position += 2;
            Require(context, encoded, position, length);
            position += length;
        }
        if ((flags & 8) != 0) { SkipTerminated(context, encoded, ref position); }
        if ((flags & 16) != 0) { SkipTerminated(context, encoded, ref position); }
        if ((flags & 2) != 0)
        {
            Require(context, encoded, position, 2);
            if (BinaryPrimitives.ReadUInt16LittleEndian(encoded.AsSpan(position, 2)) != (Crc(encoded, start, position - start) & 0xffff))
            { context.Fail("SCN2D002", "Invalid gzip header checksum."); }
            position += 2;
        }
    }

    private static void SkipTerminated(ImportContext context, byte[] encoded, ref int position)
    {
        while (true)
        {
            Require(context, encoded, position, 1);
            if (encoded[position++] == 0) { return; }
        }
    }

    private static uint Crc(byte[] bytes, int offset, int count)
    { Crc32 checksum = new(); checksum.Update(new ArraySegment<byte>(bytes, offset, count)); return (uint)checksum.Value; }
    private static void Require(ImportContext context, byte[] bytes, int position, int count)
    { if (count > bytes.Length - position) { context.Fail("SCN2D002", "The gzip header or trailer is incomplete."); } }
}
