using System.Runtime.CompilerServices;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Specs;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.Tests.UI.Motion.Core;

public sealed class MotionValueTests
{
    [Fact]
    public void JumpToNotifiesOnceWhenValueChanges()
    {
        UIRootHarness harness = new();
        MotionValue<double> value = harness.Graph.CreateValue(0d);
        List<MotionValueChanged<double>> changes = [];
        using IDisposable subscription = value.Subscribe(changes.Add);

        value.JumpTo(10d);
        value.JumpTo(10d);

        MotionValueChanged<double> change = Assert.Single(changes);
        Assert.Equal(0d, change.OldValue);
        Assert.Equal(10d, change.NewValue);
        Assert.Equal(10d, value.Current);
        Assert.Equal(10d, value.Target);
        Assert.False(value.IsAnimating);
    }

    [Fact]
    public void AnimateToUpdatesOverManualTicks()
    {
        UIRootHarness harness = new();
        MotionValue<double> value = harness.Graph.CreateValue(0d);

        value.AnimateTo(10d, MotionFactory.Tween<double>(TimeSpan.FromMilliseconds(100)));
        MotionFrameResult first = harness.Tick(TimeSpan.Zero);
        MotionFrameResult second = harness.Tick(TimeSpan.FromMilliseconds(50));

        Assert.Equal(0d, first.Frame.Delta.TotalMilliseconds);
        Assert.InRange(value.Current, 0.01d, 9.99d);
        Assert.True(value.IsAnimating);
        Assert.True(second.NeedsAnotherFrame);
        Assert.Equal(1, second.MotionNodesSampled);
        Assert.Equal(1, second.MotionValuesChanged);
    }

    [Fact]
    public void RetargetPreservesActiveMotion()
    {
        UIRootHarness harness = new();
        MotionValue<double> value = harness.Graph.CreateValue(0d);
        value.AnimateTo(10d, MotionFactory.Tween<double>(TimeSpan.FromMilliseconds(100)));
        harness.Tick(TimeSpan.Zero);
        harness.Tick(TimeSpan.FromMilliseconds(40));
        double beforeRetarget = value.Current;

        MotionHandle handle = value.AnimateTo(
            20d,
            MotionFactory.Tween<double>(TimeSpan.FromMilliseconds(100)),
            new MotionStartOptions(RetargetMode.Restart));
        MotionFrameResult retargetFrame = harness.Tick(TimeSpan.FromMilliseconds(16));

        Assert.True(handle.IsActive);
        Assert.True(harness.Graph.HasActiveMotion);
        Assert.InRange(value.Current, beforeRetarget, 20d);
        Assert.Equal(20d, value.Target);
        Assert.Equal(1, retargetFrame.MotionNodesSampled);
    }

    [Fact]
    public void RetargetCanPreserveProgressWhenRequested()
    {
        UIRootHarness harness = new();
        MotionValue<double> value = harness.Graph.CreateValue(0d);
        value.AnimateTo(10d, MotionFactory.Tween<double>(TimeSpan.FromMilliseconds(100), Easings.Linear));
        harness.Tick(TimeSpan.Zero);
        harness.Tick(TimeSpan.FromMilliseconds(80));

        value.AnimateTo(
            20d,
            MotionFactory.Tween<double>(TimeSpan.FromMilliseconds(100), Easings.Linear),
            new MotionStartOptions(RetargetMode.PreserveProgress));
        harness.Tick(TimeSpan.FromMilliseconds(10));

        Assert.InRange(value.Current, 15d, 20d);
    }

    [Fact]
    public void PreserveProgressRetargetUsesNewSpec()
    {
        UIRootHarness harness = new();
        MotionValue<double> value = harness.Graph.CreateValue(0d);
        value.AnimateTo(10d, MotionFactory.Tween<double>(TimeSpan.FromMilliseconds(100), Easings.Linear));
        harness.Tick(TimeSpan.Zero);
        harness.Tick(TimeSpan.FromMilliseconds(80));

        value.AnimateTo(
            20d,
            MotionFactory.Tween<double>(TimeSpan.FromMilliseconds(1000), Easings.Linear),
            new MotionStartOptions(RetargetMode.PreserveProgress));
        harness.Tick(TimeSpan.FromMilliseconds(10));

        Assert.InRange(value.Current, 8.01d, 12d);
    }

