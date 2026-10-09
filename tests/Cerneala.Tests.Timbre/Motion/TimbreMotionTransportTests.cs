using Cerneala.Tests.Timbre.Harness;
using Cerneala.Tests.Timbre.Markup;
using Cerneala.Timbre;
using Cerneala.UI.Controls;
using Cerneala.UI.Motion;
using Cerneala.UI.Motion.Core;
using static Cerneala.Tests.Timbre.Motion.TimbreMotionTestKit;

namespace Cerneala.Tests.Timbre.Motion;

// Animation time follows the captured playback's transport: it starts at
// the first PCM, holds while paused or seeking and is not restarted by loops.
// Visual Motion keeps its own clock.
public sealed class TimbreMotionTransportTests
{
    [Fact]
    public void TimbreMotionPauseFreezesTheAnimationAndResumeContinuesIt()
    {
        using MarkupTimbreFixture fixture = new(Panel(string.Empty), "TimbreMotionPause.crn");
        Button button = PlayButton(fixture);
        TimbrePlayback playback = button.Timbre.Play(Resource(fixture, "Tone"), start => start.Volume = 0.2f);
        Wait(() => fixture.Rig.StartAsync(playback));
        MotionHandle audio = playback.Motion().Animate(TimbrePlayback.VolumeParameter).To(0.8f).With(Linear(300));
        MotionHandle visual = button.Motion().Opacity.To(0f, Linear(300));
        fixture.Pump();
        Advance(fixture, 1);
        AssertNear(0.35f, playback.Volume);

        playback.Pause();
        Advance(fixture, 2);
        AssertNear(0.35f, playback.Volume);
        AssertNear(0.25f, button.Opacity); // visual Motion is not suspended by audio transport

        playback.Resume();
        Advance(fixture, 1);
        AssertNear(0.5f, playback.Volume);
        Assert.True(audio.IsActive);
        Assert.True(visual.IsCompleted);
    }

    [Fact]
    public void TimbreMotionTimeStartsAtTheFirstPcmAndAPendingPauseHoldsIt()
    {
        using MarkupTimbreFixture fixture = new(Panel(string.Empty), "TimbreMotionPending.crn");
        TimbrePlayback playback = PlayButton(fixture).Timbre.Play(Resource(fixture, "Tone"), start => start.Volume = 0.2f);
        MotionHandle audio = playback.Motion().Animate(TimbrePlayback.VolumeParameter).To(0.8f).With(Linear(300));
        fixture.Pump();
        Advance(fixture, 2);
        Assert.Equal(TimbrePlaybackState.Pending, playback.State);
        Assert.Equal(0.2f, playback.Volume);

        playback.Pause();
        Wait(() => fixture.Rig.ReadyAsync(playback));
        fixture.Rig.Output.Release();
        Wait(() => fixture.Rig.SyncAsync());
        Advance(fixture, 2);
        Assert.Equal(TimbrePlaybackState.Paused, playback.State);
        Assert.Equal(0.2f, playback.Volume);

        playback.Resume();
        WaitPlaying(fixture, playback);
        Advance(fixture, 2);
        AssertNear(0.5f, playback.Volume);
        Assert.True(audio.IsActive);
    }

    [Fact]
    public void TimbreMotionHoldsWhileTheLatestSeekIsPendingAndContinuesWithoutRestart()
    {
        using MarkupTimbreFixture fixture = new(Panel(string.Empty), "TimbreMotionSeek.crn");
        DeterministicTimbreSourceFactory factory = new(10 * 48000);
        TimbrePlayback playback = PlayButton(fixture).Timbre.Play(TimbreRig.Clip(factory, TimbreLoading.Streaming, volume: 0.2f));
        Wait(() => fixture.Rig.StartAsync(playback));
        playback.Motion().Animate(TimbrePlayback.VolumeParameter).To(0.8f).With(Linear(300));
        fixture.Pump();
        Advance(fixture, 1);
        AssertNear(0.35f, playback.Volume);

        TaskCompletionSource gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        DeterministicTimbreReader reader = factory.Readers.Single();
        reader.ReadGate = token => gate.Task.WaitAsync(token);
        int reads = reader.ReadCount;
        fixture.Rig.Output.ConsumeAll();
        Wait(() => reader.WaitForReadCountAsync(reads + 1));
        Task first = playback.SeekAsync(TimbreRig.FramesToTime(10000));
        Task second = playback.SeekAsync(TimbreRig.FramesToTime(30000));
        Advance(fixture, 2);

        Assert.True(first.IsCanceled);
        Assert.False(second.IsCompleted);
        AssertNear(0.35f, playback.Volume);

        reader.ReadGate = null;
        gate.SetResult();
        fixture.Rig.Output.Consume(TimbreRig.Block); // capacity for the old-position release fade
        Wait(() => HarnessWait.WithTimeout(second, null, "Latest seek did not complete."));
        Advance(fixture, 1);
        AssertNear(0.5f, playback.Volume);
        Assert.Equal(1, fixture.Rig.Runtime.GetDiagnostics().PlaybacksStarted);
    }

    [Fact]
    public void TimbreMotionIsNotRestartedWhenALoopingSourceWraps()
    {
        using MarkupTimbreFixture fixture = new(Panel(string.Empty), "TimbreMotionLoop.crn");
        TimbrePlayback playback = PlayButton(fixture).Timbre.Play(
            new TimbreSound(TimbreSource.FromFile(MarkupTimbreFixture.ShortSource), volume: 0.2f, loop: true));
        Wait(() => fixture.Rig.StartAsync(playback));
        MotionHandle audio = playback.Motion().Animate(TimbrePlayback.VolumeParameter).To(0.8f).With(Linear(300));
        fixture.Pump();
        List<float> volumes = [];
        for (int frame = 0; frame < 4; frame++)
        {
            Wait(() => fixture.Rig.NextBlockAsync());
            fixture.Pump(TimeSpan.FromMilliseconds(75));
            volumes.Add(playback.Volume);
        }

        Assert.True(fixture.Rig.Runtime.GetDiagnostics().LoopWraps > 4);
        Assert.Equal(volumes.OrderBy(volume => volume), volumes);
        AssertNear(0.8f, volumes[3]);
        Assert.True(audio.IsCompleted);
        Assert.Equal(TimbrePlaybackState.Playing, playback.State);
    }
}
