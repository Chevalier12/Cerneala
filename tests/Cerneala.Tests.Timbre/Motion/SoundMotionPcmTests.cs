using Cerneala.Tests.Timbre.Harness;
using Cerneala.Tests.Timbre.Markup;
using Cerneala.Timbre;
using Cerneala.UI.Controls;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Motion;
using static Cerneala.Tests.Timbre.Motion.SoundMotionTestKit;

namespace Cerneala.Tests.Timbre.Motion;

// Motion samples reach the DSP only of the captured playback, at block
// boundaries, and cost no layout or render work.
public sealed class SoundMotionPcmTests
{
    private const int Steps = 20;

    // The mixer sums voices linearly before the output limit, so with the
    // same block schedule mix(A animated + B) must equal mix(B) + mix(A
    // animated): B's PCM is untouched by A's Volume, cutoff and echo
    // animation, and A animated alone differs from A static.
    [Fact]
    public void SoundMotionPublicationsReachOnlyTheCapturedPlaybackPcm()
    {
        float[][] both = Run(includeA: true, animateA: true, includeB: true);
        float[][] onlyB = Run(includeA: false, animateA: false, includeB: true);
        float[][] onlyAnimated = Run(includeA: true, animateA: true, includeB: false);
        float[][] onlyStatic = Run(includeA: true, animateA: false, includeB: false);

        for (int step = 0; step < Steps; step++)
        {
            for (int sample = 0; sample < both[step].Length; sample++)
            {
                Assert.InRange(both[step][sample] - (onlyB[step][sample] + onlyAnimated[step][sample]), -1e-6f, 1e-6f);
            }
        }

        Assert.True(onlyAnimated[0].SequenceEqual(onlyStatic[0]), "The block mixed before the first sample must use the start values.");
        Assert.True(Enumerable.Range(1, Steps - 1).Count(step => !onlyAnimated[step].SequenceEqual(onlyStatic[step])) >= Steps - 2);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SoundMotionFramesDoNotMeasureArrangeOrInvalidateRender(bool hidden)
    {
        using MarkupSoundFixture fixture = new(Panel(string.Empty), "SoundMotionFrames.crn");
        Button button = PlayButton(fixture);
        if (hidden)
        {
            button.Visibility = Visibility.Collapsed;
            fixture.Pump();
            fixture.Pump();
        }

        SoundPlayback playback = button.Sounds.Play(Resource(fixture, "Tone"), start => start.Volume = 0.2f);
        Wait(() => fixture.Rig.StartAsync(playback));
        playback.Motion().Animate(SoundPlayback.VolumeParameter).To(0.8f).With(Linear(300));
        playback.Motion().Animate(CutOf(playback.Clip)).To(6000f).With(Linear(300));
        fixture.Pump();

        int valuesChanged = 0;
        for (int frame = 0; frame < 4; frame++)
        {
            fixture.Clock.Advance(TimeSpan.FromMilliseconds(75));
            UiFrame result = fixture.Host.Update(
                new InputFrame(PointerSnapshot.Empty, PointerSnapshot.Empty, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []),
                fixture.Host.Viewport,
                TimeSpan.Zero);
            FrameStats stats = result.Stats;
            Assert.Equal(0, stats.MeasureCalls);
            Assert.Equal(0, stats.ArrangeCalls);
            Assert.Equal(0, stats.RenderedElements);
            Assert.Equal(0, stats.MotionRenderInvalidations);
            Assert.Equal(0, stats.MotionLayoutInvalidations);
            Assert.Equal(0, stats.MotionPropertyWrites);
            valuesChanged += stats.MotionValuesChanged;
        }

        Assert.Equal(8, valuesChanged); // Volume and Cut, four frames
        AssertNear(0.8f, playback.Volume);
    }

    private static float[][] Run(bool includeA, bool animateA, bool includeB)
    {
        using MarkupSoundFixture fixture = new(Panel(string.Empty), "SoundMotionPcm.crn");
        Button button = PlayButton(fixture);
        List<SoundPlayback> playbacks = [];
        SoundPlayback? a = null;
        if (includeA)
        {
            a = button.Sounds.Play(Chain(new DeterministicSoundSourceFactory(10 * 48000), SoundLoading.Streaming), start => start.Volume = 0.3f);
            playbacks.Add(a);
        }

        if (includeB)
        {
            playbacks.Add(button.Sounds.Play(
                Chain(new DeterministicSoundSourceFactory(10 * 48000, (frame, channel) => channel == 0 ? 0.25f : -0.125f), SoundLoading.Preload),
                start => start.Volume = 0.4f));
        }

        Wait(() => fixture.Rig.StartAsync(playbacks.ToArray()));
        if (animateA)
        {
            a!.Motion().Animate(SoundPlayback.VolumeParameter).To(0.6f).With(Linear(300));
            a.Motion().Animate(Parameter(a.Clip, "Cut")).To(300f).With(Linear(300));
            a.Motion().Animate(Parameter(a.Clip, "Mix")).To(0.8f).With(Linear(300));
        }

        fixture.Pump();
        List<float[]> blocks = [];
        for (int step = 0; step < Steps; step++)
        {
            float[] block = [];
            Wait(async () => block = await fixture.Rig.NextBlockAsync());
            blocks.Add(block);
            fixture.Pump(TimeSpan.FromMilliseconds(15));
        }

        return blocks.ToArray();
    }

    private static SoundClip Chain(DeterministicSoundSourceFactory factory, SoundLoading loading)
    {
        SoundParameter<float> cut = new("Cut", 4000f);
        SoundParameter<float> mix = new("Mix", 0.2f);
        return new SoundClip(
            SoundSource.FromReader(factory.Open, "deterministic"),
            loading: loading,
            parameters: [cut, mix],
            modifiers: [new LowPass(cut), new Delay(time: 0.01f, feedback: 0.3f, mix: mix)]);
    }

    private static SoundParameter<float> Parameter(SoundClip clip, string name) =>
        (SoundParameter<float>)clip.Parameters.Single(parameter => parameter.Name == name);
}