    [Fact]
    public void OldRetargetedHandleCannotCancelNewHandle()
    {
        UIRootHarness harness = new();
        MotionValue<double> value = harness.Graph.CreateValue(0d);
        MotionHandle oldHandle = value.AnimateTo(10d, MotionFactory.Tween<double>(TimeSpan.FromMilliseconds(100)));
        harness.Tick(TimeSpan.Zero);
        harness.Tick(TimeSpan.FromMilliseconds(40));

        MotionHandle newHandle = value.AnimateTo(20d, MotionFactory.Tween<double>(TimeSpan.FromMilliseconds(100)));
        oldHandle.Cancel(MotionCancelBehavior.Complete);
        harness.Tick(TimeSpan.FromMilliseconds(20));

        Assert.True(newHandle.IsActive);
        Assert.True(value.IsAnimating);
        Assert.NotEqual(20d, value.Current);
    }

    [Fact]
    public void CancelKeepCurrentStopsFutureTicks()
    {
        UIRootHarness harness = new();
        MotionValue<double> value = harness.Graph.CreateValue(0d);
        MotionHandle handle = value.AnimateTo(10d, MotionFactory.Tween<double>(TimeSpan.FromMilliseconds(100)));
        harness.Tick(TimeSpan.Zero);
        harness.Tick(TimeSpan.FromMilliseconds(40));
        double canceledAt = value.Current;

        handle.Cancel();
        MotionFrameResult result = harness.Tick(TimeSpan.FromMilliseconds(40));

        Assert.True(handle.IsCanceled);
        Assert.False(value.IsAnimating);
        Assert.Equal(canceledAt, value.Current);
        Assert.False(result.HasWork);
        Assert.False(harness.Graph.HasActiveMotion);
    }

    [Fact]
    public void CompleteJumpsToTargetAndFiresCompletionOnce()
    {
        UIRootHarness harness = new();
        MotionValue<double> value = harness.Graph.CreateValue(0d);
        MotionHandle handle = value.AnimateTo(10d, MotionFactory.Tween<double>(TimeSpan.FromSeconds(1)));
        int completed = 0;
        MotionCompletionState? state = null;
        handle.Completed += (_, args) =>
        {
            completed++;
            state = args.State;
        };

        handle.Complete();
        handle.Complete();

        Assert.Equal(10d, value.Current);
        Assert.False(value.IsAnimating);
        Assert.True(handle.IsCompleted);
        Assert.Equal(1, completed);
        Assert.Equal(MotionCompletionState.Completed, state);
    }

    [Fact]
    public void DisposingHandleUnregistersCallbacks()
    {
        UIRootHarness harness = new();
        MotionValue<double> value = harness.Graph.CreateValue(0d);
        MotionHandle handle = value.AnimateTo(10d, MotionFactory.Tween<double>(TimeSpan.FromMilliseconds(100)));
        bool callbackInvoked = false;
        handle.Completed += (_, _) => callbackInvoked = true;

        handle.Dispose();
        harness.Tick(TimeSpan.FromMilliseconds(100));

        Assert.True(handle.IsCanceled);
        Assert.False(callbackInvoked);
        Assert.False(value.IsAnimating);
    }

    [Fact]
    public void DisposingHandleReleasesCompletionCallbackTarget()
    {
        UIRootHarness harness = new();
        MotionValue<double> value = harness.Graph.CreateValue(0d);
        MotionHandle handle = value.AnimateTo(10d, MotionFactory.Tween<double>(TimeSpan.FromMilliseconds(100)));
        WeakReference callbackTarget = AttachCompletionTargetAndDispose(handle);

        ForceCollection();

        Assert.False(callbackTarget.IsAlive);
        GC.KeepAlive(handle);
    }

    [Fact]
    public async Task CompletionResolvesAfterNaturalCompletion()
    {
        UIRootHarness harness = new();
        MotionValue<double> value = harness.Graph.CreateValue(0d);
        MotionHandle handle = value.AnimateTo(10d, MotionFactory.Tween<double>(TimeSpan.FromMilliseconds(100)));

        harness.Tick(TimeSpan.Zero);
        harness.Tick(TimeSpan.FromMilliseconds(100));
        await handle.Completion;

        Assert.True(handle.IsCompleted);
        Assert.Equal(10d, value.Current);
    }

