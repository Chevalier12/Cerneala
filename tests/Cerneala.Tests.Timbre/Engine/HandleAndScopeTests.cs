using Cerneala.Tests.Timbre.Harness;
using Cerneala.Timbre;

namespace Cerneala.Tests.Timbre.Engine;

public sealed class HandleAndScopeTests
{
    private static readonly Func<long, int, float> Signal = DeterministicTimbreReader.DefaultSignal;

    [Fact]
    public async Task PlayWithoutHandleOverlapsAndCancelTargetsOneIdentity()
    {
        using TimbreRig rig = new();
        TimbreClip clip = TimbreRig.Clip(new DeterministicTimbreSourceFactory(48000));
        TimbrePlayback first = rig.Scope.Play(clip);
        TimbrePlayback second = rig.Scope.Play(clip);
        await rig.StartAsync(first, second);

        first.Cancel();
        float[] block = await rig.NextBlockAsync();

        Assert.Equal(TimbrePlaybackState.Canceled, first.State);
        Assert.Equal(TimbrePlaybackState.Playing, second.State);
        TimbreRig.AssertPcm(TimbreRig.Expected(TimbreRig.Block, Signal, 1f, TimbreRig.Budget), block);
        Assert.Equal(TimbrePlaybackState.Canceled, (await TimbreRig.CompletionAsync(first)).State);
    }

    [Fact]
    public async Task PlayWithSameHandleReplacesOnlyTheOccupant()
    {
        using TimbreRig rig = new();
        TimbreClip clip = TimbreRig.Clip(new DeterministicTimbreSourceFactory(48000));
        TimbreHandle slot = rig.Scope.CreateHandle();
        TimbrePlayback old = rig.Scope.Play(clip, handle: slot);
        TimbrePlayback free = rig.Scope.Play(clip);
        Assert.Same(old, slot.Current);
        Assert.Same(rig.Scope, slot.Scope);

        TimbrePlayback current = rig.Scope.Play(clip, start => start.Volume = 0.5f, handle: slot);

        Assert.Equal(TimbrePlaybackState.Canceled, old.State);
        Assert.Same(current, slot.Current);
        float[] pcm = await rig.StartAsync(free, current);
        TimbreRig.AssertPcm(TimbreRig.Expected(TimbreRig.Budget, Signal, 1.5f), pcm);
        Assert.Equal(TimbrePlaybackState.Playing, free.State);
    }

    [Fact]
    public async Task CancelOnStaleReferenceDoesNotCancelTheNewOccupant()
    {
        using TimbreRig rig = new();
        TimbreClip clip = TimbreRig.Clip(new DeterministicTimbreSourceFactory(48000));
        TimbreHandle slot = rig.Scope.CreateHandle();
        TimbrePlayback old = rig.Scope.Play(clip, handle: slot);
        TimbrePlayback current = rig.Scope.Play(clip, handle: slot);

        old.Cancel();
        old.Cancel();

        Assert.Equal(TimbrePlaybackState.Pending, current.State);
        Assert.Same(current, slot.Current);
        await rig.StartAsync(current);
        Assert.Equal(TimbrePlaybackState.Playing, current.State);

        slot.Cancel();
        Assert.Equal(TimbrePlaybackState.Canceled, current.State);
        Assert.Null(slot.Current);
        slot.Cancel(); // empty slot: no-op
        Assert.Null(slot.Current);
    }

    [Fact]
    public void EmptySlotCancelIsNoOp()
    {
        using TimbreRig rig = new();
        TimbreHandle slot = rig.Scope.CreateHandle();
        slot.Cancel();
        Assert.Null(slot.Current);
        Assert.Equal(0, rig.Runtime.GetDiagnostics().PlaybacksStarted);
    }

    [Fact]
    public void SynchronousStartFailuresKeepTheOccupantAndCreateNoIdentity()
    {
        using TimbreRig rig = new();
        TimbreParameter<float> declared = new("Declared", 1f);
        TimbreParameter<float> foreign = new("Foreign", 1f);
        TimbreClip clip = new(TimbreSource.FromReader(new DeterministicTimbreSourceFactory(48000).Open), parameters: [declared]);
        TimbreHandle slot = rig.Scope.CreateHandle();
        TimbrePlayback occupant = rig.Scope.Play(clip, handle: slot);
        using TimbreScope otherScope = rig.Runtime.CreateScope();
        TimbreHandle foreignSlot = otherScope.CreateHandle();
        InvalidOperationException thrown = new("configure failed");

        Assert.Same(thrown, Assert.Throws<InvalidOperationException>(() => rig.Scope.Play(clip, _ => throw thrown, slot)));
        Assert.Throws<ArgumentException>(() => rig.Scope.Play(clip, start => start.Set(foreign, 1f), slot));
        Assert.Throws<ArgumentOutOfRangeException>(() => rig.Scope.Play(clip, start => start.Set(declared, float.NaN), slot));
        Assert.Throws<ArgumentOutOfRangeException>(() => rig.Scope.Play(clip, start => start.Volume = 2f, slot));
        Assert.Throws<ArgumentOutOfRangeException>(() => rig.Scope.Play(clip, start => start.Volume = float.PositiveInfinity, slot));
        Assert.Throws<ArgumentException>(() => rig.Scope.Play(clip, handle: foreignSlot));
        Assert.Throws<ArgumentNullException>(() => rig.Scope.Play(null!, handle: slot));

        Assert.Same(occupant, slot.Current);
        Assert.Equal(TimbrePlaybackState.Pending, occupant.State);
        Assert.Equal(1, rig.Runtime.GetDiagnostics().PlaybacksStarted);
    }

