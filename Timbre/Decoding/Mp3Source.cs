using Cerneala.Timbre.Decoding.NLayer;

namespace Cerneala.Timbre.Decoding;

// MPEG-1/2/2.5 Layer III through the vendored NLayer frame decoder. Framing, tags,
// gapless trimming and seeking are owned here, without a frame index:
// memory does not depend on the stream duration.
internal sealed class Mp3Source : DecodedSource
{
    // Decoder delay of the MP3 synthesis filterbank (LAME's 528 + 1).
    internal const int DecoderDelay = 529;
    private const int MaxFrameBytes = 2881;
    private const int HistoryFrames = 64;
    private const int PrerollBytes = 2048;
    private const int MinPrerollFrames = 3;

    private readonly Stream stream;
    private readonly string sourceName;
    private readonly MpegFrameDecoder decoder = new();
    private readonly Mp3Frame frame = new();
    private readonly float[] decoded = new float[1152 * 2];
    private readonly long dataStart;
    private readonly long dataEnd;
    private readonly int samplesPerFrame;
    private readonly int firstAudioFrameOffset;
    private readonly long trimStart;
    private readonly long? totalFrames;
    private readonly FrameHeader reference;

    // Scan state: stream offset and index of the next frame to decode.
    private long nextOffset;
    private long nextFrameIndex;
    private readonly long[] history = new long[HistoryFrames];

    // Output state: decoded frames pending in `decoded`, position in source frames.
    private int pendingStart;
    private int pendingEnd;
    private long position;
    private long decodedSamples;

    internal Mp3Source(Stream stream, string sourceName)
    {
        this.stream = stream;
        this.sourceName = sourceName;
        long length = stream.Length;
        long offset = SkipId3v2(stream, 0);
        dataEnd = TrailingTagStart(stream, length);
        Span<byte> bytes = stackalloc byte[4];
        if (!TryReadHeader(offset, bytes, out FrameHeader first) ||
            offset + first.Length < dataEnd && (!TryReadHeader(offset + first.Length, bytes, out FrameHeader second) || !first.Matches(second)))
        {
            throw Unsupported(sourceName, "no MPEG audio frame sequence at the start of the data.");
        }

        if (first.Layer != 3)
        {
            throw Unsupported(sourceName, $"MPEG Layer {(first.Layer == 1 ? "I" : "II")} audio is not MP3.");
        }

        reference = first;
        samplesPerFrame = first.SamplesPerFrame;
        ValidateFormat("MP3", sourceName, first.SampleRate, first.Channels);
        dataStart = offset;

        // A first frame carrying a Xing/Info (LAME) or VBRI tag is metadata, not audio.
        byte[] tagFrame = new byte[first.Length];
        stream.Position = offset;
        stream.ReadExactly(tagFrame);
        long? frameCount = null;
        long encoderDelay = 0;
        long encoderPadding = 0;
        bool gapless = false;
        if (TryParseInfoTag(tagFrame, first, out long? xingFrames, out int? delay, out int? padding))
        {
            firstAudioFrameOffset = first.Length;
            frameCount = xingFrames;
            if (delay is int d && padding is int p)
            {
                encoderDelay = d;
                encoderPadding = p;
                gapless = true;
            }
        }

        trimStart = gapless ? encoderDelay + DecoderDelay : 0;
        if (frameCount is long count)
        {
            long decodedTotal = count * samplesPerFrame;
            totalFrames = gapless ? Math.Max(0, decodedTotal - encoderDelay - encoderPadding) : decodedTotal;
        }

        SampleRate = first.SampleRate;
        Channels = first.Channels;
        RestartAt(0);
    }

    internal override string Codec => "MP3";

    internal override int SampleRate { get; }

    internal override int Channels { get; }

    internal override long? LengthFrames => totalFrames;

