using Cerneala.Tests.Timbre.Markup;
using Cerneala.Timbre;
using Cerneala.UI.Controls;
using Cerneala.UI.Motion;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Properties;
using Cerneala.UI.Motion.Specs;
using Cerneala.UI.Timbre;
using static Cerneala.Tests.Timbre.Motion.SoundMotionTestKit;

namespace Cerneala.Tests.Timbre.Motion;

// Deterministic manual-clock timelines of audio Motion on the root graph:
// values, From/current, conflicts, hold and the approved spec families.
public sealed class SoundMotionTimelineTests
{
    [Fact]
    public void SoundMotionTweenSamplesInitialIntermediateAndFinalValuesAndHoldsTheEnd()
    {
        using MarkupSoundFixture fixture = new(Panel(string.Empty), "SoundMotionTween.crn");
        SoundPlayback playback = Start(fixture, 0.2f);
        SoundParameter<float> cut = CutOf(playback.Clip);

        MotionHandle volume = playback.Motion().Animate(SoundPlayback.VolumeParameter).To(0.8f).With(Linear(300));
        MotionHandle tone = playback.Motion().Animate(cut).To(6000f).With(Linear(300));
        fixture.Pump();
        AssertNear(0.2f, playback.Volume);
        AssertNear(900f, Cut(playback));

        List<(float Volume, float Cut)> samples = [];
        for (int frame = 0; frame < 4; frame++)
        {
            fixture.Pump(TimeSpan.FromMilliseconds(75));
            samples.Add((playback.Volume, Cut(playback)));
        }

        AssertNear(0.35f, samples[0].Volume);
        AssertNear(0.5f, samples[1].Volume);
        AssertNear(0.65f, samples[2].Volume);
        AssertNear(0.8f, samples[3].Volume);
        AssertNear(2175f, samples[0].Cut, 0.5f);
        AssertNear(6000f, samples[3].Cut, 0.5f);
        Assert.True(volume.IsCompleted);
        Assert.True(tone.IsCompleted);

        fixture.Pump(TimeSpan.FromMilliseconds(75));
        Assert.Equal(0.8f, playback.Volume);
        Assert.Equal(0, SoundPlaybackMotion.ActiveTargets(fixture.Root));
    }

    [Fact]
    public void SoundMotionFromIsPublishedAtStartAndHoldOffRestoresThePreviousValue()
    {
        using MarkupSoundFixture fixture = new(Panel(string.Empty), "SoundMotionFrom.crn");
        Button button = PlayButton(fixture);
        SoundPlayback playback = button.Sounds.Play(Resource(fixture, "Tone"), start => start.Volume = 0.6f);

        playback.Motion()
            .Animate(SoundPlayback.VolumeParameter)
            .From(0f)
            .To(1f)
            .With(Linear(150), new MotionPropertyStartOptions { HoldOnComplete = false });

        Assert.Equal(SoundPlaybackState.Pending, playback.State);
        Assert.Equal(0f, playback.Volume); // From is published before the first PCM
        WaitPlaying(fixture, playback);
        fixture.Pump();
        Advance(fixture, 1);
        AssertNear(0.5f, playback.Volume);
        Advance(fixture, 2);

        Assert.Equal(0.6f, playback.Volume);
    }

    [Fact]
    public void SoundMotionRetargetsFromTheCurrentSampleAndRespectsPriority()
    {
        using MarkupSoundFixture fixture = new(Panel(string.Empty), "SoundMotionConflict.crn");
        SoundPlayback playback = Start(fixture, 0.2f);

        MotionHandle first = playback.Motion().Animate(SoundPlayback.VolumeParameter).To(0.8f).With(Linear(300));
        fixture.Pump();
        Advance(fixture, 2);
        AssertNear(0.5f, playback.Volume);

        MotionHandle second = playback.Motion().Animate(SoundPlayback.VolumeParameter).To(0.2f).With(Linear(300));
        Assert.True(first.IsCanceled);
        Advance(fixture, 2);
        AssertNear(0.35f, playback.Volume);

        MotionHandle high = playback.Motion()
            .Animate(SoundPlayback.VolumeParameter)
            .To(1f)
            .With(Linear(300), new MotionPropertyStartOptions { HoldOnComplete = true, Priority = MotionPriority.ReducedMotion });
        MotionHandle rejected = playback.Motion().Animate(SoundPlayback.VolumeParameter).To(0f).With(Linear(300));

        Assert.True(second.IsCanceled);
        Assert.True(rejected.IsCanceled);
        Assert.True(high.IsActive);
        Advance(fixture, 2);
        AssertNear(0.675f, playback.Volume);
    }

    [Fact]
    public void SoundMotionAcceptsSpringKeyframesAndRepeatSpecs()
    {
        using MarkupSoundFixture fixture = new(Panel(string.Empty), "SoundMotionSpecs.crn");
        SoundPlayback playback = Start(fixture, 0f);
        SoundParameter<float> cut = CutOf(playback.Clip);

        MotionHandle spring = playback.Motion().Animate(cut).To(5000f).With(Cerneala.UI.Motion.Specs.Motion.Spring<float>());
        MotionHandle keyframes = playback.Motion()
            .Animate(SoundPlayback.VolumeParameter)
            .With(new KeyframesSpec<float>(
                [
                    new MotionKeyframe<float>(0f, 0f, Easings.Linear),
                    new MotionKeyframe<float>(0.5f, 1f, Easings.Linear),
                    new MotionKeyframe<float>(1f, 0.4f, Easings.Linear)
                ],
                TimeSpan.FromMilliseconds(300)));
        fixture.Pump();
        Advance(fixture, 2);
        AssertNear(1f, playback.Volume);
        Advance(fixture, 2);
        AssertNear(0.4f, playback.Volume);

        for (int frame = 0; frame < 60 && spring.IsActive; frame++)
        {
            fixture.Pump(TimeSpan.FromMilliseconds(50));
        }

        Assert.True(keyframes.IsCompleted);
        Assert.True(spring.IsCompleted);
        AssertNear(5000f, Cut(playback), 1f);
    }

    private static SoundPlayback Start(MarkupSoundFixture fixture, float volume)
    {
        SoundPlayback playback = PlayButton(fixture).Sounds.Play(Resource(fixture, "Tone"), start => start.Volume = volume);
        Wait(() => fixture.Rig.StartAsync(playback));
        return playback;
    }
}
