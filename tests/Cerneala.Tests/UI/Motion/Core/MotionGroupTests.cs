using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Specs;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

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
    public void ParallelCountsChildrenThatCompletedBeforeComposition()
    {
        MotionHandle completed = FakeHandle();
        MotionHandle active = FakeHandle();
        completed.FinishCompleted(fireEvent: true);

        MotionGroupHandle group = MotionGroup.Parallel(completed, active);
        active.FinishCompleted(fireEvent: true);

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
    public void SequenceObservesChildrenThatCompleteBeforeSubscription()
    {
        MotionHandle completed = FakeHandle();
        MotionHandle second = FakeHandle();
        completed.FinishCompleted(fireEvent: true);
        int started = 0;

        MotionGroupHandle group = MotionSequence.Start(
            () => { started++; return completed; },
            () => { started++; return second; });

        Assert.Equal(2, started);
        second.FinishCompleted(fireEvent: true);
        Assert.True(group.IsCompleted);
    }

    [Fact]
    public async Task SequenceBecomesCanceledWhenActiveChildIsCanceled()
    {
        MotionHandle child = FakeHandle();
        MotionGroupHandle group = MotionSequence.Start(() => child);

        child.Cancel();

        Assert.True(group.IsCanceled);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await group.Completion.AsTask());
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
    public void ParallelObservesTerminalChildWhenEarlierCompletionSubscriberThrows()
    {
        MotionGraph graph = new();
        MotionValue<float> value = graph.CreateValue(0f);
        MotionHandle child = value.AnimateTo(
            1f,
            MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100), Easings.Linear));
        child.Completed += (_, _) => throw new InvalidOperationException("hostile subscriber");
        MotionGroupHandle group = MotionGroup.Parallel(child);

        Assert.Throws<InvalidOperationException>(() => child.Complete());

        Assert.True(child.IsCompleted);
        Assert.True(group.IsCompleted);
        Assert.True(group.Completion.IsCompletedSuccessfully);
    }

    [Fact]
    public void SequenceObservesTerminalChildWhenEarlierCompletionSubscriberThrows()
    {
        MotionGraph graph = new();
        MotionValue<float> value = graph.CreateValue(0f);
        MotionValue<float> secondValue = graph.CreateValue(0f);
        MotionHandle first = value.AnimateTo(
            1f,
            MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100), Easings.Linear));
        first.Completed += (_, _) => throw new InvalidOperationException("hostile subscriber");
        MotionHandle second = secondValue.AnimateTo(
            2f,
            MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100), Easings.Linear));
        second.Cancel();
        int started = 0;
        MotionGroupHandle group = MotionSequence.Start(
            () => first,
            () => { started++; return second; });

        Assert.Throws<InvalidOperationException>(() => first.Complete());

        Assert.Equal(1, started);
        Assert.True(group.IsCanceled);
        Assert.True(group.Completion.IsCanceled);
    }

    [Fact]
    public void CancelingParallelGroupFinishesCancellationWhenAChildSubscriberThrows()
    {
        MotionGraph graph = new();
        MotionValue<float> firstValue = graph.CreateValue(0f);
        MotionValue<float> secondValue = graph.CreateValue(0f);
        MotionHandle first = firstValue.AnimateTo(
            1f,
            MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100), Easings.Linear));
        MotionHandle second = secondValue.AnimateTo(
            1f,
            MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100), Easings.Linear));
        first.Completed += (_, _) => throw new InvalidOperationException("hostile subscriber");
        MotionGroupHandle group = MotionGroup.Parallel(first, second);

        Assert.Throws<InvalidOperationException>(() => group.Cancel());

        Assert.True(group.IsCanceled);
        Assert.True(first.IsCanceled);
        Assert.True(
            group.Completion.IsCanceled && second.IsCanceled,
            $"completionCanceled={group.Completion.IsCanceled}; secondCanceled={second.IsCanceled}");
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
