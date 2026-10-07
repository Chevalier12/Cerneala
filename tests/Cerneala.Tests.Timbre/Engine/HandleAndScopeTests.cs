using Cerneala.Tests.Timbre.Harness;
using Cerneala.Timbre;

namespace Cerneala.Tests.Timbre.Engine;

public sealed class HandleAndScopeTests
{
    private static readonly Func<long, int, float> Signal = DeterministicSoundReader.DefaultSignal;

    [Fact]
    public async Task PlayWithoutHandleOverlapsAndCancelTargetsOneIdentity()
    {
        using TimbreRig rig = new();
        SoundClip clip = TimbreRig.Clip(new DeterministicSoundSourceFactory(48000));
        SoundPlayback first = rig.Scope.Play(clip);
        SoundPlayback second = rig.Scope.Play(clip);
        await rig.StartAsync(first, second);

        first.Cancel();
        float[] block = await rig.NextBlockAsync();

        Assert.Equal(SoundPlaybackState.Canceled, first.State);
        Assert.Equal(SoundPlaybackState.Playing, second.State);
        TimbreRig.AssertPcm(TimbreRig.Expected(TimbreRig.Block, Signal, 1f, TimbreRig.Budget), block);
        Assert.Equal(SoundPlaybackState.Canceled, (await TimbreRig.CompletionAsync(first)).State);
    }

    [Fact]
    public async Task PlayWithSameHandleReplacesOnlyTheOccupant()
    {
        using TimbreRig rig = new();
        SoundClip clip = TimbreRig.Clip(new DeterministicSoundSourceFactory(48000));
        SoundHandle slot = rig.Scope.CreateHandle();
        SoundPlayback old = rig.Scope.Play(clip, handle: slot);
        SoundPlayback free = rig.Scope.Play(clip);
        Assert.Same(old, slot.Current);
        Assert.Same(rig.Scope, slot.Scope);

        SoundPlayback current = rig.Scope.Play(clip, start => start.Volume = 0.5f, handle: slot);

        Assert.Equal(SoundPlaybackState.Canceled, old.State);
        Assert.Same(current, slot.Current);
        float[] pcm = await rig.StartAsync(free, current);
        TimbreRig.AssertPcm(TimbreRig.Expected(TimbreRig.Budget, Signal, 1.5f), pcm);
        Assert.Equal(SoundPlaybackState.Playing, free.State);
    }

    [Fact]
    public async Task CancelOnStaleReferenceDoesNotCancelTheNewOccupant()
    {
        using TimbreRig rig = new();
        SoundClip clip = TimbreRig.Clip(new DeterministicSoundSourceFactory(48000));
        SoundHandle slot = rig.Scope.CreateHandle();
        SoundPlayback old = rig.Scope.Play(clip, handle: slot);
        SoundPlayback current = rig.Scope.Play(clip, handle: slot);

        old.Cancel();
        old.Cancel();

        Assert.Equal(SoundPlaybackState.Pending, current.State);
        Assert.Same(current, slot.Current);
        await rig.StartAsync(current);
        Assert.Equal(SoundPlaybackState.Playing, current.State);

        slot.Cancel();
        Assert.Equal(SoundPlaybackState.Canceled, current.State);
        Assert.Null(slot.Current);
        slot.Cancel(); // empty slot: no-op
        Assert.Null(slot.Current);
    }

    [Fact]
    public void EmptySlotCancelIsNoOp()
    {
        using TimbreRig rig = new();
        SoundHandle slot = rig.Scope.CreateHandle();
        slot.Cancel();
        Assert.Null(slot.Current);
        Assert.Equal(0, rig.Runtime.GetDiagnostics().PlaybacksStarted);
    }

    [Fact]
    public void SynchronousStartFailuresKeepTheOccupantAndCreateNoIdentity()
    {
        using TimbreRig rig = new();
        SoundParameter<float> declared = new("Declared", 1f);
        SoundParameter<float> foreign = new("Foreign", 1f);
        SoundClip clip = new(SoundSource.FromReader(new DeterministicSoundSourceFactory(48000).Open), parameters: [declared]);
        SoundHandle slot = rig.Scope.CreateHandle();
        SoundPlayback occupant = rig.Scope.Play(clip, handle: slot);
        using SoundScope otherScope = rig.Runtime.CreateScope();
        SoundHandle foreignSlot = otherScope.CreateHandle();
        InvalidOperationException thrown = new("configure failed");

        Assert.Same(thrown, Assert.Throws<InvalidOperationException>(() => rig.Scope.Play(clip, _ => throw thrown, slot)));
        Assert.Throws<ArgumentException>(() => rig.Scope.Play(clip, start => start.Set(foreign, 1f), slot));
        Assert.Throws<ArgumentOutOfRangeException>(() => rig.Scope.Play(clip, start => start.Set(declared, float.NaN), slot));
        Assert.Throws<ArgumentOutOfRangeException>(() => rig.Scope.Play(clip, start => start.Volume = 2f, slot));
        Assert.Throws<ArgumentOutOfRangeException>(() => rig.Scope.Play(clip, start => start.Volume = float.PositiveInfinity, slot));
        Assert.Throws<ArgumentException>(() => rig.Scope.Play(clip, handle: foreignSlot));
        Assert.Throws<ArgumentNullException>(() => rig.Scope.Play(null!, handle: slot));

        Assert.Same(occupant, slot.Current);
        Assert.Equal(SoundPlaybackState.Pending, occupant.State);
        Assert.Equal(1, rig.Runtime.GetDiagnostics().PlaybacksStarted);
    }

