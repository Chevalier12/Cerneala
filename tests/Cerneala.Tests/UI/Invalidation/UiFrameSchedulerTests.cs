using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

namespace Cerneala.Tests.UI.Invalidation;

public sealed class UiFrameSchedulerTests
{
    [Fact]
    public void SchedulerNoOpsWhenNothingIsDirty()
    {
        UIRoot root = new();
        int measured = 0;

        FrameStats stats = root.ProcessFrame(new FramePhaseProcessors
        {
            Measure = _ => measured++
        });

        Assert.Equal(0, measured);
        Assert.Equal(1, stats.NoWorkFrames);
    }

    [Fact]
    public void ProcessesPhasesInOrder()
    {
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);
        root.ProcessFrame();
        List<FramePhase> phases = [];
        child.Invalidate(InvalidationFlags.Measure | InvalidationFlags.HitTest, "all");

        root.ProcessFrame(new FramePhaseProcessors
        {
            Measure = _ => phases.Add(FramePhase.Measure),
            Arrange = _ => phases.Add(FramePhase.Arrange),
            RenderCache = _ => phases.Add(FramePhase.RenderCache),
            HitTest = _ => phases.Add(FramePhase.HitTest)
        });

        Assert.Equal(
            [FramePhase.Measure, FramePhase.Measure, FramePhase.Arrange, FramePhase.Arrange, FramePhase.RenderCache, FramePhase.HitTest],
            phases);
    }

    [Fact]
    public void MvpProcessesAllQueuedWorkEvenWithBudget()
    {
        UIRoot root = new();
        UIElement first = new();
        UIElement second = new();
        root.VisualChildren.Add(first);
        root.VisualChildren.Add(second);
        root.ProcessFrame();
        first.Invalidate(InvalidationFlags.Render, "render");
        second.Invalidate(InvalidationFlags.Render, "render");

        FrameStats stats = root.ProcessFrame(FramePhaseProcessors.Empty, new FrameBudget(1));

        Assert.Equal(2, stats.RenderedElements);
        Assert.False(root.Scheduler.HasWork);
    }

    [Fact]
    public void FailedPhaseKeepsDirtyFlagsAndQueuedWork()
    {
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);
        root.ProcessFrame();
        child.Invalidate(InvalidationFlags.Render, "render");

        Assert.Throws<InvalidOperationException>(() => root.ProcessFrame(new FramePhaseProcessors
        {
            RenderCache = _ => throw new InvalidOperationException("boom")
        }));

        Assert.True(child.DirtyState.Has(InvalidationFlags.Render));
        Assert.Equal(1, root.RenderQueue.Count);
    }

    [Fact]
    public void ProcessesAspectBeforeLayoutAndRender()
    {
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);
        root.ProcessFrame();
        List<FramePhase> phases = [];
        child.Invalidate(InvalidationFlags.Aspect | InvalidationFlags.Measure | InvalidationFlags.HitTest, "aspect and layout");

        root.ProcessFrame(new FramePhaseProcessors
        {
            Aspect = _ => phases.Add(FramePhase.Aspect),
            Measure = _ => phases.Add(FramePhase.Measure),
            Arrange = _ => phases.Add(FramePhase.Arrange),
            RenderCache = _ => phases.Add(FramePhase.RenderCache),
            HitTest = _ => phases.Add(FramePhase.HitTest)
        });

        Assert.Equal(
            [FramePhase.Aspect, FramePhase.Measure, FramePhase.Measure, FramePhase.Arrange, FramePhase.Arrange, FramePhase.RenderCache, FramePhase.HitTest],
            phases);
    }

    [Fact]
    public void ProcessesInheritedPropertiesBeforeAspect()
    {
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);
        root.ProcessFrame();
        List<FramePhase> phases = [];
        child.Invalidate(InvalidationFlags.Inherited | InvalidationFlags.Aspect | InvalidationFlags.Measure, "inherit and aspect");

        root.ProcessFrame(new FramePhaseProcessors
        {
            InheritedProperties = _ => phases.Add(FramePhase.InheritedProperties),
            Aspect = _ => phases.Add(FramePhase.Aspect),
            Measure = _ => phases.Add(FramePhase.Measure)
        });

        Assert.Equal(
            [FramePhase.InheritedProperties, FramePhase.Aspect, FramePhase.Measure, FramePhase.Measure],
            phases);
    }

    [Fact]
    public void FailedAspectPhaseKeepsDirtyFlagsAndQueuedWork()
    {
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);
        root.ProcessFrame();
        child.Invalidate(InvalidationFlags.Aspect, "aspect");

        Assert.Throws<InvalidOperationException>(() => root.ProcessFrame(new FramePhaseProcessors
        {
            Aspect = _ => throw new InvalidOperationException("boom")
        }));

        Assert.True(child.DirtyState.Has(InvalidationFlags.Aspect));
        Assert.Equal(1, root.AspectQueue.Count);
    }

    [Fact]
    public void SuccessfulDerivedPhaseClearsOriginalSpecializedDirtyFlags()
    {
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);
        child.Invalidate(InvalidationFlags.InputVisual, "hover");

        root.ProcessFrame();

        Assert.False(child.DirtyState.Has(InvalidationFlags.InputVisual));
        Assert.False(child.DirtyState.Has(InvalidationFlags.Render));
        Assert.False(child.DirtyState.IsDirty);
    }

    [Fact]
    public void SuccessfulResourceMeasureOnlyPhaseClearsOriginalResourceDirtyFlag()
    {
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);
        child.Invalidate(new InvalidationRequest(
            child,
            InvalidationFlags.Resource,
            "resource measure",
            resourceEffects: InvalidationFlags.Measure));

        root.ProcessFrame();

        Assert.False(child.DirtyState.Has(InvalidationFlags.Resource));
        Assert.False(child.DirtyState.Has(InvalidationFlags.Measure));
        Assert.False(child.DirtyState.IsDirty);
    }
}
