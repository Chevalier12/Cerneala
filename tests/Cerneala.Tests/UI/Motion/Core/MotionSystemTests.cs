using System.Reflection;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Motion.Core;

namespace Cerneala.Tests.UI.Motion.Core;

public sealed class MotionSystemTests
{
    [Fact]
    public void MotionSystemUsesManualClockDeterministically()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(motionClock: clock);
        root.Motion.Graph.ActivateShellNode(nodesSampled: 1);

        MotionFrameResult first = root.Motion.Tick();
        clock.Advance(TimeSpan.FromMilliseconds(16));
        MotionFrameResult second = root.Motion.Tick();

        Assert.Equal(TimeSpan.Zero, first.Frame.Delta);
        Assert.Equal(TimeSpan.FromMilliseconds(16), second.Frame.Delta);
        Assert.Equal(1, first.Frame.FrameIndex);
        Assert.Equal(2, second.Frame.FrameIndex);
    }

    [Fact]
    public void MotionSystemDoesNotTickWhenNoActiveMotion()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(motionClock: clock);

        MotionFrameResult result = root.Motion.Tick();

        Assert.False(result.HasWork);
        Assert.False(result.NeedsAnotherFrame);
        Assert.Equal(0, result.MotionFrames);
        Assert.Equal(0, result.Frame.FrameIndex);
    }

    [Fact]
    public void MotionSystemRequestsAnotherFrameWhileGraphActive()
    {
        UIRoot root = new();
        root.Motion.Graph.ActivateShellNode(nodesSampled: 2);

        MotionFrameResult result = root.Motion.Tick();

        Assert.True(result.NeedsAnotherFrame);
        Assert.True(root.Motion.HasActiveMotion);
        Assert.Equal(2, result.MotionNodesSampled);
    }

    [Fact]
    public void MotionSystemClampsHugeDelta()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(motionClock: clock);
        root.Motion.Graph.ActivateShellNode(nodesSampled: 1);
        root.Motion.Tick();

        clock.Advance(TimeSpan.FromSeconds(5));
        MotionFrameResult result = root.Motion.Tick();

        Assert.Equal(TimeSpan.FromMilliseconds(100), result.Frame.Delta);
    }

    [Fact]
    public void MotionSystemRestartsDeltaAtZeroAfterMotionBecomesIdle()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(motionClock: clock);
        root.Motion.Graph.ActivateShellNode(nodesSampled: 1, completed: 1);
        root.Motion.Tick();

        clock.Advance(TimeSpan.FromSeconds(5));
        root.Motion.Graph.ActivateShellNode(nodesSampled: 1);
        MotionFrameResult result = root.Motion.Tick();

        Assert.Equal(TimeSpan.Zero, result.Frame.Delta);
    }

    [Fact]
    public void MotionGraphPublicApiDoesNotExposeTestHooks()
    {
        MethodInfo[] publicMethods = typeof(MotionGraph).GetMethods(BindingFlags.Instance | BindingFlags.Public);

        Assert.DoesNotContain(publicMethods, method => method.Name.Contains("Test", StringComparison.Ordinal));
    }

    [Fact]
    public void MotionSystemRejectsGraphMutationFromWrongThread()
    {
        UIRoot root = new();
        Exception? thrown = null;
        Thread thread = new(() =>
        {
            try
            {
                root.Motion.Graph.ActivateShellNode(nodesSampled: 1);
            }
            catch (Exception ex)
            {
                thrown = ex;
            }
        });

        thread.Start();
        thread.Join();

        Assert.IsType<InvalidOperationException>(thrown);
    }

    [Fact]
    public void MotionFrameCoordinatorRunsBeforeAndAfterLayoutPhasesInOrder()
    {
        UIRoot root = new();
        root.Motion.Graph.ActivateShellNode(nodesSampled: 1);

        root.Motion.Frames.BeginFrame(MotionFrameReason.Manual);
        root.Motion.Frames.BeforeLayout();
        root.Motion.Frames.AfterLayout();
        root.Motion.Frames.BeforeRender();
        root.Motion.Frames.EndFrame();

        Assert.Equal(
            [
                MotionFramePhase.PreInput,
                MotionFramePhase.BeforeLayout,
                MotionFramePhase.AfterLayout,
                MotionFramePhase.BeforeRender,
                MotionFramePhase.AfterRender
            ],
            root.Motion.Diagnostics.Phases);
    }

    [Fact]
    public void UiFrameSchedulerInvokesMotionCoordinatorAroundLayoutAndRender()
    {
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);
        root.ProcessFrame();
        child.Invalidate(InvalidationFlags.Measure | InvalidationFlags.Render, "layout and render");
        bool beforeLayoutObservedDuringMeasure = false;
        bool afterLayoutNotObservedDuringMeasure = false;
        bool beforeRenderObservedDuringRender = false;

        root.ProcessFrame(new FramePhaseProcessors
        {
            Measure = _ =>
            {
                beforeLayoutObservedDuringMeasure = root.Motion.Diagnostics.Phases.Contains(MotionFramePhase.BeforeLayout);
                afterLayoutNotObservedDuringMeasure = !root.Motion.Diagnostics.Phases.Contains(MotionFramePhase.AfterLayout);
            },
            RenderCache = _ => beforeRenderObservedDuringRender = root.Motion.Diagnostics.Phases.Contains(MotionFramePhase.BeforeRender)
        });

        Assert.True(beforeLayoutObservedDuringMeasure);
        Assert.True(afterLayoutNotObservedDuringMeasure);
        Assert.True(beforeRenderObservedDuringRender);
        Assert.Equal(
            [
                MotionFramePhase.PreInput,
                MotionFramePhase.BeforeLayout,
                MotionFramePhase.AfterLayout,
                MotionFramePhase.BeforeRender,
                MotionFramePhase.AfterRender
            ],
            root.Motion.Diagnostics.Phases);
    }

    [Fact]
    public void UIRootDoesNotProcessMotionWhenSchedulerAndMotionAreIdle()
    {
        UIRoot root = new();

        FrameStats stats = root.ProcessFrame();

        Assert.Equal(1, stats.NoWorkFrames);
        Assert.Equal(0, stats.MotionFrames);
    }

    [Fact]
    public void LayoutMotionSnapshotsAreCapturedBeforeAndAfterLayout()
    {
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);
        root.ProcessFrame();
        child.Invalidate(InvalidationFlags.Measure, "layout");

        root.ProcessFrame();

        Assert.Equal(1, root.Motion.Diagnostics.BeforeLayoutSnapshotCaptures);
        Assert.Equal(1, root.Motion.Diagnostics.AfterLayoutSnapshotCaptures);
    }

    [Fact]
    public void UIRootFrameStatsIncludeMotionCounters()
    {
        UIRoot root = new();
        root.Motion.Graph.ActivateShellNode(
            nodesSampled: 3,
            valuesChanged: 2,
            propertyWrites: 1,
            renderInvalidations: 1,
            layoutInvalidations: 1,
            completed: 1,
            skippedByReducedMotion: 1);

        FrameStats stats = root.ProcessFrame();

        Assert.Equal(1, stats.MotionFrames);
        Assert.Equal(3, stats.MotionNodesSampled);
        Assert.Equal(2, stats.MotionValuesChanged);
        Assert.Equal(1, stats.MotionPropertyWrites);
        Assert.Equal(1, stats.MotionCompleted);
        Assert.Equal(1, stats.MotionRenderInvalidations);
        Assert.Equal(1, stats.MotionLayoutInvalidations);
        Assert.Equal(1, stats.MotionSkippedByReducedMotion);
        Assert.True(stats.HasWork);
    }

    [Fact]
    public void UiHostProcessesActiveMotionWithoutSchedulerWork()
    {
        UIRoot root = new();
        UiHost host = new(new UiHostOptions { Root = root });
        host.Update(EmptyInputFrame(), new UiViewport(100, 100), TimeSpan.Zero);
        root.Motion.Graph.ActivateShellNode(nodesSampled: 1);

        UiFrame frame = host.Update(EmptyInputFrame(), new UiViewport(100, 100), TimeSpan.Zero);

        Assert.Equal(1, frame.Stats.MotionFrames);
        Assert.Equal(0, frame.Stats.NoWorkFrames);
    }

    private static InputFrame EmptyInputFrame()
    {
        return new InputFrame(PointerSnapshot.Empty, PointerSnapshot.Empty, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
    }
}
