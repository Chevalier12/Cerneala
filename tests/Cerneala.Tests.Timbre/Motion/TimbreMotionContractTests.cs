using Cerneala.Tests.Timbre.Markup;
using Cerneala.Timbre;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Motion;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Interpolation;
using Cerneala.UI.Motion.Specs;
using static Cerneala.Tests.Timbre.Motion.TimbreMotionTestKit;

namespace Cerneala.Tests.Timbre.Motion;

// Setter, range, terminal, Reduced Motion and ownership rules of the C#
// audio Motion API.
public sealed class TimbreMotionContractTests
{
    [Fact]
    public void TimbreMotionManualSetterCancelsOnlyTheAnimationOfThatParameter()
    {
        using MarkupTimbreFixture fixture = new(Panel(string.Empty), "TimbreMotionSetter.crn");
        TimbrePlayback playback = Start(fixture, 0.2f);
        MotionHandle volume = playback.Motion().Animate(TimbrePlayback.VolumeParameter).To(0.8f).With(Linear(300));
        MotionHandle tone = playback.Motion().Animate(CutOf(playback.Clip)).To(6000f).With(Linear(300));
        fixture.Pump();
        Advance(fixture, 1);

        playback.Volume = 0.1f;
        Advance(fixture, 1);

        Assert.True(volume.IsCanceled);
        Assert.Equal(0.1f, playback.Volume);
        Assert.True(tone.IsActive);
        AssertNear(3450f, Cut(playback), 0.5f);

        playback.Set(TimbrePlayback.VolumeParameter, 0.3f);
        Advance(fixture, 2);
        Assert.Equal(0.3f, playback.Volume);
        Assert.True(tone.IsCompleted);
    }

    [Fact]
    public void TimbreMotionSpringOvershootEndsOnlyThatAnimationOnTheLastValidValue()
    {
        using MarkupTimbreFixture fixture = new(Panel(string.Empty), "TimbreMotionOvershoot.crn");
        TimbrePlayback playback = Start(fixture, 0.5f);
        MotionHandle volume = playback.Motion()
            .Animate(TimbrePlayback.VolumeParameter)
            .To(1f)
            .With(new SpringSpec<float>(stiffness: 400f, damping: 4f, mass: 1f));
        MotionHandle tone = playback.Motion().Animate(CutOf(playback.Clip)).To(6000f).With(Linear(600));
        fixture.Pump();
        for (int frame = 0; frame < 40 && volume.IsActive; frame++)
        {
            fixture.Pump(TimeSpan.FromMilliseconds(16));
        }

        Assert.True(volume.IsCanceled);
        Assert.InRange(playback.Volume, 0.5f, 1f);
        Assert.Equal(1, fixture.Rig.Runtime.GetDiagnostics().MotionSamplesRejected);
        Assert.Equal(1, fixture.Root.Detective.CaptureTimbre()!.MotionSamplesRejected);
        Assert.True(tone.IsActive);
        Assert.Equal(TimbrePlaybackState.Playing, playback.State);
    }

    [Fact]
    public void TimbreMotionNonFiniteSampleIsRejectedWithoutClamping()
    {
        using MarkupTimbreFixture fixture = new(Panel(string.Empty), "TimbreMotionNaN.crn");
        TimbrePlayback playback = Start(fixture, 0.4f);
        MotionHandle handle = playback.Motion().Animate(TimbrePlayback.VolumeParameter).To(0.9f).With(new NaNAfterSpec(TimeSpan.FromMilliseconds(100)));
        fixture.Pump();
        Advance(fixture, 1, 50);
        AssertNear(0.525f, playback.Volume);

        Advance(fixture, 1, 75);

        Assert.True(handle.IsCanceled);
        AssertNear(0.525f, playback.Volume);
        Assert.Equal(1, fixture.Rig.Runtime.GetDiagnostics().MotionSamplesRejected);
    }

