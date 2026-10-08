using Cerneala.Tests.Timbre.Harness;
using Cerneala.Tests.Timbre.Markup;
using Cerneala.Timbre;
using Cerneala.UI.Controls;
using Cerneala.UI.Motion;
using Cerneala.UI.Motion.Core;
using static Cerneala.Tests.Timbre.Motion.SoundMotionTestKit;

namespace Cerneala.Tests.Timbre.Motion;

// Animation time follows the captured playback's transport: it starts at
// the first PCM, holds while paused or seeking and is not restarted by loops.
// Visual Motion keeps its own clock.
public sealed class SoundMotionTransportTests
{
    [Fact]
    public void SoundMotionPauseFreezesTheAnimationAndResumeContinuesIt()
    {
        using MarkupSoundFixture fixture = new(Panel(string.Empty), "SoundMotionPause.crn");
        Button button = PlayButton(fixture);
        SoundPlayback playback = button.Sounds.Play(Resource(fixture, "Tone"), start => start.Volume = 0.2f);
        Wait(() => fixture.Rig.StartAsync(playback));
        MotionHandle audio = playback.Motion().Animate(SoundPlayback.VolumeParameter).To(0.8f).With(Linear(300));
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
    public void SoundMotionTimeStartsAtTheFirstPcmAndAPendingPauseHoldsIt()
    {
        using MarkupSoundFixture fixture = new(Panel(string.Empty), "SoundMotionPending.crn");
        SoundPlayback playback = PlayButton(fixture).Sounds.Play(Resource(fixture, "Tone"), start => start.Volume = 0.2f);
        MotionHandle audio = playback.Motion().Animate(SoundPlayback.VolumeParameter).To(0.8f).With(Linear(300));
        fixture.Pump();
        Advance(fixture, 2);
        Assert.Equal(SoundPlaybackState.Pending, playback.State);
        Assert.Equal(0.2f, playback.Volume);

        playback.Pause();
        Wait(() => fixture.Rig.ReadyAsync(playback));
        fixture.Rig.Output.Release();
        Wait(() => fixture.Rig.SyncAsync());
        Advance(fixture, 2);
        Assert.Equal(SoundPlaybackState.Paused, playback.State);
        Assert.Equal(0.2f, playback.Volume);

        playback.Resume();
        WaitPlaying(fixture, playback);
        Advance(fixture, 2);
        AssertNear(0.5f, playback.Volume);
        Assert.True(audio.IsActive);
    }

    [Fact]
    public void SoundMotionHoldsWhileTheLatestSeekIsPendingAndContinuesWithoutRestart()
    {
        using MarkupSoundFixture fixture = new(Panel(string.Empty), "SoundMotionSeek.crn");
        DeterministicSoundSourceFactory factory = new(10 * 48000);
        SoundPlayback playback = PlayButton(fixture).Sounds.Play(TimbreRig.Clip(factory, SoundLoading.Streaming, volume: 0.2f));
        Wait(() => fixture.Rig.StartAsync(playback));
        playback.Motion().Animate(SoundPlayback.VolumeParameter).To(0.8f).With(Linear(300));
        fixture.Pump();
        Advance(fixture, 1);
        AssertNear(0.35f, playback.Volume);

        TaskCompletionSource gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        DeterministicSoundReader reader = factory.Readers.Single();
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
        Wait(() => HarnessWait.WithTimeout(second, null, "Latest seek did not complete."));
        Advance(fixture, 1);
        AssertNear(0.5f, playback.Volume);
        Assert.Equal(1, fixture.Rig.Runtime.GetDiagnostics().PlaybacksStarted);
    }

    [Fact]
    public void SoundMotionIsNotRestartedWhenALoopingSourceWraps()
    {
        using MarkupSoundFixture fixture = new(Panel(string.Empty), "SoundMotionLoop.crn");
        SoundPlayback playback = PlayButton(fixture).Sounds.Play(
            new SoundClip(SoundSource.FromFile(MarkupSoundFixture.ShortSource), volume: 0.2f, loop: true));
        Wait(() => fixture.Rig.StartAsync(playback));
        MotionHandle audio = playback.Motion().Animate(SoundPlayback.VolumeParameter).To(0.8f).With(Linear(300));
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
        Assert.Equal(SoundPlaybackState.Playing, playback.State);
    }
}
