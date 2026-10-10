using System.Buffers.Binary;

namespace Cerneala.Timbre.Decoding;

// RIFF/WAVE little-endian PCM 8/16/24/32-bit and IEEE float32, mono or
// stereo, including WAVE_FORMAT_EXTENSIBLE with those subformats.
internal sealed class WavSource : DecodedSource
{
    private const ushort FormatPcm = 1;
    private const ushort FormatFloat = 3;
    private const ushort FormatExtensible = 0xFFFE;
    private const int ChunkBytes = 16 * 1024;

    private readonly Stream stream;
    private readonly string sourceName;
    private readonly long dataOffset;
    private readonly long frames;
    private readonly int blockAlign;
    private readonly int bitsPerSample;
    private readonly bool isFloat;
    private readonly byte[] chunk = new byte[ChunkBytes];
    private long position;

    internal WavSource(Stream stream, string sourceName)
    {
        this.stream = stream;
        this.sourceName = sourceName;
        Span<byte> header = stackalloc byte[12];
        stream.Position = 0;
        ReadExactly(header, "RIFF header");
        if (header[..4].SequenceEqual("RIFX"u8) || header[..4].SequenceEqual("RF64"u8) || header[..4].SequenceEqual("BW64"u8))
        {
            throw Unsupported(sourceName, $"{System.Text.Encoding.ASCII.GetString(header[..4])} WAVE files are not supported (RIFF little-endian only).");
        }

        if (!header[8..12].SequenceEqual("WAVE"u8))
        {
            throw Unsupported(sourceName, $"RIFF form type '{System.Text.Encoding.ASCII.GetString(header[8..12])}' is not WAVE.");
        }

        ushort format = 0;
        int channels = 0;
        int sampleRate = 0;
        bool haveFormat = false;
        long dataSize = -1;
        Span<byte> chunkHeader = stackalloc byte[8];
        while (dataSize < 0)
        {
            if (stream.Read(chunkHeader) < 8)
            {
                throw Invalid(sourceName, haveFormat ? "missing data chunk." : "missing fmt chunk.");
            }

            uint size = BinaryPrimitives.ReadUInt32LittleEndian(chunkHeader[4..]);
            if (chunkHeader[..4].SequenceEqual("fmt "u8))
            {
                if (size < 16 || size > 1024)
                {
                    throw Invalid(sourceName, $"fmt chunk of {size} bytes.");
                }

                byte[] fmtChunk = new byte[size];
                Span<byte> fmt = fmtChunk;
                ReadExactly(fmt, "fmt chunk");
                format = BinaryPrimitives.ReadUInt16LittleEndian(fmt);
                channels = BinaryPrimitives.ReadUInt16LittleEndian(fmt[2..]);
                sampleRate = (int)BinaryPrimitives.ReadUInt32LittleEndian(fmt[4..]);
                blockAlign = BinaryPrimitives.ReadUInt16LittleEndian(fmt[12..]);
                bitsPerSample = BinaryPrimitives.ReadUInt16LittleEndian(fmt[14..]);
                if (format == FormatExtensible)
                {
                    if (size < 40)
                    {
                        throw Invalid(sourceName, "WAVE_FORMAT_EXTENSIBLE fmt chunk is too short.");
                    }

                    // The subformat GUID's first two bytes carry the format tag.
                    format = BinaryPrimitives.ReadUInt16LittleEndian(fmt[24..]);
                    if (!fmt[26..40].SequenceEqual(ExtensibleGuidTail))
                    {
                        throw Unsupported(sourceName, "WAVE_FORMAT_EXTENSIBLE subformat is not PCM or IEEE float.");
                    }
                }

                SkipPadding(size);
                haveFormat = true;
            }
            else if (chunkHeader[..4].SequenceEqual("data"u8))
            {
                if (!haveFormat)
                {
                    throw Invalid(sourceName, "data chunk precedes fmt chunk.");
                }

                dataSize = size;
            }
            else
            {
                stream.Seek((long)size + (size & 1), SeekOrigin.Current);
            }
        }

        isFloat = format == FormatFloat;
        if (format is not (FormatPcm or FormatFloat))
        {
            throw Unsupported(sourceName, $"WAVE format tag {format} (only PCM and IEEE float are supported).");
        }

        if (isFloat ? bitsPerSample != 32 : bitsPerSample is not (8 or 16 or 24 or 32))
        {
            throw Unsupported(sourceName, $"{bitsPerSample}-bit {(isFloat ? "float" : "PCM")} samples.");
        }

        ValidateFormat("WAV", sourceName, sampleRate, channels);
        if (blockAlign != channels * bitsPerSample / 8)
        {
            throw Invalid(sourceName, $"block align {blockAlign} does not match {channels} × {bitsPerSample}-bit samples.");
        }

        dataOffset = stream.Position;
        if (dataSize % blockAlign != 0)
        {
            throw Invalid(sourceName, "data chunk does not hold a whole number of frames.");
        }

        frames = dataSize / blockAlign;
        SampleRate = sampleRate;
        Channels = channels;
    }

