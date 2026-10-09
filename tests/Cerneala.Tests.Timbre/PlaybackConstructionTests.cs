using Cerneala.Tests.Timbre.Harness;
using Cerneala.Timbre;

namespace Cerneala.Tests.Timbre;

public sealed class PlaybackConstructionTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task PlaybackAndDelayStateAreConstructedOutsideMixerLock()
    {
        using TimbreRig rig = new();
        bool observed = false;
        rig.Runtime.PlaybackConstructed = playback =>
        {
            observed = true;
            Assert.Equal(2L * 96000 * sizeof(float), playback.Render.Chain!.StateBytes);
            Assert.False(Monitor.IsEntered(rig.Runtime.Sync), "Playback/DSP construction holds the mixer lock.");
        };

        TimbrePlayback playback = rig.Scope.Play(DelayClip());
        Assert.True(observed);
        await rig.StartAsync(playback);
        Assert.Equal(playback.Render.Chain!.StateBytes, rig.Runtime.GetDiagnostics().DspStateBytes);
        playback.Cancel();
        rig.Output.Consume(TimbreRig.Block); // capacity for the render-side release fade
        await TimbreRig.ReleasedAsync(playback);
        await rig.SyncAsync();
        Assert.Equal(0, rig.Runtime.GetDiagnostics().DspStateBytes);
    }

    [Fact]
    public async Task MixerCanSubmitWhileAnotherPlaybackIsConstructedButUnpublished()
    {
        using TimbreRig rig = new();
        TimbrePlayback existing = rig.Scope.Play(PlainClip());
        await rig.StartAsync(existing);
        await rig.SyncAsync();
        long submitted = rig.Output.SubmittedFrames;
        using ConstructionGate gate = new(rig.Runtime);
        Task<TimbrePlayback> start = Task.Run(() => rig.Scope.Play(DelayClip()));
        try
        {
            await gate.Entered.WaitAsync(Timeout);
            rig.Output.Consume(TimbreRig.Block);
            await rig.Output.WaitForSubmittedFramesAsync(submitted + TimbreRig.Block).WaitAsync(Timeout);
            Assert.False(start.IsCompleted);
        }
        finally
        {
            gate.Release();
            await start.WaitAsync(Timeout);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DisposalDuringConstructionRejectsPublication(bool disposeRuntime)
    {
        using TimbreRig rig = new();
        using ConstructionGate gate = new(rig.Runtime);
        Task<Exception?> start = Task.Run<Exception?>(() => Record.Exception(() => rig.Scope.Play(DelayClip())));
        try
        {
            await gate.Entered.WaitAsync(Timeout);
            await Task.Run(() =>
            {
                if (disposeRuntime)
                {
                    rig.Runtime.Dispose();
                }
                else
                {
                    rig.Scope.Dispose();
                }
            }).WaitAsync(Timeout);
        }
        finally
        {
            gate.Release();
            await start.WaitAsync(Timeout);
        }

        Assert.IsType<ObjectDisposedException>(await start);
        Assert.Equal(0, rig.Runtime.GetDiagnostics().PlaybacksStarted);
        Assert.Equal(0, rig.Runtime.GetDiagnostics().ActiveVoices);
        Assert.Equal(0, rig.Runtime.GetDiagnostics().DspStateBytes);
        Assert.Equal(0, rig.Output.OpenCount);
    }

    [Fact]
    public async Task VoiceCapacityIsRecheckedAfterConstruction()
    {
        using TimbreRig rig = new(options => options.MaxVoices = 1);
        TimbreHandle handle = rig.Scope.CreateHandle();
        using ConstructionGate gate = new(rig.Runtime);
        Task<Exception?> start = Task.Run<Exception?>(() => Record.Exception(() => rig.Scope.Play(DelayClip(), handle: handle)));
        TimbrePlayback? winner = null;
        try
        {
            await gate.Entered.WaitAsync(Timeout);
            // The pending construction already captured the hook.
            rig.Runtime.PlaybackConstructed = null;
            winner = await Task.Run(() => rig.Scope.Play(PlainClip())).WaitAsync(Timeout);
        }
        finally
        {
            gate.Release();
            await start.WaitAsync(Timeout);
        }

        Assert.Equal(TimbreErrorKind.VoiceLimitExceeded, Assert.IsType<TimbreException>(await start).Kind);
        Assert.Null(handle.Current);
        Assert.False(winner!.Completion.IsCompleted);
        Assert.Equal(1, rig.Runtime.GetDiagnostics().PlaybacksStarted);
        Assert.Equal(1, rig.Runtime.GetDiagnostics().ActiveVoices);
        Assert.Equal(0, rig.Runtime.GetDiagnostics().DspStateBytes);
    }

    [Fact]
    public async Task ReplacementUsesTheHandleOccupantAtPublication()
    {
        using TimbreRig rig = new(options => options.MaxVoices = 1);
        TimbreHandle handle = rig.Scope.CreateHandle();
        TimbrePlayback original = rig.Scope.Play(PlainClip(), handle: handle);
        using ConstructionGate gate = new(rig.Runtime);
        Task<TimbrePlayback> start = Task.Run(() => rig.Scope.Play(DelayClip(), handle: handle));
        TimbrePlayback? interim = null;
        TimbrePlayback replacement;
        try
        {
            await gate.Entered.WaitAsync(Timeout);
            rig.Runtime.PlaybackConstructed = null;
            interim = await Task.Run(() => rig.Scope.Play(PlainClip(), handle: handle)).WaitAsync(Timeout);
            Assert.Same(interim, handle.Current);
        }
        finally
        {
            gate.Release();
            replacement = await start.WaitAsync(Timeout);
        }

        Assert.Same(replacement, handle.Current);
        Assert.Equal(TimbrePlaybackState.Canceled, original.State);
        Assert.Equal(TimbrePlaybackState.Canceled, interim!.State);
        Assert.False(replacement.Completion.IsCompleted);
        Assert.Equal(3, rig.Runtime.GetDiagnostics().PlaybacksStarted);
        Assert.Equal(1, rig.Runtime.GetDiagnostics().ActiveVoices);
        Assert.Equal(replacement.Render.Chain!.StateBytes, rig.Runtime.GetDiagnostics().DspStateBytes);
    }

    [Fact]
    public async Task CanceledHandleDoesNotReserveCapacityDuringConstruction()
    {
        using TimbreRig rig = new(options => options.MaxVoices = 1);
        TimbreHandle handle = rig.Scope.CreateHandle();
        TimbrePlayback original = rig.Scope.Play(PlainClip(), handle: handle);
        using ConstructionGate gate = new(rig.Runtime);
        Task<Exception?> start = Task.Run<Exception?>(() => Record.Exception(() => rig.Scope.Play(DelayClip(), handle: handle)));
        TimbrePlayback? winner = null;
        try
        {
            await gate.Entered.WaitAsync(Timeout);
            rig.Runtime.PlaybackConstructed = null;
            winner = await Task.Run(() =>
            {
                handle.Cancel();
                return rig.Scope.Play(PlainClip());
            }).WaitAsync(Timeout);
        }
        finally
        {
            gate.Release();
            await start.WaitAsync(Timeout);
        }

        Assert.Equal(TimbreErrorKind.VoiceLimitExceeded, Assert.IsType<TimbreException>(await start).Kind);
        Assert.Null(handle.Current);
        Assert.Equal(TimbrePlaybackState.Canceled, original.State);
        Assert.False(winner!.Completion.IsCompleted);
        Assert.Equal(2, rig.Runtime.GetDiagnostics().PlaybacksStarted);
        Assert.Equal(1, rig.Runtime.GetDiagnostics().ActiveVoices);
        Assert.Equal(0, rig.Runtime.GetDiagnostics().DspStateBytes);
    }

    [Fact]
    public void ConstructionFailureLeavesThePreviousOccupantAndAccountingUntouched()
    {
        using TimbreRig rig = new(options => options.MaxVoices = 1);
        TimbreHandle handle = rig.Scope.CreateHandle();
        TimbrePlayback previous = rig.Scope.Play(PlainClip(), handle: handle);
        InvalidOperationException failure = new("Construction failed.");
        rig.Runtime.PlaybackConstructed = _ => throw failure;

        Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => rig.Scope.Play(DelayClip(), handle: handle)));
        Assert.Same(previous, handle.Current);
        Assert.False(previous.Completion.IsCompleted);
        Assert.Equal(1, rig.Runtime.GetDiagnostics().PlaybacksStarted);
        Assert.Equal(1, rig.Runtime.GetDiagnostics().ActiveVoices);
        Assert.Equal(0, rig.Runtime.GetDiagnostics().DspStateBytes);
    }

    [Fact]
    public void AlreadyFullRuntimeRejectsBeforeConstructingPlayback()
    {
        using TimbreRig rig = new(options => options.MaxVoices = 1);
        TimbrePlayback existing = rig.Scope.Play(PlainClip());
        bool constructed = false;
        rig.Runtime.PlaybackConstructed = _ => constructed = true;

        Assert.Equal(TimbreErrorKind.VoiceLimitExceeded,
            Assert.Throws<TimbreException>(() => rig.Scope.Play(DelayClip())).Kind);
        Assert.False(constructed);
        Assert.False(existing.Completion.IsCompleted);
        Assert.Equal(1, rig.Runtime.GetDiagnostics().PlaybacksStarted);
        Assert.Equal(0, rig.Runtime.GetDiagnostics().DspStateBytes);
    }

    private static TimbreSound PlainClip() => TimbreRig.Clip(
        new DeterministicTimbreSourceFactory(48000, (_, _) => 0.25f));

    private static TimbreSound DelayClip() => new(
        TimbreSource.FromReader(new DeterministicTimbreSourceFactory(48000, (_, _) => 0.25f).Open),
        loading: TimbreLoading.Preload,
        modifiers: [new Delay(time: 2f, feedback: 0.5f, mix: 0.5f)]);

    private sealed class ConstructionGate : IDisposable
    {
        private readonly TimbreRuntime runtime;
        private readonly ManualResetEventSlim release = new();
        private readonly TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ConstructionGate(TimbreRuntime runtime)
        {
            this.runtime = runtime;
            runtime.PlaybackConstructed = _ =>
            {
                entered.TrySetResult();
                if (!release.Wait(TimeSpan.FromSeconds(30)))
                {
                    throw new TimeoutException("Construction gate was not released.");
                }
            };
        }

        public Task Entered => entered.Task;

        public void Release() => release.Set();

        public void Dispose()
        {
            runtime.PlaybackConstructed = null;
            release.Dispose();
        }
    }
}
