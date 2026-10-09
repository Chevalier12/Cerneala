using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Specs;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.Tests.UI.Motion.Core;

public sealed class MotionHandleThreadAffinityTests
{
    [Theory]
    [InlineData("KeepCurrent", false)]
    [InlineData("Revert", false)]
    [InlineData("CancelComplete", false)]
    [InlineData("Complete", false)]
    [InlineData("Dispose", false)]
    [InlineData("KeepCurrent", true)]
    [InlineData("Revert", true)]
    [InlineData("CancelComplete", true)]
    [InlineData("Complete", true)]
    [InlineData("Dispose", true)]
    public void WrongThreadLifecycleCallPreservesActiveMotion(string operation, bool rootOwned)
    {
        MotionGraph graph = rootOwned ? new UIRoot().Motion.Graph : new MotionGraph();
        MotionValue<double> value = graph.CreateValue(0d);
        List<MotionValueChanged<double>> changes = [];
        using IDisposable subscription = value.Subscribe(changes.Add);
        MotionHandle handle = value.AnimateTo(
            10d,
            MotionFactory.Tween<double>(TimeSpan.FromMilliseconds(100), Easings.Linear));
        Task completion = handle.Completion.AsTask();
        int completedEvents = 0;
        handle.Completed += (_, _) => completedEvents++;
        Tick(graph, 40, 1);
        var velocity = value.Velocity;
        Assert.Equal(4d, value.Current, 5);
        Assert.True(value.IsAnimating);
        Assert.Single(changes);

        Exception? thrown = null;
        Thread worker = new(() => thrown = Record.Exception(() => Invoke(handle, operation)))
        {
            IsBackground = true
        };
        worker.Start();
        Assert.True(worker.Join(TimeSpan.FromSeconds(10)), "Lifecycle call did not return.");

        Assert.IsType<InvalidOperationException>(thrown);
        Assert.Equal(4d, value.Current, 5);
        Assert.Equal(10d, value.Target);
        Assert.Equal(velocity, value.Velocity);
        Assert.True(value.IsAnimating);
        Assert.True(handle.IsActive);
        Assert.False(handle.IsCanceled);
        Assert.False(handle.IsCompleted);
        Assert.False(completion.IsCompleted);
        Assert.Equal(0, completedEvents);
        Assert.Single(changes);
        Assert.Equal(1, graph.ActiveNodeCount);

        MotionFrameResult continued = Tick(graph, 20, 2);
        Assert.Equal(1, continued.MotionNodesSampled);
        Assert.Equal(6d, value.Current, 5);
        Assert.Equal(2, changes.Count);
        Assert.True(handle.IsActive);

        Invoke(handle, operation);

        double expectedCurrent = operation switch
        {
            "Revert" => 0d,
            "CancelComplete" or "Complete" => 10d,
            _ => 6d
        };
        Assert.Equal(expectedCurrent, value.Current, 5);
        Assert.Equal(expectedCurrent, value.Target, 5);
        Assert.False(value.IsAnimating);
        Assert.False(handle.IsActive);
        Assert.Equal(operation == "Complete", handle.IsCompleted);
        Assert.Equal(operation != "Complete", handle.IsCanceled);
        Assert.Equal(operation == "Dispose" ? 0 : 1, completedEvents);
        Assert.True(completion.IsCompleted);
        Assert.Equal(operation != "Complete", completion.IsCanceled);
        Assert.Equal(0, graph.ActiveNodeCount);
        Assert.False(Tick(graph, 100, 3).HasWork);
    }

    [Theory]
    [InlineData("KeepCurrent")]
    [InlineData("Revert")]
    [InlineData("CancelComplete")]
    [InlineData("Complete")]
    [InlineData("Dispose")]
    public void WrongThreadLifecycleCallPreservesSampledVelocity(string operation)
    {
        MotionGraph graph = new();
        MotionValue<double> value = graph.CreateValue(0d);
        MotionHandle handle = value.AnimateTo(10d, MotionFactory.Spring<double>());
        Tick(graph, 16, 1);
        MotionVelocity<double>? velocity = value.Velocity;
        Assert.NotNull(velocity);
        Assert.NotEqual(0d, velocity.Value.Value);

        Exception? thrown = null;
        Thread worker = new(() => thrown = Record.Exception(() => Invoke(handle, operation)))
        {
            IsBackground = true
        };
        worker.Start();
        Assert.True(worker.Join(TimeSpan.FromSeconds(10)), "Lifecycle call did not return.");

        Assert.IsType<InvalidOperationException>(thrown);
        Assert.Equal(velocity, value.Velocity);
        handle.Complete();
        Assert.Equal(0, graph.ActiveNodeCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TerminalHandleLifecycleCallsRemainNoOpsFromAnotherThread(bool complete)
    {
        MotionGraph graph = new();
        MotionValue<double> value = graph.CreateValue(0d);
        MotionHandle terminal = value.AnimateTo(10d, MotionFactory.Tween<double>(TimeSpan.FromMilliseconds(100)));
        if (complete)
        {
            terminal.Complete();
        }
        else
        {
            terminal.Cancel();
        }

        MotionHandle replacement = value.AnimateTo(20d, MotionFactory.Tween<double>(TimeSpan.FromMilliseconds(100)));
        Exception? thrown = null;
        Thread worker = new(() => thrown = Record.Exception(() =>
        {
            terminal.Cancel();
            terminal.Complete();
            terminal.Dispose();
            terminal.Dispose();
        }))
        {
            IsBackground = true
        };
        worker.Start();
        Assert.True(worker.Join(TimeSpan.FromSeconds(10)), "Lifecycle calls did not return.");

        Assert.Null(thrown);
        Assert.True(replacement.IsActive);
        Assert.True(value.IsAnimating);
        Assert.Equal(20d, value.Target);
        Assert.Equal(1, graph.ActiveNodeCount);
        replacement.Dispose();
    }

    private static MotionFrameResult Tick(MotionGraph graph, int milliseconds, int frameIndex)
    {
        TimeSpan delta = TimeSpan.FromMilliseconds(milliseconds);
        return graph.Tick(new MotionFrame(delta, delta, frameIndex, MotionFrameReason.Manual, MotionFramePhase.BeforeRender));
    }

    private static void Invoke(MotionHandle handle, string operation)
    {
        switch (operation)
        {
            case "KeepCurrent":
                handle.Cancel(MotionCancelBehavior.KeepCurrent);
                break;
            case "Revert":
                handle.Cancel(MotionCancelBehavior.Revert);
                break;
            case "CancelComplete":
                handle.Cancel(MotionCancelBehavior.Complete);
                break;
            case "Complete":
                handle.Complete();
                break;
            case "Dispose":
                handle.Dispose();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(operation));
        }
    }
}
