using Cerneala.Tests.Timbre.Harness;
using Cerneala.Timbre;

namespace Cerneala.Tests.Timbre.Decoding;

// File sources reach the real runtime through the decoders: preload mixes the
// decoded PCM unchanged, the Auto policy sees the decoder's length, and the
// decoder's reservation counts against an explicit memory limit.
public sealed class TimbreDecoderRuntimeTests
{
    [Theory]
    [InlineData("mp3-mpeg1-44100-stereo-cbr128.mp3")]
    [InlineData("vorbis-22050-mono-q2.ogg")]
    [InlineData("opus-stereo-48000-96k.opus")]
    public async Task PreloadedFilePlaysTheDecodedPcm(string name)
    {
        string path = DecodingCorpus.PathOf(name);
        float[] decoded;
        using (TimbreReader reader = DecodingCorpus.Open(path))
        {
            decoded = DecodingCorpus.DecodeAll(reader);
        }

        using TimbreRig rig = new();
        TimbrePlayback playback = rig.Scope.Play(new TimbreSound(TimbreSource.FromFile(path), loading: TimbreLoading.Preload));
        List<float> mixed = [.. await rig.StartAsync(playback)];
        for (int block = 0; block < 16; block++)
        {
            mixed.AddRange(await rig.NextBlockAsync());
        }

        TimbreRig.AssertPcm(decoded[..mixed.Count], [.. mixed], tolerance: 0);
        Assert.Equal(TimbreRig.FramesToTime(decoded.Length / 2), playback.Duration);
    }

    [Fact]
    public async Task WavFilePlaysThroughTheRuntime()
    {
        string path = WavFixture.Write("runtime.wav", 48000, 2, 16, seconds: 0.5);
        using TimbreRig rig = new();
        TimbrePlayback playback = rig.Scope.Play(new TimbreSound(path, loading: TimbreLoading.Preload));

        float[] mixed = await rig.StartAsync(playback);

        using TimbreReader reader = DecodingCorpus.Open(path);
        TimbreRig.AssertPcm(DecodingCorpus.DecodeAll(reader)[..mixed.Length], mixed, tolerance: 0);
    }

    [Fact]
    public async Task AutoLoadingUsesTheDecodedLength()
    {
        using TimbreRig rig = new(hold: false);
        TimbreSound shortClip = new(DecodingCorpus.PathOf("vorbis-48000-stereo-q2.ogg"));
        TimbreSound longClip = new(DecodingCorpus.PathOf("opus-long-mono-32k.opus"));

        await rig.Runtime.PrepareAsync(shortClip);
        await rig.Runtime.PrepareAsync(longClip);

        // 94 976 frames · 8 bytes ≤ 1 MiB are preloaded; 300 s are not.
        Assert.Equal(94976L * 8, rig.Runtime.GetDiagnostics().CacheBytes);
        Assert.Equal(1, rig.Runtime.GetDiagnostics().CacheEntries);
    }

    [Fact]
    public async Task DecoderReservationIsRefusedAgainstAnExplicitLimit()
    {
        using TimbreRig rig = new(options => options.StreamingMemoryLimit = 128 * 1024, hold: false);

        TimbrePlaybackResult result = await TimbreRig.CompletionAsync(rig.Scope.Play(new TimbreSound(DecodingCorpus.PathOf("vorbis-44100-stereo-q4.ogg"))));
        TimbreException prepared = await Assert.ThrowsAsync<TimbreException>(() =>
            rig.Runtime.PrepareAsync(new TimbreSound(DecodingCorpus.PathOf("vorbis-44100-stereo-q4.ogg"))));

        Assert.Equal(TimbreErrorKind.ResourceLimitExceeded, result.Error!.Kind);
        Assert.Equal(TimbreErrorKind.ResourceLimitExceeded, prepared.Kind);
        Assert.Equal(0, rig.Runtime.GetDiagnostics().ReaderMemoryBytes);
        Assert.Equal(0, rig.Runtime.GetDiagnostics().LiveReaders);
    }

    [Fact]
    public void ThirdPartyNoticesShipWithTheDecoders()
    {
        string notice = Path.Combine(AppContext.BaseDirectory, "Cerneala.Timbre.THIRD-PARTY-NOTICES.txt");

        Assert.True(File.Exists(notice), "The decoder license notices are not copied with the Cerneala assembly.");
        string text = File.ReadAllText(notice);
        Assert.Contains("NLayer", text);
        Assert.Contains("NVorbis", text);
        Assert.Contains("Concentus", text);
    }
}