    [Fact]
    public async Task CancelingHandleMarksCompletionAsCanceled()
    {
        UIRootHarness harness = new();
        MotionValue<double> value = harness.Graph.CreateValue(0d);
        MotionHandle handle = value.AnimateTo(10d, MotionFactory.Tween<double>(TimeSpan.FromMilliseconds(100)));

        handle.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await handle.Completion.AsTask());
        Assert.True(handle.IsCanceled);
    }

    [Fact]
    public void DerivedValuesRecomputeWhenDependenciesChange()
    {
        UIRootHarness harness = new();
        MotionValue<double> x = harness.Graph.CreateValue(2d);
        MotionValue<double> y = harness.Graph.CreateValue(3d);
        using DerivedMotionValue<double> sum = MotionValue.Combine(x, y, static (left, right) => left + right);
        List<double> observed = [];
        using IDisposable subscription = sum.Subscribe(change => observed.Add(change.NewValue));

        x.JumpTo(4d);
        y.JumpTo(6d);

        Assert.Equal(10d, sum.Current);
        Assert.Equal([7d, 10d], observed);
    }

    [Fact]
    public void GraphStagesMutationFromCallbacks()
    {
        UIRootHarness harness = new();
        MotionValue<double> first = harness.Graph.CreateValue(0d);
        MotionValue<double> second = harness.Graph.CreateValue(0d);
        MotionHandle? secondHandle = null;
        using IDisposable subscription = first.Subscribe(_ =>
        {
            secondHandle = second.AnimateTo(1d, MotionFactory.Tween<double>(TimeSpan.FromMilliseconds(10)));
        });

        first.AnimateTo(1d, MotionFactory.Tween<double>(TimeSpan.FromMilliseconds(10)));
        MotionFrameResult firstFrame = harness.Tick(TimeSpan.FromMilliseconds(10));
        MotionFrameResult secondFrame = harness.Tick(TimeSpan.FromMilliseconds(10));

        Assert.Equal(1, firstFrame.MotionNodesSampled);
        Assert.Equal(1, secondFrame.MotionNodesSampled);
        Assert.NotNull(secondHandle);
        Assert.True(secondHandle.IsCompleted);
        Assert.Equal(1d, second.Current);
    }

    [Fact]
    public void GraphKeepsNodeWhenCallbackRemovesAndReaddsSameNode()
    {
        UIRootHarness harness = new();
        RemoveAndReaddNode node = new(harness.Graph);
        harness.Graph.Register(node);

        MotionFrameResult first = harness.Tick(TimeSpan.FromMilliseconds(1));
        MotionFrameResult second = harness.Tick(TimeSpan.FromMilliseconds(1));

        Assert.True(first.NeedsAnotherFrame);
        Assert.True(second.HasWork);
        Assert.Equal(2, node.Ticks);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference AttachCompletionTargetAndDispose(MotionHandle handle)
    {
        CompletionCallbackTarget target = new();
        handle.Completed += target.OnCompleted;
        WeakReference reference = new(target);
        handle.Dispose();
        return reference;
    }

    private static void ForceCollection()
    {
        for (int i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }

    private sealed class CompletionCallbackTarget
    {
        public void OnCompleted(object? sender, MotionCompletedEventArgs args)
        {
        }
    }

    private sealed class RemoveAndReaddNode(MotionGraph graph) : MotionNode
    {
        public int Ticks { get; private set; }

        protected internal override MotionNodeTickResult Tick(MotionFrame frame)
        {
            Ticks++;
            if (Ticks == 1)
            {
                graph.Unregister(this);
                graph.Register(this);
            }

            return new MotionNodeTickResult();
        }
    }

    private sealed class UIRootHarness
    {
        private int frameIndex;

        public MotionGraph Graph { get; } = new(new MotionThreadGuard(Environment.CurrentManagedThreadId));

        public MotionFrameResult Tick(TimeSpan delta)
        {
            frameIndex++;
            return Graph.Tick(new MotionFrame(delta, delta, frameIndex, MotionFrameReason.Manual, MotionFramePhase.BeforeRender));
        }
    }
}