    private static ReadOnlySpan<byte> ExtensibleGuidTail =>
        [0x00, 0x00, 0x00, 0x00, 0x10, 0x00, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71];

    internal override string Codec => "WAV";

    internal override int SampleRate { get; }

    internal override int Channels { get; }

    internal override long? LengthFrames => frames;

    internal override int Read(Span<float> destination, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        int wanted = (int)Math.Min(Math.Min(destination.Length / Channels, frames - position), ChunkBytes / blockAlign);
        if (wanted <= 0)
        {
            return 0;
        }

        int bytes = wanted * blockAlign;
        int read = stream.ReadAtLeast(chunk.AsSpan(0, bytes), bytes, throwOnEndOfStream: false);
        if (read < bytes)
        {
            throw Invalid(sourceName, $"data ends after {position + (read / blockAlign)} of {frames} frames (truncated).");
        }

        Convert(chunk.AsSpan(0, bytes), destination[..(wanted * Channels)]);
        position += wanted;
        return wanted;
    }

    internal override void Seek(long frame, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(frame);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(frame, frames);
        stream.Position = dataOffset + (frame * blockAlign);
        position = frame;
    }

    public override void Dispose() => stream.Dispose();

    private void Convert(ReadOnlySpan<byte> source, Span<float> destination)
    {
        switch (bitsPerSample)
        {
            case 8:
                for (int index = 0; index < destination.Length; index++)
                {
                    destination[index] = (source[index] - 128) / 128f;
                }

                break;
            case 16:
                for (int index = 0; index < destination.Length; index++)
                {
                    destination[index] = BinaryPrimitives.ReadInt16LittleEndian(source[(index * 2)..]) / 32768f;
                }

                break;
            case 24:
                for (int index = 0; index < destination.Length; index++)
                {
                    int offset = index * 3;
                    int value = source[offset] | (source[offset + 1] << 8) | ((sbyte)source[offset + 2] << 16);
                    destination[index] = value / 8388608f;
                }

                break;
            case 32 when isFloat:
                for (int index = 0; index < destination.Length; index++)
                {
                    destination[index] = BinaryPrimitives.ReadSingleLittleEndian(source[(index * 4)..]);
                }

                break;
            default:
                for (int index = 0; index < destination.Length; index++)
                {
                    destination[index] = (float)(BinaryPrimitives.ReadInt32LittleEndian(source[(index * 4)..]) / 2147483648.0);
                }

                break;
        }
    }

    private void ReadExactly(Span<byte> buffer, string what)
    {
        if (stream.ReadAtLeast(buffer, buffer.Length, throwOnEndOfStream: false) < buffer.Length)
        {
            throw Invalid(sourceName, $"truncated {what}.");
        }
    }

    private void SkipPadding(uint size)
    {
        if ((size & 1) != 0)
        {
            stream.Seek(1, SeekOrigin.Current);
        }
    }
}