    [Fact]
    public void TimbreMotionRejectsInvalidStartsSynchronously()
    {
        using MarkupTimbreFixture fixture = new(Panel(string.Empty), "TimbreMotionArguments.crn");
        TimbrePlayback playback = Start(fixture, 0.4f);
        TimbreParameter<float> foreign = new("Cut", 900f);

        Assert.Throws<ArgumentNullException>(() => ((TimbrePlayback)null!).Motion());
        Assert.Throws<ArgumentNullException>(() => playback.Motion().Animate(null!));
        Assert.Throws<ArgumentException>(() => playback.Motion().Animate(foreign).To(1000f).With(Linear(100)));
        Assert.Throws<ArgumentOutOfRangeException>(() => playback.Motion().Animate(TimbrePlayback.VolumeParameter).To(1.5f).With(Linear(100)));
        Assert.Throws<ArgumentOutOfRangeException>(() => playback.Motion().Animate(CutOf(playback.Clip)).From(float.NaN).To(1000f).With(Linear(100)));
        Assert.Throws<ArgumentOutOfRangeException>(() => playback.Motion().Animate(CutOf(playback.Clip)).To(5f).With(Linear(100)));
        Assert.Equal(0.4f, playback.Volume);
        Assert.Equal(0, playback.AnimatedPublications);

        playback.Cancel();
        Assert.Throws<InvalidOperationException>(() => playback.Motion().Animate(TimbrePlayback.VolumeParameter).To(0.5f).With(Linear(100)));

        using TimbreScope standalone = fixture.Rig.Runtime.CreateScope();
        TimbrePlayback unowned = standalone.Play(Resource(fixture, "Tone"));
        Assert.Throws<InvalidOperationException>(() => unowned.Motion().Animate(TimbrePlayback.VolumeParameter).To(0.5f).With(Linear(100)));
        MotionHandle explicitRoot = unowned.Motion(fixture.Root).Animate(TimbrePlayback.VolumeParameter).To(0.5f).With(Linear(100));
        Assert.True(explicitRoot.IsActive);
        UIRoot other = new();
        Assert.Throws<InvalidOperationException>(() => unowned.Motion(other).Animate(TimbrePlayback.VolumeParameter).To(0.5f).With(Linear(100)));
    }

    [Fact]
    public void TimbreMotionIgnoresTheVisualReducedMotionPreference()
    {
        using MarkupTimbreFixture fixture = new(Panel(string.Empty), "TimbreMotionReduced.crn");
        fixture.Root.Motion.ReducedMotion.SetMode(ReducedMotionMode.Reduce);
        Button button = PlayButton(fixture);
        TimbrePlayback playback = Start(fixture, 0.2f);
        playback.Motion().Animate(TimbrePlayback.VolumeParameter).To(0.8f).With(Linear(300));
        button.Motion().Opacity.To(0f, Linear(300));
        fixture.Pump();
        Advance(fixture, 2);

        AssertNear(0.5f, playback.Volume);
        Assert.Equal(0f, button.Opacity); // visual Motion honours Reduce
    }

    private static TimbrePlayback Start(MarkupTimbreFixture fixture, float volume)
    {
        TimbrePlayback playback = PlayButton(fixture).Timbre.Play(Resource(fixture, "Tone"), start => start.Volume = volume);
        Wait(() => fixture.Rig.StartAsync(playback));
        return playback;
    }

    // A linear sampler that turns non-finite once `after` has elapsed.
    private sealed class NaNAfterSpec(TimeSpan after) : MotionSpec<float>
    {
        public override MotionSampler<float> CreateSampler(float from, float to, ValueMixer<float> mixer, MotionSpecContext context) =>
            new Sampler(from, to, after);

        private sealed class Sampler(float from, float to, TimeSpan after) : MotionSampler<float>
        {
            private TimeSpan elapsed;

            public override float Current => elapsed > after ? float.NaN : from + ((to - from) * (float)(elapsed / (after * 2)));

            public override bool IsComplete => false;

            public override void Advance(TimeSpan delta) => elapsed += delta;

            public override void Retarget(float to, RetargetMode mode)
            {
            }
        }
    }
}
