using Cerneala.Timbre;
using Cerneala.Timbre.Engine;

namespace Cerneala.Tests.Timbre.Decoding;

// Format detection by content, and structured errors without fallback:
// excluded variants are UnsupportedFormat, damaged data is InvalidData, a
// missing decoder is UnsupportedFormat (never InvalidData), and a failure is
// never followed by another decoder or a silent successful end.
public sealed class TimbreDecoderErrorTests
{
    [Theory]
    [InlineData("vorbis-44100-stereo-q4.ogg", "renamed.mp3")]
    [InlineData("opus-stereo-48000-96k.opus", "renamed.wav")]
    [InlineData("mp3-mpeg1-44100-stereo-cbr128.mp3", "renamed.ogg")]
    [InlineData("mp3-id3v2-id3v1-44100-stereo.mp3", "renamed")]
    public void FormatIsDetectedByContentNotExtension(string name, string renamed)
    {
        string copy = Path.Combine(Path.GetTempPath(), "cerneala-timbre-renamed", $"{Path.GetFileNameWithoutExtension(name)}-{renamed}");
        Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
        File.Copy(DecodingCorpus.PathOf(name), copy, overwrite: true);

        float[] original;
        using (TimbreReader reader = DecodingCorpus.Open(DecodingCorpus.PathOf(name)))
        {
            original = DecodingCorpus.DecodeAll(reader);
        }

        using TimbreReader renamedReader = DecodingCorpus.Open(copy);
        Assert.True(original.AsSpan().SequenceEqual(DecodingCorpus.DecodeAll(renamedReader)));
    }

    [Fact]
    public void WavContentWithAnMp3ExtensionDecodesAsWav()
    {
        string wav = WavFixture.Write("content.mp3", 44100, 2, 16);
        using TimbreReader reader = DecodingCorpus.Open(wav);
        Assert.Equal(48000, DecodingCorpus.DecodeAll(reader).Length / 2);
    }

    [Theory]
    [InlineData("mpeg-layer2-44100-stereo.mp2", TimbreErrorKind.UnsupportedFormat)]
    [InlineData("ogg-chained-vorbis.ogg", TimbreErrorKind.UnsupportedFormat)]
    [InlineData("ogg-multiplexed-vorbis-opus.ogg", TimbreErrorKind.UnsupportedFormat)]
    [InlineData("ogg-flac.ogg", TimbreErrorKind.UnsupportedFormat)]
    [InlineData("opus-surround-6ch.opus", TimbreErrorKind.UnsupportedFormat)]
    [InlineData("mp3-truncated.mp3", TimbreErrorKind.InvalidData)]
    [InlineData("mp3-corrupt-sync.mp3", TimbreErrorKind.InvalidData)]
    [InlineData("vorbis-truncated.ogg", TimbreErrorKind.InvalidData)]
    [InlineData("vorbis-corrupt-crc.ogg", TimbreErrorKind.InvalidData)]
    [InlineData("opus-truncated.opus", TimbreErrorKind.InvalidData)]
    public void NegativeCorpusFilesFailWithTheirErrorKind(string name, TimbreErrorKind kind) =>
        AssertFails(DecodingCorpus.PathOf(name), kind);

    public static IEnumerable<object[]> WavNegatives()
    {
        yield return [WavFixture.Write("rifx.wav", 44100, 2, 16, riff: "RIFX"), TimbreErrorKind.UnsupportedFormat];
        yield return [WavFixture.Write("rf64.wav", 44100, 2, 16, riff: "RF64"), TimbreErrorKind.UnsupportedFormat];
        yield return [WavFixture.Write("adpcm.wav", 44100, 2, 16, formatTag: 2), TimbreErrorKind.UnsupportedFormat];
        yield return [WavFixture.Write("float64.wav", 44100, 2, 64, formatTag: 3), TimbreErrorKind.UnsupportedFormat];
        yield return [WavFixture.Write("surround.wav", 44100, 3, 16), TimbreErrorKind.UnsupportedFormat];
        yield return [WavFixture.Write("4khz.wav", 4000, 1, 16), TimbreErrorKind.UnsupportedFormat];
        yield return [WavFixture.Write("384khz.wav", 384000, 1, 16, seconds: 0.1), TimbreErrorKind.UnsupportedFormat];
        yield return [WavFixture.Write("truncated.wav", 44100, 2, 16, truncateBytes: 1000), TimbreErrorKind.InvalidData];
        yield return [WavFixture.Write("partial-frame.wav", 44100, 2, 16, declaredDataBytes: 4001, truncateBytes: 0), TimbreErrorKind.InvalidData];
        yield return [WavFixture.Write("no-data.wav", 44100, 2, 16, omitData: true), TimbreErrorKind.InvalidData];
        yield return [WavFixture.Write("riff-avi.wav", 44100, 2, 16, form: "AVI "), TimbreErrorKind.UnsupportedFormat];
    }

