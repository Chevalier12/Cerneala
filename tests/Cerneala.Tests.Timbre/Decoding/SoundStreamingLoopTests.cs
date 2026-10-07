using Cerneala.Tests.Timbre.Harness;
using Cerneala.Timbre;

namespace Cerneala.Tests.Timbre.Decoding;

// A streaming loop rewinds the same reader: every repetition is the whole
// trimmed source, back to back, re-read from the source (no cached copy),
// with the same buffers and reservations as the first pass.
public sealed class SoundStreamingLoopTests
{
    public static IEnumerable<object[]> Files()
    {
        yield return ["mp3-mpeg1-44100-stereo-cbr128.mp3"];
        yield return ["vorbis-22050-mono-q2.ogg"];
        yield return ["opus-stereo-preskip-trim.opus"];
        yield return [WavFixture.Write("loop.wav", 32000, 2, 16, seconds: 1.5)];
    }

    [Theory]
    [MemberData(nameof(Files))]
    public async Task StreamingLoopRepeatsTheWholeTrimmedSourceWithoutGaps(string name)
    {
        ObservedSource source = new(Path.IsPathRooted(name) ? name : DecodingCorpus.PathOf(name), loop: true);
        float[] once = source.Decode();
        long length = once.Length / 2;
        using TimbreRig rig = new();
        SoundPlayback playback = rig.Scope.Play(source.Clip);
        List<float> mixed = [.. await rig.StartAsync(playback)];
        long reservedFirstPass = rig.Runtime.GetDiagnostics().ReaderMemoryBytes;
        long wanted = (length * 2) + (length / 2);
        while (mixed.Count / 2 < wanted)
        {
            mixed.AddRange(await StreamingDrive.NextBlockAsync(rig, playback));
        }

        float[] expected = new float[wanted * 2];
        for (long frame = 0; frame < wanted; frame++)
        {
            long source0 = frame % length;
            expected[frame * 2] = once[source0 * 2];
            expected[(frame * 2) + 1] = once[(source0 * 2) + 1];
        }

        SoundRuntimeDiagnostics diagnostics = rig.Runtime.GetDiagnostics();
        Assert.Equal(0, diagnostics.UnderrunFrames);
        TimbreRig.AssertPcm(expected, mixed.GetRange(0, (int)(wanted * 2)).ToArray(), tolerance: 0);
        Assert.True(diagnostics.LoopWraps >= 2);
        Assert.False(playback.Completion.IsCompleted);
        Assert.Equal(reservedFirstPass, diagnostics.ReaderMemoryBytes);
        ObservedFileStream stream = Assert.Single(source.Streams);
        Assert.True(stream.BytesRead >= 2 * new FileInfo(source.Path).Length * 0.9, "Repetitions were not re-read from the source.");
    }
}