    [Fact]
    public void ModifiedClipStartsAndReplacesThroughTheSamePathAsAPlainClip()
    {
        using TimbreRig rig = new();
        SoundClip plain = TimbreRig.Clip(new DeterministicSoundSourceFactory(48000));
        SoundClip filtered = new(plain.Source, modifiers: [new LowPass()]);
        SoundHandle slot = rig.Scope.CreateHandle();
        SoundPlayback occupant = rig.Scope.Play(plain, handle: slot);

        SoundPlayback replacement = rig.Scope.Play(filtered, handle: slot);

        Assert.Equal(SoundPlaybackState.Canceled, occupant.State);
        Assert.Same(replacement, slot.Current);
        Assert.Equal(SoundPlaybackState.Pending, replacement.State);
    }

    [Fact]
    public async Task VoiceLimitCountsPausedPlaybacksAndRejectsWithoutStealing()
    {
        using TimbreRig rig = new(options => options.MaxVoices = 2);
        SoundClip clip = TimbreRig.Clip(new DeterministicSoundSourceFactory(48000));
        SoundHandle slot = rig.Scope.CreateHandle();
        SoundPlayback playing = rig.Scope.Play(clip);
        SoundPlayback paused = rig.Scope.Play(clip, handle: slot);
        paused.Pause();

        SoundException rejected = Assert.Throws<SoundException>(() => rig.Scope.Play(clip));
        Assert.Equal(SoundErrorKind.VoiceLimitExceeded, rejected.Kind);
        Assert.Equal(SoundPlaybackState.Pending, playing.State);
        Assert.Equal(SoundPlaybackState.Paused, paused.State);

        // Replacing an occupant frees its voice within the same transaction.
        SoundPlayback replacement = rig.Scope.Play(clip, handle: slot);
        Assert.Equal(SoundPlaybackState.Canceled, paused.State);
        Assert.Same(replacement, slot.Current);

        playing.Cancel();
        await TimbreRig.CompletionAsync(playing);
        Assert.Equal(SoundPlaybackState.Pending, rig.Scope.Play(clip).State);
    }

    [Fact]
    public async Task ScopeDisposalCancelsOnlyItsPlaybacksAndKeepsTheSharedOutput()
    {
        using TimbreRig rig = new();
        SoundClip clip = TimbreRig.Clip(new DeterministicSoundSourceFactory(48000));
        SoundScope other = rig.Runtime.CreateScope();
        SoundHandle otherSlot = other.CreateHandle();
        SoundPlayback mine = rig.Scope.Play(clip);
        SoundPlayback theirs = other.Play(clip, handle: otherSlot);
        await rig.StartAsync(mine, theirs);

        other.Dispose();
        other.Dispose();
        float[] block = await rig.NextBlockAsync();

        Assert.True(other.IsDisposed);
        Assert.Equal(SoundPlaybackState.Canceled, theirs.State);
        Assert.Null(otherSlot.Current);
        Assert.Equal(SoundPlaybackState.Playing, mine.State);
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
        SoundClip clip = TimbreRig.Clip(new DeterministicSoundSourceFactory(48000));
        SoundScope other = rig.Runtime.CreateScope();
        SoundPlayback first = rig.Scope.Play(clip);
        SoundPlayback second = other.Play(clip);
        await rig.StartAsync(first, second);

        rig.Dispose();

        Assert.Equal(SoundPlaybackState.Canceled, first.State);
        Assert.Equal(SoundPlaybackState.Canceled, second.State);
        Assert.True(rig.Scope.IsDisposed);
        Assert.True(other.IsDisposed);
        Assert.Equal(1, rig.Output.CloseCount);
        Assert.False(rig.Output.IsOpen);
        Assert.Throws<ObjectDisposedException>(() => rig.Scope.Play(clip));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => rig.Runtime.PrepareAsync(clip));
    }
}
