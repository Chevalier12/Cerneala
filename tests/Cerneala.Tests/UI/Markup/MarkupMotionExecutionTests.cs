using System.Reflection;
using System.Runtime.CompilerServices;
using Cerneala.UI.Elements;
using Cerneala.UI.Markup;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Specs;

namespace Cerneala.Tests.UI.Markup;

public sealed class MarkupMotionExecutionTests
{
    [Fact]
    public async Task ParallelWaitsForEveryLeafAndCompletesExactlyOnce()
    {
        MotionHandle first = FakeHandle();
        MotionHandle second = FakeHandle();
        MarkupMotionExecution execution = MarkupMotionExecution.Parallel(
            () => MarkupMotionExecution.From(first),
            () => MarkupMotionExecution.From(second));
        int completions = 0;
        execution.Completed += (_, _) => completions++;

        first.FinishCompleted(fireEvent: true);
        Assert.False(execution.IsCompleted);
        second.FinishCompleted(fireEvent: true);

        await execution.Completion;
        Assert.True(execution.IsCompleted);
        Assert.Equal(1, completions);
    }

    [Fact]
    public void SequenceStartsNextChildOnlyAfterNaturalCompletion()
    {
        MotionHandle first = FakeHandle();
        MotionHandle second = FakeHandle();
        int started = 0;
        MarkupMotionExecution execution = MarkupMotionExecution.Sequence(
            () => { started++; return MarkupMotionExecution.From(first); },
            () => { started++; return MarkupMotionExecution.From(second); });

        Assert.Equal(1, started);
        first.FinishCompleted(fireEvent: true);
        Assert.Equal(2, started);
        second.FinishCompleted(fireEvent: true);
        Assert.True(execution.IsCompleted);
    }

    [Fact]
    public void CancelingSequenceIsIdempotentAndDoesNotStartFutureChildren()
    {
        MotionHandle first = FakeHandle();
        int started = 0;
        MarkupMotionExecution execution = MarkupMotionExecution.Sequence(
            () => { started++; return MarkupMotionExecution.From(first); },
            () => { started++; return MarkupMotionExecution.From(FakeHandle()); });

        execution.Cancel();
        execution.Cancel();

        Assert.True(first.IsCanceled);
        Assert.True(execution.IsCanceled);
        Assert.Equal(1, started);
    }

    [Fact]
    public void NestedGroupsWorkInBothDirections()
    {
        MotionHandle first = FakeHandle();
        MotionHandle second = FakeHandle();
        MotionHandle third = FakeHandle();
        MarkupMotionExecution parallelThenSequence = MarkupMotionExecution.Parallel(
            () => MarkupMotionExecution.From(first),
            () => MarkupMotionExecution.Sequence(
                () => MarkupMotionExecution.From(second),
                () => MarkupMotionExecution.From(third)));

        first.FinishCompleted(fireEvent: true);
        second.FinishCompleted(fireEvent: true);
        third.FinishCompleted(fireEvent: true);
        Assert.True(parallelThenSequence.IsCompleted);

        MotionHandle fourth = FakeHandle();
        MotionHandle fifth = FakeHandle();
        MarkupMotionExecution sequenceThenParallel = MarkupMotionExecution.Sequence(
            () => MarkupMotionExecution.Parallel(
                () => MarkupMotionExecution.From(fourth),
                () => MarkupMotionExecution.From(fifth)));

        fourth.FinishCompleted(fireEvent: true);
        fifth.FinishCompleted(fireEvent: true);
        Assert.True(sequenceThenParallel.IsCompleted);
    }

    [Fact]
    public void EmptyAndSingleChildGroupsHaveDeterministicTerminalState()
    {
        MarkupMotionExecution emptyParallel = MarkupMotionExecution.Parallel();
        MarkupMotionExecution emptySequence = MarkupMotionExecution.Sequence();
        MotionHandle leaf = FakeHandle();
        MarkupMotionExecution single = MarkupMotionExecution.Sequence(
            () => MarkupMotionExecution.From(leaf));

        Assert.True(emptyParallel.IsCompleted);
        Assert.True(emptySequence.IsCompleted);
        Assert.False(single.IsCompleted);
        leaf.FinishCompleted(fireEvent: true);
        Assert.True(single.IsCompleted);
    }

    [Fact]
    public void DetachCancelsNestedChildrenAndPreventsFutureSequenceSteps()
    {
        UIRoot root = new();
        UIElement owner = new();
        using IDisposable session = GeneratedMarkup.AttachMotionSession(owner);
        ElementLifecycle.AttachSubtree(root, owner);
        MotionHandle active = FakeHandle();
        int futureStarts = 0;
        MarkupMotionExecution execution = GeneratedMarkup.StartMotionExecution(
            session,
            () => MarkupMotionExecution.Sequence(
                () => MarkupMotionExecution.Parallel(
                    () => MarkupMotionExecution.From(active)),
                () => { futureStarts++; return MarkupMotionExecution.From(FakeHandle()); }));

        ElementLifecycle.DetachSubtree(root, owner);

        Assert.True(active.IsCanceled);
        Assert.True(execution.IsCanceled);
        Assert.Equal(0, futureStarts);
    }

