using Cerneala.UI.Invalidation;

namespace Cerneala.Tests.UI.Invalidation;

public sealed class DirtyStateTests
{
    [Fact]
    public void MarkingDirtyRecordsFlagsAndVersion()
    {
        DirtyState state = new();

        bool changed = state.Mark(InvalidationFlags.Render);

        Assert.True(changed);
        Assert.True(state.Has(InvalidationFlags.Render));
        Assert.Equal(1, state.Version);
    }

    [Fact]
    public void RepeatingSameDirtyRequestIsIdempotent()
    {
        DirtyState state = new();
        state.Mark(InvalidationFlags.Render);

        bool changed = state.Mark(InvalidationFlags.Render);

        Assert.False(changed);
        Assert.Equal(1, state.Version);
    }

    [Fact]
    public void ClearingProcessedFlagsKeepsUnrelatedFlags()
    {
        DirtyState state = new();
        state.Mark(InvalidationFlags.Measure | InvalidationFlags.Render | InvalidationFlags.HitTest);

        bool changed = state.Clear(InvalidationFlags.Render);

        Assert.True(changed);
        Assert.True(state.Has(InvalidationFlags.Measure));
        Assert.False(state.Has(InvalidationFlags.Render));
        Assert.True(state.Has(InvalidationFlags.HitTest));
    }

    [Fact]
    public void HasRequiresAllRequestedFlags()
    {
        DirtyState state = new();
        state.Mark(InvalidationFlags.Render);

        Assert.False(state.Has(InvalidationFlags.Measure | InvalidationFlags.Render));
    }
}