    [Theory]
    [MemberData(nameof(WavNegatives))]
    public void WavVariantsOutsideTheMatrixOrDamagedFail(string path, TimbreErrorKind kind) => AssertFails(path, kind);

    // An unknown chunk that claims more bytes than the file holds is damaged
    // data, whatever bytes follow its header (here: a well-formed data chunk).
    [Theory]
    [InlineData(0xFFFFFFFDu)]
    [InlineData(0xFFFFFFFFu)]
    public void AnUnknownChunkLongerThanTheFileIsInvalidData(uint declaredSize)
    {
        string path = Path.Combine(Path.GetTempPath(), "cerneala-timbre-wav", $"oversized-chunk-{declaredSize:X8}.wav");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using (BinaryWriter writer = new(File.Create(path)))
        {
            writer.Write("RIFF"u8);
            writer.Write(0u);
            writer.Write("WAVE"u8);
            writer.Write("fmt "u8);
            writer.Write(16u);
            writer.Write((ushort)1);
            writer.Write((ushort)2);
            writer.Write(48000u);
            writer.Write(48000u * 4);
            writer.Write((ushort)4);
            writer.Write((ushort)16);
            writer.Write("junk"u8);
            writer.Write(declaredSize);
            writer.Write("data"u8);
            writer.Write(4800u * 4);
            writer.Write(new byte[4800 * 4]);
            writer.Seek(4, SeekOrigin.Begin);
            writer.Write((uint)(writer.BaseStream.Length - 8));
        }

        AssertFails(path, TimbreErrorKind.InvalidData);
    }