    internal override int Read(Span<float> destination, CancellationToken cancellationToken)
    {
        ThrowIfFaulted();
        int capacity = destination.Length / Channels;
        int written = 0;
        while (written < capacity)
        {
            if (pendingStart == pendingEnd)
            {
                if (totalFrames is long total && position >= total)
                {
                    break;
                }

                cancellationToken.ThrowIfCancellationRequested();
                if (!DecodeNextFrame())
                {
                    if (totalFrames is long declared && position < declared)
                    {
                        throw Invalid(sourceName, $"the stream ends after {position} of the {declared} frames its Info tag declares (truncated).");
                    }

                    break;
                }

                continue;
            }

            int count = Math.Min(capacity - written, pendingEnd - pendingStart);
            if (totalFrames is long limit)
            {
                count = (int)Math.Min(count, limit - position);
                if (count <= 0)
                {
                    // Encoder padding after the gapless end.
                    pendingStart = pendingEnd;
                    break;
                }
            }

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
        if (totalFrames is long total)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(frame, total);
        }

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
        if (frame == totalFrames)
        {
            pendingStart = pendingEnd = 0;
            nextOffset = dataEnd;
            position = frame;
            return;
        }

        long target = frame + trimStart;
        long targetFrame = target / samplesPerFrame;

        // Walk the frame headers (no index is kept) to the target frame,
        // remembering recent offsets for the bit-reservoir pre-roll.
        long index = 0;
        long offset = dataStart + firstAudioFrameOffset;
        Span<byte> bytes = stackalloc byte[4];
        while (index < targetFrame)
        {
            if ((index & 63) == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (offset >= dataEnd)
            {
                throw new ArgumentOutOfRangeException(nameof(frame), frame, "The seek target is past the end of the source.");
            }

            history[index % HistoryFrames] = offset;
            offset += ReadFrameHeader(offset, bytes).Length;
            index++;
        }

        if (offset >= dataEnd)
        {
            throw new ArgumentOutOfRangeException(nameof(frame), frame, "The seek target is past the end of the source.");
        }

        long start = PrerollStart(targetFrame, offset);
        decoder.Reset();
        nextOffset = start == targetFrame ? offset : history[start % HistoryFrames];
        nextFrameIndex = start;
        decodedSamples = start * samplesPerFrame;
        pendingStart = pendingEnd = 0;
        position = frame;
        while (nextFrameIndex <= targetFrame)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!DecodeNextFrame(discardBefore: target))
            {
                break;
            }
        }
    }

    // Pre-roll: restart a few frames earlier so the bit reservoir, the IMDCT
    // overlap and the synthesis filterbank match a continuous decode. Needs
    // `history` for the frames before `targetFrame`.
    private long PrerollStart(long targetFrame, long targetOffset)
    {
        long start = targetFrame;
        while (start > 0 && targetFrame - start < HistoryFrames - 1)
        {
            start--;
            if (targetFrame - start >= MinPrerollFrames && targetOffset - history[start % HistoryFrames] >= PrerollBytes)
            {
                break;
            }
        }

        return start;
    }

    private void RestartAt(long frame)
    {
        decoder.Reset();
        nextOffset = dataStart + firstAudioFrameOffset;
        nextFrameIndex = 0;
        decodedSamples = 0;
        pendingStart = pendingEnd = 0;
        position = frame;
    }

    // Decodes the next frame into `decoded`, exposing only samples at decoded
    // index ≥ max(trimStart, discardBefore). Returns false at the end.
    private bool DecodeNextFrame(long discardBefore = 0)
    {
        if (nextOffset >= dataEnd)
        {
            return false;
        }

        Span<byte> bytes = stackalloc byte[4];
        FrameHeader header = ReadFrameHeader(nextOffset, bytes);
        if (nextOffset + header.Length > dataEnd)
        {
            throw Invalid(sourceName, $"the last MPEG frame at byte {nextOffset} is truncated.");
        }

        stream.Position = nextOffset;
        frame.Load(stream, header);
        history[nextFrameIndex % HistoryFrames] = nextOffset;
        nextOffset += header.Length;
        nextFrameIndex++;
        int samples;
        try
        {
            samples = decoder.DecodeFrame(frame, decoded, 0);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw Invalid(sourceName, $"MPEG frame {nextFrameIndex - 1} could not be decoded.", exception);
        }

        int frames = samples / Channels;
        long frameStart = decodedSamples;
        decodedSamples += samplesPerFrame;
        if (frames != samplesPerFrame)
        {
            // After a restart the first frames may lack their bit reservoir;
            // NLayer then yields nothing. That is only acceptable in pre-roll.
            if (frameStart + samplesPerFrame > discardBefore)
            {
                throw Invalid(sourceName, $"MPEG frame {nextFrameIndex - 1} decoded to {frames} of {samplesPerFrame} samples.");
            }

            pendingStart = pendingEnd = 0;
            return true;
        }

        long keepFrom = Math.Max(trimStart, discardBefore);
        int skip = (int)Math.Clamp(keepFrom - frameStart, 0, frames);
        pendingStart = skip;
        pendingEnd = frames;
        return true;
    }

    private FrameHeader ReadFrameHeader(long offset, Span<byte> bytes)
    {
        if (!TryReadHeader(offset, bytes, out FrameHeader header))
        {
            throw Invalid(sourceName, $"MPEG frame sync lost at byte {offset}.");
        }

        if (!header.Matches(reference))
        {
            throw Invalid(sourceName, $"MPEG frame at byte {offset} changes version, layer, rate or channels.");
        }

        return header;
    }

    private bool TryReadHeader(long offset, Span<byte> bytes, out FrameHeader header)
    {
        header = default;
        if (offset + 4 > stream.Length)
        {
            return false;
        }

        stream.Position = offset;
        if (stream.ReadAtLeast(bytes, 4, throwOnEndOfStream: false) < 4)
        {
            return false;
        }

        return FrameHeader.TryParse(bytes, out header);
    }

    private static long SkipId3v2(Stream stream, long offset)
    {
        Span<byte> tag = stackalloc byte[10];
        while (true)
        {
            stream.Position = offset;
            if (stream.ReadAtLeast(tag, 10, throwOnEndOfStream: false) < 10 || !tag[..3].SequenceEqual("ID3"u8))
            {
                return offset;
            }

            int size = (tag[6] << 21) | (tag[7] << 14) | (tag[8] << 7) | tag[9];
            bool footer = (tag[5] & 0x10) != 0;
            offset += 10 + size + (footer ? 10 : 0);
        }
    }

    private static long TrailingTagStart(Stream stream, long length)
    {
        long end = length;
        Span<byte> buffer = stackalloc byte[32];
        if (end >= 128)
        {
            stream.Position = end - 128;
            stream.ReadExactly(buffer[..3]);
            if (buffer[..3].SequenceEqual("TAG"u8))
            {
                end -= 128;
            }
        }

        if (end >= 32)
        {
            stream.Position = end - 32;
            stream.ReadExactly(buffer);
            if (buffer[..8].SequenceEqual("APETAGEX"u8))
            {
                int size = BitConverter.ToInt32(buffer[12..16]);
                bool hasHeader = (buffer[23] & 0x80) != 0;
                end -= size + (hasHeader ? 32 : 0);
            }
        }

        return end;
    }

    private static bool TryParseInfoTag(byte[] data, FrameHeader header, out long? frames, out int? delay, out int? padding)
    {
        frames = null;
        delay = null;
        padding = null;
        int sideInfo = header.Version == 1 ? (header.Channels == 1 ? 17 : 32) : (header.Channels == 1 ? 9 : 17);
        int offset = 4 + (header.HasCrc ? 2 : 0) + sideInfo;
        if (offset + 8 <= data.Length && (data.AsSpan(offset, 4).SequenceEqual("Xing"u8) || data.AsSpan(offset, 4).SequenceEqual("Info"u8)))
        {
            int flags = ReadBigEndian(data, offset + 4);
            int cursor = offset + 8;
            if ((flags & 1) != 0)
            {
                frames = (uint)ReadBigEndian(data, cursor);
                cursor += 4;
            }

            cursor += (flags & 2) != 0 ? 4 : 0;
            cursor += (flags & 4) != 0 ? 100 : 0;
            cursor += (flags & 8) != 0 ? 4 : 0;

            // LAME extension: 9-byte version string, then delay/padding at +21.
            if (cursor + 24 <= data.Length && data.AsSpan(cursor, 4).SequenceEqual("LAME"u8) || cursor + 24 <= data.Length && data.AsSpan(cursor, 4).SequenceEqual("Lavc"u8))
            {
                int packed = (data[cursor + 21] << 16) | (data[cursor + 22] << 8) | data[cursor + 23];
                delay = packed >> 12;
                padding = packed & 0xFFF;
            }

            return true;
        }

        int vbri = 4 + 32;
        if (vbri + 18 <= data.Length && data.AsSpan(vbri, 4).SequenceEqual("VBRI"u8))
        {
            frames = (uint)ReadBigEndian(data, vbri + 14);
            return true;
        }

        return false;
    }

    private static int ReadBigEndian(byte[] data, int offset) =>
        (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];

    internal readonly struct FrameHeader
    {
        private static readonly int[,] Bitrates =
        {
            { 0, 32, 64, 96, 128, 160, 192, 224, 256, 288, 320, 352, 384, 416, 448 },
            { 0, 32, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, 384 },
            { 0, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320 },
            { 0, 32, 48, 56, 64, 80, 96, 112, 128, 144, 160, 176, 192, 224, 256 },
            { 0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160 },
        };

        private static readonly int[] Rates = [44100, 48000, 32000];

        internal int Version { get; init; } // 1, 2, or 25
        internal int Layer { get; init; }
        internal bool HasCrc { get; init; }
        internal int BitrateIndex { get; init; }
        internal int SampleRateIndex { get; init; }
        internal int SampleRate { get; init; }
        internal int Padding { get; init; }
        internal int ChannelMode { get; init; }
        internal int ModeExtension { get; init; }
        internal bool Copyright { get; init; }
        internal int Channels => ChannelMode == 3 ? 1 : 2;
        internal int Bitrate { get; init; }
        internal int Length { get; init; }
        internal int SamplesPerFrame => Layer == 1 ? 384 : Layer == 2 || Version == 1 ? 1152 : 576;

        internal bool Matches(FrameHeader other) =>
            Version == other.Version && Layer == other.Layer && SampleRate == other.SampleRate && Channels == other.Channels;

        internal static bool TryParse(ReadOnlySpan<byte> bytes, out FrameHeader header)
        {
            header = default;
            if (bytes[0] != 0xFF || (bytes[1] & 0xE0) != 0xE0)
            {
                return false;
            }

            int versionBits = (bytes[1] >> 3) & 3;
            int layerBits = (bytes[1] >> 1) & 3;
            int bitrateIndex = bytes[2] >> 4;
            int rateIndex = (bytes[2] >> 2) & 3;
            if (versionBits == 1 || layerBits == 0 || bitrateIndex is 0 or 15 || rateIndex == 3)
            {
                return false;
            }

            int version = versionBits switch { 3 => 1, 2 => 2, _ => 25 };
            int layer = 4 - layerBits;
            int rate = Rates[rateIndex] / (version == 1 ? 1 : version == 2 ? 2 : 4);
            int table = version == 1 ? layer - 1 : layer == 1 ? 3 : 4;
            int bitrate = Bitrates[table, bitrateIndex] * 1000;
            int padding = (bytes[2] >> 1) & 1;
            int length = layer == 1
                ? ((12 * bitrate / rate) + padding) * 4
                : (layer == 3 && version != 1 ? 72 : 144) * bitrate / rate + padding;
            header = new FrameHeader
            {
                Version = version,
                Layer = layer,
                HasCrc = (bytes[1] & 1) == 0,
                BitrateIndex = bitrateIndex,
                SampleRateIndex = rateIndex,
                SampleRate = rate,
                Padding = padding,
                ChannelMode = bytes[3] >> 6,
                ModeExtension = (bytes[3] >> 4) & 3,
                Copyright = (bytes[3] & 8) != 0,
                Bitrate = bitrate,
                Length = length,
            };
            return true;
        }
    }

    // IMpegFrame over one frame's bytes, reused for every frame.
    private sealed class Mp3Frame : IMpegFrame
    {
        private readonly byte[] data = new byte[MaxFrameBytes];
        private FrameHeader header;
        private int readOffset;
        private int bitsRead;
        private ulong bitBucket;

        public int SampleRate => header.SampleRate;
        public int SampleRateIndex => header.SampleRateIndex;
        public int FrameLength => header.Length;
        public int BitRate => header.Bitrate;
        public MpegVersion Version => header.Version switch { 1 => MpegVersion.Version1, 2 => MpegVersion.Version2, _ => MpegVersion.Version25 };
        public MpegLayer Layer => MpegLayer.LayerIII;
        public MpegChannelMode ChannelMode => (MpegChannelMode)header.ChannelMode;
        public int ChannelModeExtension => header.ModeExtension;
        public int SampleCount => header.SamplesPerFrame;
        public int BitRateIndex => header.BitrateIndex;
        public bool IsCopyrighted => header.Copyright;
        public bool HasCrc => header.HasCrc;
        public bool IsCorrupted => false;

        internal void Load(Stream stream, FrameHeader frameHeader)
        {
            header = frameHeader;
            stream.ReadExactly(data, 0, header.Length);
            Reset();
        }

        public void Reset()
        {
            readOffset = 4 + (header.HasCrc ? 2 : 0);
            bitsRead = 0;
            bitBucket = 0;
        }

        public int ReadBits(int bitCount)
        {
            if (bitCount is < 1 or > 32)
            {
                throw new ArgumentOutOfRangeException(nameof(bitCount));
            }

            while (bitsRead < bitCount)
            {
                if (readOffset == header.Length)
                {
                    throw new EndOfStreamException();
                }

                bitBucket = (bitBucket << 8) | data[readOffset++];
                bitsRead += 8;
            }

            int value = (int)((bitBucket >> (bitsRead - bitCount)) & ((1UL << bitCount) - 1));
            bitsRead -= bitCount;
            return value;
        }
    }
}
