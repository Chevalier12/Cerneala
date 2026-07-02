using Cerneala.UI.Diagnostics;

namespace Cerneala.UI.Invalidation;

public sealed class UiFrameScheduler
{
    private readonly LayoutQueue layoutQueue;
    private readonly RenderQueue renderQueue;
    private readonly HitTestQueue hitTestQueue;
    private readonly InvalidationTrace trace;
    private const InvalidationFlags ConcreteWorkFlags =
        InvalidationFlags.Measure |
        InvalidationFlags.Arrange |
        InvalidationFlags.Render |
        InvalidationFlags.HitTest;

    private const InvalidationFlags SpecializedWorkFlags =
        InvalidationFlags.Text |
        InvalidationFlags.Image |
        InvalidationFlags.Resource |
        InvalidationFlags.Style |
        InvalidationFlags.InputVisual |
        InvalidationFlags.Subtree;

    public UiFrameScheduler(
        LayoutQueue layoutQueue,
        RenderQueue renderQueue,
        HitTestQueue hitTestQueue,
        InvalidationTrace? trace = null)
    {
        this.layoutQueue = layoutQueue ?? throw new ArgumentNullException(nameof(layoutQueue));
        this.renderQueue = renderQueue ?? throw new ArgumentNullException(nameof(renderQueue));
        this.hitTestQueue = hitTestQueue ?? throw new ArgumentNullException(nameof(hitTestQueue));
        this.trace = trace ?? InvalidationTrace.Disabled;
    }

    public bool HasWork => layoutQueue.HasWork || renderQueue.HasWork || hitTestQueue.HasWork;

    public FrameStats ProcessFrame(
        FramePhaseProcessors? processors = null,
        FrameBudget budget = default)
    {
        processors ??= FramePhaseProcessors.Empty;
        budget = budget == default ? FrameBudget.ProcessAll : budget;

        FrameStats stats = new();
        if (!HasWork)
        {
            stats.CountNoWorkFrame();
            trace.RecordPhaseSummary(FramePhase.Idle, 0);
            return stats;
        }

        ProcessMeasure(processors, stats);
        ProcessArrange(processors, stats);
        ProcessRender(processors, stats);
        ProcessHitTest(processors, stats);

        return stats;
    }

    private void ProcessMeasure(FramePhaseProcessors processors, FrameStats stats)
    {
        IReadOnlyList<Elements.UIElement> snapshot = layoutQueue.SnapshotMeasure();
        foreach (Elements.UIElement element in snapshot)
        {
            processors.Process(FramePhase.Measure, element);
            InvalidationFlags cleared = ClearProcessedFlags(element, InvalidationFlags.Measure);
            layoutQueue.RemoveMeasure(element);
            stats.Count(FramePhase.Measure);
            trace.RecordPhase(FramePhase.Measure, element, InvalidationFlags.Measure);
            trace.RecordClear(element, cleared);
        }

        trace.RecordPhaseSummary(FramePhase.Measure, snapshot.Count);
    }

    private void ProcessArrange(FramePhaseProcessors processors, FrameStats stats)
    {
        IReadOnlyList<Elements.UIElement> snapshot = layoutQueue.SnapshotArrange();
        foreach (Elements.UIElement element in snapshot)
        {
            processors.Process(FramePhase.Arrange, element);
            InvalidationFlags cleared = ClearProcessedFlags(element, InvalidationFlags.Arrange);
            layoutQueue.RemoveArrange(element);
            stats.Count(FramePhase.Arrange);
            trace.RecordPhase(FramePhase.Arrange, element, InvalidationFlags.Arrange);
            trace.RecordClear(element, cleared);
        }

        trace.RecordPhaseSummary(FramePhase.Arrange, snapshot.Count);
    }

    private void ProcessRender(FramePhaseProcessors processors, FrameStats stats)
    {
        IReadOnlyList<Elements.UIElement> snapshot = renderQueue.Snapshot();
        foreach (Elements.UIElement element in snapshot)
        {
            processors.Process(FramePhase.RenderCache, element);
            InvalidationFlags cleared = ClearProcessedFlags(element, InvalidationFlags.Render);
            renderQueue.Remove(element);
            stats.Count(FramePhase.RenderCache);
            trace.RecordPhase(FramePhase.RenderCache, element, InvalidationFlags.Render);
            trace.RecordClear(element, cleared);
        }

        trace.RecordPhaseSummary(FramePhase.RenderCache, snapshot.Count);
    }

    private void ProcessHitTest(FramePhaseProcessors processors, FrameStats stats)
    {
        IReadOnlyList<Elements.UIElement> snapshot = hitTestQueue.Snapshot();
        foreach (Elements.UIElement element in snapshot)
        {
            processors.Process(FramePhase.HitTest, element);
            InvalidationFlags cleared = ClearProcessedFlags(element, InvalidationFlags.HitTest);
            hitTestQueue.Remove(element);
            stats.Count(FramePhase.HitTest);
            trace.RecordPhase(FramePhase.HitTest, element, InvalidationFlags.HitTest);
            trace.RecordClear(element, cleared);
        }

        trace.RecordPhaseSummary(FramePhase.HitTest, snapshot.Count);
    }

    private static InvalidationFlags ClearProcessedFlags(Elements.UIElement element, InvalidationFlags phaseFlag)
    {
        InvalidationFlags cleared = phaseFlag;
        element.DirtyState.Clear(phaseFlag);

        if ((element.DirtyState.Flags & ConcreteWorkFlags) == InvalidationFlags.None)
        {
            cleared |= SpecializedWorkFlags;
            element.DirtyState.Clear(SpecializedWorkFlags);
        }

        return cleared;
    }
}