    [Theory]
    [InlineData("text.mp3")]
    [InlineData("empty.wav")]
    [InlineData("noise.ogg")]
    public void ContentWithoutARecognizedFormatIsUnsupported(string name)
    {
        string path = Path.Combine(Path.GetTempPath(), "cerneala-timbre-unknown", name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        byte[] content = name switch
        {
            "text.mp3" => "this is not audio at all"u8.ToArray(),
            "empty.wav" => [],
            _ => [.. Enumerable.Range(0, 4096).Select(index => (byte)((index * 7919) % 251))],
        };
        File.WriteAllBytes(path, content);

        AssertFails(path, TimbreErrorKind.UnsupportedFormat);
    }

    [Fact]
    public void AnUnavailableDecoderIsUnsupportedNotInvalidData()
    {
        TrackingStream stream = new([1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12]);
        FakeDecoder missing = new(() => throw new FileNotFoundException("Could not load file or assembly 'NVorbis'."));

        TimbreException error = Assert.Throws<TimbreException>(() =>
            TimbreDecoders.Open(stream, "missing-decoder", Budget(), [missing]));

        Assert.Equal(TimbreErrorKind.UnsupportedFormat, error.Kind);
        Assert.IsType<FileNotFoundException>(error.InnerException);
        Assert.True(stream.Disposed);
    }

    [Fact]
    public void AFailingDecoderIsNotFollowedByAnotherDecoder()
    {
        TrackingStream stream = new([1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12]);
        FakeDecoder failing = new(() => throw new TimbreException(TimbreErrorKind.InvalidData, "damaged"));
        FakeDecoder fallback = new(() => throw new InvalidOperationException("Fallback decoder must not run."));

        TimbreException error = Assert.Throws<TimbreException>(() =>
            TimbreDecoders.Open(stream, "no-fallback", Budget(), [failing, fallback]));

        Assert.Equal(TimbreErrorKind.InvalidData, error.Kind);
        Assert.Equal(1, failing.Calls);
        Assert.Equal(0, fallback.Calls);
        Assert.True(stream.Disposed);
    }

    [Fact]
    public void DecoderMemoryIsReservedBeforeTheDecoderIsBuilt()
    {
        TimbreMemoryPool pool = new(64 * 1024);
        TimbreMemoryBudget budget = new(pool, "vorbis");

        TimbreException error = Assert.Throws<TimbreException>(() =>
            DecodingCorpus.Open(DecodingCorpus.PathOf("vorbis-44100-stereo-q4.ogg"), budget));

        Assert.Equal(TimbreErrorKind.ResourceLimitExceeded, error.Kind);
        budget.Close();
        Assert.Equal(0, pool.Reserved);
    }

    // #845: a file shorter than its first frame is truncated data, not an unreadable source.
    [Theory]
    [InlineData(100)]
    [InlineData(200)]
    [InlineData(400)]
    public void Mp3CutInsideItsFirstFrameIsInvalidData(int keptBytes)
    {
        string path = TempFile("cerneala-timbre-mp3-cut", $"first-frame-{keptBytes}.mp3");
        File.WriteAllBytes(path, File.ReadAllBytes(DecodingCorpus.PathOf("mp3-mpeg1-44100-stereo-cbr128.mp3"))[..keptBytes]);

        AssertFails(path, TimbreErrorKind.InvalidData);
    }

    // #846: the LAME tag packs delay (12 bits) and padding (12 bits) at +21 from
    // "LAME"; the corpus file has delay 576 and padding 1080.
    [Theory]
    [InlineData(1080)]
    [InlineData(529)]
    [InlineData(528)]
    [InlineData(0)]
    public void Mp3WithSmallEncoderPaddingDeliversExactlyItsReportedLength(int padding)
    {
        byte[] bytes = File.ReadAllBytes(DecodingCorpus.PathOf("mp3-mpeg1-44100-stereo-cbr128.mp3"));
        int lame = bytes.AsSpan(0, 200).IndexOf("LAME"u8);
        Assert.True(lame >= 0, "The corpus file has no LAME tag in its first 200 bytes.");
        int packed = (576 << 12) | padding;
        bytes[lame + 21] = (byte)(packed >> 16);
        bytes[lame + 22] = (byte)(packed >> 8);
        bytes[lame + 23] = (byte)packed;
        string path = TempFile("cerneala-timbre-mp3-padding", $"padding-{padding}.mp3");
        File.WriteAllBytes(path, bytes);

        long declared = 0;
        float[] pcm = [];
        Exception? failure = Record.Exception(() =>
        {
            using TimbreReader reader = DecodingCorpus.Open(path);
            declared = reader.LengthFrames!.Value;
            pcm = DecodingCorpus.DecodeAll(reader);
        });

        Assert.Null(failure);
        Assert.Equal(declared, pcm.Length / 2);
    }

    // #847: the end position is a valid seek target, with or without an Info tag.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Mp3SeekToTheEndPositionIsAccepted(bool stripInfoTag)
    {
        byte[] bytes = File.ReadAllBytes(DecodingCorpus.PathOf("mp3-mpeg1-48000-mono-cbr64.mp3"));
        if (stripInfoTag)
        {
            bytes = bytes[Mp3FrameLength(bytes)..];
        }

        string path = TempFile("cerneala-timbre-mp3-seek-end", $"seek-end-{stripInfoTag}.mp3");
        File.WriteAllBytes(path, bytes);

        using TimbreReader reader = DecodingCorpus.Open(path);
        long frames = DecodingCorpus.DecodeAll(reader).Length / 2;
        Exception? seekFailure = Record.Exception(() => reader.SeekAsync(frames, CancellationToken.None).AsTask().GetAwaiter().GetResult());

        Assert.Null(seekFailure);
        TimbreReadResult after = reader.ReadAsync(new float[2048], CancellationToken.None).AsTask().GetAwaiter().GetResult();
        Assert.Equal(new TimbreReadResult(0, endOfSource: true), after);
    }

    [Fact]
    public void Mp3SeekPastTheEndPositionOfAFileWithoutAnInfoTagThrows()
    {
        byte[] bytes = File.ReadAllBytes(DecodingCorpus.PathOf("mp3-mpeg1-48000-mono-cbr64.mp3"));
        string path = TempFile("cerneala-timbre-mp3-seek-end", "seek-past-end.mp3");
        File.WriteAllBytes(path, bytes[Mp3FrameLength(bytes)..]);

        using TimbreReader reader = DecodingCorpus.Open(path);
        long frames = DecodingCorpus.DecodeAll(reader).Length / 2;

        Assert.Throws<ArgumentOutOfRangeException>(() => reader.SeekAsync(frames + 1, CancellationToken.None).AsTask().GetAwaiter().GetResult());
    }

    private static string TempFile(string folder, string name)
    {
        string path = Path.Combine(Path.GetTempPath(), folder, name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        return path;
    }

    // Length of the MPEG-1 Layer III frame at the start of `data`.
    private static int Mp3FrameLength(byte[] data)
    {
        int[] bitrates = [0, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320];
        int[] rates = [44100, 48000, 32000];
        int bitrate = bitrates[data[2] >> 4] * 1000;
        int rate = rates[(data[2] >> 2) & 3];
        return (144 * bitrate / rate) + ((data[2] >> 1) & 1);
    }

    private static TimbreMemoryBudget Budget() => new(new TimbreMemoryPool(long.MaxValue), "test");

    private static void AssertFails(string path, TimbreErrorKind kind)
    {
        TimbreException error = Assert.Throws<TimbreException>(() =>
        {
            using TimbreReader reader = DecodingCorpus.Open(path);
            DecodingCorpus.DecodeAll(reader);
        });
        Assert.Equal(kind, error.Kind);
    }

    private sealed class FakeDecoder(Func<TimbreReader> create) : ITimbreDecoder
    {
        public int Calls { get; private set; }

        public int HeaderLength => 4;

        public bool CanDecode(ReadOnlySpan<byte> header) => true;

        public TimbreReader Create(Stream stream, string sourceName, TimbreMemoryBudget budget)
        {
            Calls++;
            return create();
        }
    }

    private sealed class TrackingStream(byte[] data) : MemoryStream(data)
    {
        public bool Disposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }
}
