using Cerneala.Tests.Timbre.Dsp;
using Cerneala.Tests.Timbre.Harness;
using Cerneala.Timbre;

namespace Cerneala.Tests.Timbre.Decoding;

// Decoded streams reach the output through the one Timbre engine: the same
// reader feeds the runtime's DSP chain and mix (no mixer or device in the
// decoder adapters), and the result equals the independent DSP oracle applied
// to the decoded PCM, across stream segments and loop repetitions.
public sealed class TimbreDecoderIntegrationTests
{
    private const float Tolerance = 2e-5f;

    public static IEnumerable<object[]> Files()
    {
        yield return ["mp3-mpeg1-44100-stereo-cbr128.mp3"];
        yield return ["vorbis-22050-mono-q2.ogg"];
        yield return ["opus-stereo-48000-96k.opus"];
        yield return [WavFixture.Write("integration.wav", 32000, 2, 24, seconds: 1)];
    }

    [Theory]
    [MemberData(nameof(Files))]
    public async Task StreamedFileThroughLowPassAndDelayEqualsTheOracleOnTheDecodedPcm(string name)
    {
        ObservedSource source = new(Path.IsPathRooted(name) ? name : DecodingCorpus.PathOf(name));
        float[] decoded = source.Decode();
        TimbreSound clip = new(
            TimbreSource.FromStream(_ => new ObservedFileStream(source.Path), "integration"),
            volume: 0.7f,
            loop: true,
            loading: TimbreLoading.Streaming,
            modifiers: [new LowPass(cutoff: 3000f), new Delay(time: 300f / 48000f, feedback: 0.5f, mix: 0.4f)]);
        using TimbreRig rig = new();
        TimbrePlayback playback = rig.Scope.Play(clip);

        long frames = (decoded.Length / 2) + (decoded.Length / 4); // across the loop seam
        List<float> mixed = [.. await rig.StartAsync(playback)];
        while (mixed.Count / 2 < frames)
        {
            mixed.AddRange(await StreamingDrive.NextBlockAsync(rig, playback));
        }

        (double[] left, double[] right) = Repeat(decoded, frames);
        float[] expected = DspOracle.Interleave(
            DspOracle.Delay(DspOracle.LowPass(left, 3000), 300, 0.5, 0.4),
            DspOracle.Delay(DspOracle.LowPass(right, 3000), 300, 0.5, 0.4),
            gain: 0.7);
        Assert.Equal(0, rig.Runtime.GetDiagnostics().UnderrunFrames);
        TimbreRig.AssertPcm(expected, mixed.GetRange(0, (int)(frames * 2)).ToArray(), Tolerance);
        Assert.Equal(1, rig.Output.OpenCount);
    }

    // The decoded source looped to `frames` frames, per channel.
    private static (double[] Left, double[] Right) Repeat(float[] decoded, long frames)
    {
        long length = decoded.Length / 2;
        double[] left = new double[frames];
        double[] right = new double[frames];
        for (long frame = 0; frame < frames; frame++)
        {
            left[frame] = decoded[(frame % length) * 2];
            right[frame] = decoded[((frame % length) * 2) + 1];
        }

        return (left, right);
    }
}
