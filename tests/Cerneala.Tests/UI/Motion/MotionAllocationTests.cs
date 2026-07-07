using Cerneala.UI.Elements;
using Cerneala.UI.Motion;
using Cerneala.UI.Motion.Core;
using Cerneala.Tests.UI.Motion.Core;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.Tests.UI.Motion;

public sealed class MotionAllocationTests
{
    private const long IdleTickAllocationBudgetBytes = 0;
    private const long ActiveOpacityTickAllocationBudgetBytes = 8192;

    [Fact]
    public void IdleMotionTickAllocatesNoBytesAfterWarmup()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(100, 100, motionClock: clock);
        root.ProcessFrame();
        _ = root.Motion.Tick();

        long allocatedBytes = MeasureAllocatedBytes(() => _ = root.Motion.Tick());

        Assert.Equal(IdleTickAllocationBudgetBytes, allocatedBytes);
    }

    [Fact]
    public void ActiveOpacityMotionHotTickStaysWithinAllocationBudget()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(100, 100, motionClock: clock);
        UIElement element = new();
        root.VisualChildren.Add(element);
        root.ProcessFrame();

        element.Motion().Opacity.To(0.5f, MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));
        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(16));
        _ = root.Motion.Tick();
        clock.Advance(TimeSpan.FromMilliseconds(16));

        long allocatedBytes = MeasureAllocatedBytes(() => _ = root.Motion.Tick());

        Assert.InRange(allocatedBytes, 0, ActiveOpacityTickAllocationBudgetBytes);
    }

    private static long MeasureAllocatedBytes(Action action)
    {
        ForceCollection();
        long before = GC.GetAllocatedBytesForCurrentThread();
        action();
        return GC.GetAllocatedBytesForCurrentThread() - before;
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
}