    [Fact]
    public void ReusableRecipeCreatesIndependentExecutionsAndLeafHandles()
    {
        List<MotionHandle> handles = [];
        Func<MarkupMotionExecution> recipe = () =>
        {
            MotionHandle handle = FakeHandle();
            handles.Add(handle);
            return MarkupMotionExecution.Sequence(() => MarkupMotionExecution.From(handle));
        };

        MarkupMotionExecution first = recipe();
        MarkupMotionExecution second = recipe();
        first.Cancel();

        Assert.True(handles[0].IsCanceled);
        Assert.False(handles[1].IsCanceled);
        Assert.True(first.IsCanceled);
        Assert.False(second.IsCanceled);

        handles[1].FinishCompleted(fireEvent: true);
        Assert.True(second.IsCompleted);
    }

    [Fact]
    public void StartingIntoTheSameSessionHandleCancelsAndReplacesThePreviousExecution()
    {
        UIRoot root = new();
        UIElement owner = new();
        using IDisposable session = GeneratedMarkup.AttachMotionSession(owner);
        ElementLifecycle.AttachSubtree(root, owner);
        MotionHandle firstHandle = FakeHandle();
        MotionHandle secondHandle = FakeHandle();

        MarkupMotionExecution first = GeneratedMarkup.StartMotionExecution(
            session,
            "Entrance",
            () => MarkupMotionExecution.From(firstHandle));
        MarkupMotionExecution second = GeneratedMarkup.StartMotionExecution(
            session,
            "Entrance",
            () => MarkupMotionExecution.From(secondHandle));

        Assert.True(first.IsCanceled);
        Assert.True(firstHandle.IsCanceled);
        Assert.False(second.IsCanceled);
        GeneratedMarkup.CancelMotionExecution(session, "Entrance");
        GeneratedMarkup.CancelMotionExecution(session, "Entrance");
        Assert.True(second.IsCanceled);
        Assert.True(secondHandle.IsCanceled);
    }

    [Fact]
    public void RepeatedHandleRestartAndCancelLeavesMotionGraphEmptyAndExecutionsCollectible()
    {
        (MotionGraph graph, WeakReference[] executions) = CreateAndReleaseHandledExecutions();

        Assert.Equal(0, graph.ActiveNodeCount);
        for (int attempt = 0; attempt < 3 && executions.Any(reference => reference.IsAlive); attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        Assert.DoesNotContain(executions, reference => reference.IsAlive);
    }

    [Fact]
    public void ExplicitAdapterDoesNotPretendLeafAndGroupRuntimeApisAreInterchangeable()
    {
        MethodInfo? publicGroupComplete = typeof(MotionGroupHandle).GetMethod(
            "Complete",
            BindingFlags.Instance | BindingFlags.Public);
        MethodInfo groupCancel = Assert.Single(typeof(MotionGroupHandle).GetMethods().Where(
            method => method.Name == nameof(MotionGroupHandle.Cancel) && method.IsPublic));

        Assert.Null(publicGroupComplete);
        Assert.Empty(groupCancel.GetParameters());
        Assert.Equal(
            typeof(MotionHandle[]),
            typeof(MotionGroup).GetMethod(nameof(MotionGroup.Parallel))!.GetParameters()[0].ParameterType);
        Assert.Equal(
            typeof(Func<MotionHandle>[]),
            typeof(MotionSequence).GetMethod(nameof(MotionSequence.Start))!.GetParameters()[0].ParameterType);

        Assert.IsType<MarkupMotionExecution>(MarkupMotionExecution.From(FakeHandle()));
        MotionGroupHandle group = MotionGroup.Parallel(FakeHandle());
        Assert.IsType<MarkupMotionExecution>(MarkupMotionExecution.From(group));
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (MotionGraph Graph, WeakReference[] Executions) CreateAndReleaseHandledExecutions()
    {
        UIRoot root = new();
        UIElement owner = new();
        using IDisposable session = GeneratedMarkup.AttachMotionSession(owner);
        ElementLifecycle.AttachSubtree(root, owner);
        MotionGraph graph = new();
        MotionValue<float> value = graph.CreateValue(0f);
        TweenSpec<float> spec = new(TimeSpan.FromSeconds(10));
        List<WeakReference> executions = [];

        for (int index = 0; index < 250; index++)
        {
            MarkupMotionExecution execution = GeneratedMarkup.StartMotionExecution(
                session,
                "Stress",
                () => MarkupMotionExecution.From(value.AnimateTo(index + 1, spec)));
            executions.Add(new WeakReference(execution));
            if ((index & 1) == 0)
            {
                GeneratedMarkup.CancelMotionExecution(session, "Stress");
            }
        }

        GeneratedMarkup.CancelMotionExecution(session, "Stress");
        ElementLifecycle.DetachSubtree(root, owner);
        return (graph, executions.ToArray());
    }
}
