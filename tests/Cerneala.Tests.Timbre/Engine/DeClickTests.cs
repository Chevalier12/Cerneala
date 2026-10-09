using Cerneala.Tests.Timbre.Harness;
using Cerneala.Timbre;
using Xunit.Abstractions;

namespace Cerneala.Tests.Timbre.Engine;

public sealed class DeClickTests(ITestOutputHelper output)
{
    // Independent contract: rounded 5 ms at the output rate, not a mixer constant.
    private static readonly int FadeFrames = (int)Math.Round(0.005 * TimbreRuntime.SampleRate);
    private const double Frequency = 100;
    private static float Sine(long frame, int channel) =>
        (float)Math.Sin(2 * Math.PI * Frequency * frame / TimbreRuntime.SampleRate + Math.PI / 2);

    public static IEnumerable<object[]> Transitions() =>
        from loading in new[] { TimbreLoading.Preload, TimbreLoading.Streaming }
        from transition in new[] { "volume", "pause", "resume", "seek", "cancel", "stop", "replace" }
        select new object[] { transition, loading };

    [Theory]
    [MemberData(nameof(Transitions))]
    public async Task FullScaleSineHasBoundedDiscontinuity(string transition, TimbreLoading loading)
    {
        using TimbreRig rig = new();
        TimbreHandle handle = rig.Scope.CreateHandle();
        TimbrePlayback voice = rig.Scope.Play(TimbreRig.Clip(new DeterministicTimbreSourceFactory(48000, Sine), loading), handle: handle);
        TimbrePlayback clock = rig.Scope.Play(TimbreRig.Clip(new DeterministicTimbreSourceFactory(48000, (_, _) => 0)));
        float[] initial = await rig.StartAsync(voice, clock);
        float previous = initial[^2];
        Task? seek = null;
        switch (transition)
        {
            case "volume": voice.Volume = 0.25f; break;
            case "pause": voice.Pause(); break;
            case "resume":
                voice.Pause();
                float[] fade = await rig.NextBlockAsync();
                previous = fade[^2];
                voice.Resume();
                break;
            case "seek": seek = voice.SeekAsync(TimbreRig.FramesToTime(24240)); break;
            case "cancel": voice.Cancel(); break;
            case "stop": handle.Cancel(); break;
            case "replace":
                TimbrePlayback replacement = rig.Scope.Play(
                    TimbreRig.Clip(new DeterministicTimbreSourceFactory(48000, (frame, channel) => -Sine(frame, channel)), loading), handle: handle);
                await rig.ReadyAsync(replacement);
                await rig.SettledAsync(replacement);
                break;
        }

        float[] block = await rig.NextBlockAsync();
        AssertJump(block, previous, transition == "replace" ? 2 : 1, transition);
        float[] expected = transition switch
        {
            "volume" => TimbreRig.ExpectedRamp(TimbreRig.Block, Sine, 1f, 0.25f, TimbreRig.Budget),
            "resume" => TimbreRig.ExpectedRamp(TimbreRig.Block, Sine, 0f, 1f, TimbreRig.Budget + FadeFrames),
            "replace" => TimbreRig.Sum(
                TimbreRig.ExpectedRamp(TimbreRig.Block, Sine, 1f, 0f, TimbreRig.Budget),
                TimbreRig.ExpectedRamp(TimbreRig.Block, (frame, channel) => -Sine(frame, channel), 0f, 1f)),
            _ => TimbreRig.ExpectedRamp(TimbreRig.Block, Sine, 1f, 0f, TimbreRig.Budget)
        };
        TimbreRig.AssertPcm(expected, block);
        if (transition is "pause" or "seek" or "cancel" or "stop")
        {
            Assert.All(block.Skip((FadeFrames - 1) * 2), sample => Assert.Equal(0f, sample));
        }
        if (transition is "pause" or "cancel" or "stop")
        {
            Assert.Equal(TimbreRig.FramesToTime(TimbreRig.Budget + FadeFrames), voice.Position);
            float[] after = await rig.NextBlockAsync();
            Assert.All(after, sample => Assert.Equal(0f, sample));
            Assert.Equal(TimbreRig.FramesToTime(TimbreRig.Budget + FadeFrames), voice.Position);
        }
        if (transition == "volume")
        {
            for (int frame = FadeFrames - 1; frame < TimbreRig.Block; frame++)
                Assert.Equal(Sine(TimbreRig.Budget + frame, 0) * 0.25f, block[frame * 2], 5);
        }
        if (seek is not null)
        {
            await HarnessWait.WithTimeout(seek, null, "Faded seek did not complete.");
            await rig.SettledAsync(voice);
            Assert.Equal(TimbreRig.FramesToTime(24240), voice.Position);
            float[] incoming = await rig.NextBlockAsync();
            AssertJump(incoming, block[^2], 1, "seek-in");
            TimbreRig.AssertPcm(TimbreRig.ExpectedRamp(TimbreRig.Block, Sine, 0f, 1f, 24240), incoming);
        }
        if (transition is "cancel" or "stop" or "replace")
            await TimbreRig.ReleasedAsync(voice);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TransportPublishedDuringCapacityReadFadesBeforeTheNextRender(bool cancel)
    {
        using TimbreRig rig = new();
        TimbrePlayback voice = rig.Scope.Play(TimbreRig.Clip(new DeterministicTimbreSourceFactory(48000, Sine)));
        await rig.StartAsync(voice);
        await rig.SyncAsync();
        rig.Output.BeforeNextQueueRead = () =>
        {
            if (cancel) voice.Cancel(); else voice.Pause();
        };
        rig.Output.Consume(TimbreRig.Block);
        await rig.Output.WaitForSubmittedFramesAsync(TimbreRig.Budget + TimbreRig.Block);
        await rig.SyncAsync();
        Assert.Equal(TimbreRig.FramesToTime(TimbreRig.Budget + FadeFrames), voice.Position);
        TimbreRig.AssertPcm(TimbreRig.ExpectedRamp(TimbreRig.Block, Sine, 1f, 0f, TimbreRig.Budget),
            rig.Output.Read(TimbreRig.Budget, TimbreRig.Block));
    }

    [Fact]
    public async Task SeekFromAlreadyEndedProductionFadesInFromSilence()
    {
        using TimbreRig rig = new();
        TimbrePlayback voice = rig.Scope.Play(TimbreRig.Clip(new DeterministicTimbreSourceFactory(1000, Sine)));
        TimbrePlayback clock = rig.Scope.Play(TimbreRig.Clip(new DeterministicTimbreSourceFactory(48000, (_, _) => 0f)));
        float[] initial = await rig.StartAsync(voice, clock);
        await rig.SyncAsync();
        Assert.True(voice.Render.ProductionEnded);
        Assert.Equal(TimbrePlaybackState.Playing, voice.State); // old queue has not drained
        await HarnessWait.WithTimeout(voice.SeekAsync(TimeSpan.Zero), null, "Seek from ended production did not complete.");
        float[] incoming = await rig.NextBlockAsync();
        AssertJump(incoming, initial[^2], 1, "ended-seek-in");
        TimbreRig.AssertPcm(TimbreRig.ExpectedRamp(TimbreRig.Block, Sine, 0f, 1f), incoming);
    }

    private void AssertJump(float[] block, float previous, int voices, string label)
    {
        double maximum = 0;
        for (int frame = 0; frame < block.Length / 2; frame++)
        {
            maximum = Math.Max(maximum, Math.Abs(block[frame * 2] - previous));
            Assert.Equal(block[frame * 2], block[frame * 2 + 1]);
            previous = block[frame * 2];
        }
        // |Δ(s*g)| <= |Δs| + |Δg| for unit amplitude and gain <= 1.
        double bound = voices * (2 * Math.Sin(Math.PI * Frequency / TimbreRuntime.SampleRate) + 1.0 / FadeFrames) + 1e-6;
        output.WriteLine($"{label}: max jump={maximum:R}, bound={bound:R}, fade frames={FadeFrames}");
        Assert.True(maximum <= bound, $"{label}: maximum jump {maximum:R} exceeds signal-derived bound {bound:R}.");
    }

    [Fact]
    public async Task SeekIsNotCanceledWhenTheOldSourceEndsDuringItsFadeAndDrains()
    {
        using TimbreRig rig = new();
        GatedSeekReader reader = new(TimbreRig.Budget + FadeFrames); // owned/disposed by the runtime
        TimbrePlayback voice = rig.Scope.Play(new TimbreSound(TimbreSource.FromReader(() => reader), loading: TimbreLoading.Streaming));
        await rig.StartAsync(voice);
        Task seek = voice.SeekAsync(TimeSpan.Zero);
        await rig.NextBlockAsync();
        await HarnessWait.WithTimeout(reader.Seeking, null, "Seek was not dispatched after the fade.");
        rig.Output.ConsumeAll();
        await rig.SyncAsync();
        Assert.Equal(TimbrePlaybackState.Playing, voice.State);
        Assert.False(seek.IsCompleted);
        reader.Open();
        await HarnessWait.WithTimeout(seek, null, "Seek did not complete after draining the old fade.");
    }

    private sealed class GatedSeekReader(int frames) : TimbreReader
    {
        private readonly DeterministicTimbreReader source = new(frames, Sine);
        private readonly TaskCompletionSource seeking = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource open = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task Seeking => seeking.Task;
        public void Open() => open.TrySetResult();
        public override long? LengthFrames => frames;
        public override ValueTask<TimbreReadResult> ReadAsync(Memory<float> destination, CancellationToken token) => source.ReadAsync(destination, token);
        public override async ValueTask SeekAsync(long frame, CancellationToken token)
        {
            seeking.TrySetResult();
            await open.Task.WaitAsync(token);
            await source.SeekAsync(frame, token);
        }
        protected override void Dispose(bool disposing) => source.Dispose();
    }
}