    [Fact]
    public void ModifiedClipStartsAndReplacesThroughTheSamePathAsAPlainClip()
    {
        using TimbreRig rig = new();
        TimbreClip plain = TimbreRig.Clip(new DeterministicTimbreSourceFactory(48000));
        TimbreClip filtered = new(plain.Source, modifiers: [new LowPass()]);
        TimbreHandle slot = rig.Scope.CreateHandle();
        TimbrePlayback occupant = rig.Scope.Play(plain, handle: slot);

        TimbrePlayback replacement = rig.Scope.Play(filtered, handle: slot);

        Assert.Equal(TimbrePlaybackState.Canceled, occupant.State);
        Assert.Same(replacement, slot.Current);
        Assert.Equal(TimbrePlaybackState.Pending, replacement.State);
    }

    [Fact]
    public async Task VoiceLimitCountsPausedPlaybacksAndRejectsWithoutStealing()
    {
        using TimbreRig rig = new(options => options.MaxVoices = 2);
        TimbreClip clip = TimbreRig.Clip(new DeterministicTimbreSourceFactory(48000));
        TimbreHandle slot = rig.Scope.CreateHandle();
        TimbrePlayback playing = rig.Scope.Play(clip);
        TimbrePlayback paused = rig.Scope.Play(clip, handle: slot);
        paused.Pause();

        TimbreException rejected = Assert.Throws<TimbreException>(() => rig.Scope.Play(clip));
        Assert.Equal(TimbreErrorKind.VoiceLimitExceeded, rejected.Kind);
        Assert.Equal(TimbrePlaybackState.Pending, playing.State);
        Assert.Equal(TimbrePlaybackState.Paused, paused.State);

        // Replacing an occupant frees its voice within the same transaction.
        TimbrePlayback replacement = rig.Scope.Play(clip, handle: slot);
        Assert.Equal(TimbrePlaybackState.Canceled, paused.State);
        Assert.Same(replacement, slot.Current);

        playing.Cancel();
        await TimbreRig.CompletionAsync(playing);
        Assert.Equal(TimbrePlaybackState.Pending, rig.Scope.Play(clip).State);
    }

    [Fact]
    public async Task ScopeDisposalCancelsOnlyItsPlaybacksAndKeepsTheSharedOutput()
    {
        using TimbreRig rig = new();
        TimbreClip clip = TimbreRig.Clip(new DeterministicTimbreSourceFactory(48000));
        TimbreScope other = rig.Runtime.CreateScope();
        TimbreHandle otherSlot = other.CreateHandle();
        TimbrePlayback mine = rig.Scope.Play(clip);
        TimbrePlayback theirs = other.Play(clip, handle: otherSlot);
        await rig.StartAsync(mine, theirs);

        other.Dispose();
        other.Dispose();
        float[] block = await rig.NextBlockAsync();

        Assert.True(other.IsDisposed);
        Assert.Equal(TimbrePlaybackState.Canceled, theirs.State);
        Assert.Null(otherSlot.Current);
        Assert.Equal(TimbrePlaybackState.Playing, mine.State);
        TimbreRig.AssertPcm(TimbreRig.Expected(TimbreRig.Block, Signal, 1f, TimbreRig.Budget), block);
        Assert.True(rig.Output.IsOpen);
        Assert.Equal(0, rig.Output.CloseCount);
        Assert.Throws<ObjectDisposedException>(() => other.Play(clip));
        Assert.Throws<ObjectDisposedException>(() => other.CreateHandle());
        Assert.Same(rig.Runtime, other.Runtime);
    }

    [Fact]
    public async Task RuntimeDisposalCancelsEveryScopeAndClosesTheOutput()
    {
        TimbreRig rig = new();
        TimbreClip clip = TimbreRig.Clip(new DeterministicTimbreSourceFactory(48000));
        TimbreScope other = rig.Runtime.CreateScope();
        TimbrePlayback first = rig.Scope.Play(clip);
        TimbrePlayback second = other.Play(clip);
        await rig.StartAsync(first, second);

        rig.Dispose();

        Assert.Equal(TimbrePlaybackState.Canceled, first.State);
        Assert.Equal(TimbrePlaybackState.Canceled, second.State);
        Assert.True(rig.Scope.IsDisposed);
        Assert.True(other.IsDisposed);
        Assert.Equal(1, rig.Output.CloseCount);
        Assert.False(rig.Output.IsOpen);
        Assert.Throws<ObjectDisposedException>(() => rig.Scope.Play(clip));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => rig.Runtime.PrepareAsync(clip));
    }
}
