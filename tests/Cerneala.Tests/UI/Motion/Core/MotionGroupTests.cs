using Cerneala.UI.Motion.Core;

namespace Cerneala.Tests.UI.Motion.Core;

public sealed class MotionGroupTests
{
    [Fact]
    public async Task ParallelWaitsForAllChildren()
    {
        MotionHandle first = FakeHandle();
        MotionHandle second = FakeHandle();
        MotionGroupHandle group = MotionGroup.Parallel(first, second);

        first.FinishCompleted(fireEvent: true);
        Assert.False(group.IsCompleted);
        second.FinishCompleted(fireEvent: true);

        await group.Completion;
        Assert.True(group.IsCompleted);
    }

    [Fact]
    public void SequenceStartsNextChildOnlyAfterPreviousCompletion()
    {
        MotionHandle first = FakeHandle();
        MotionHandle second = FakeHandle();
        int started = 0;
        MotionGroupHandle group = MotionSequence.Start(
            () => { started++; return first; },
            () => { started++; return second; });

        Assert.Equal(1, started);
        first.FinishCompleted(fireEvent: true);
        Assert.Equal(2, started);
        second.FinishCompleted(fireEvent: true);
        Assert.True(group.IsCompleted);
    }

    [Fact]
    public void CancelingGroupCancelsActiveChildrenAndPreventsFutureSequenceChildren()
    {
        MotionHandle first = FakeHandle();
        MotionHandle second = FakeHandle();
        int started = 0;
        MotionGroupHandle group = MotionSequence.Start(
            () => { started++; return first; },
            () => { started++; return second; });

        group.Cancel();

        Assert.True(first.IsCanceled);
        Assert.False(second.IsCanceled);
        Assert.Equal(1, started);
        Assert.True(group.IsCanceled);
    }

    [Fact]
    public void StaggerStartsChildrenWithDeterministicOffsets()
    {
        MotionStagger stagger = new(TimeSpan.FromMilliseconds(20));

        Assert.Equal(TimeSpan.Zero, stagger.GetDelay(0));
        Assert.Equal(TimeSpan.FromMilliseconds(20), stagger.GetDelay(1));
        Assert.Equal(TimeSpan.FromMilliseconds(40), stagger.GetDelay(2));
    }

    private static MotionHandle FakeHandle()
    {
        MotionHandle? handle = null;
        handle = new MotionHandle(
            behavior => handle!.FinishCanceled(behavior, fireEvent: true),
            () => handle!.FinishCompleted(fireEvent: true),
            () => handle!.FinishCanceled(MotionCancelBehavior.KeepCurrent, fireEvent: false));
        return handle;
    }
}
