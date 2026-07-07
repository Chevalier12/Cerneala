using Cerneala.UI.Diagnostics;
using Cerneala.UI.Motion.Core;

namespace Cerneala.UI.Invalidation;

public sealed class UiFrameScheduler
{
    private readonly LayoutQueue layoutQueue;
    private readonly InheritedPropertyQueue inheritedPropertyQueue;
    private readonly CommandStateQueue commandStateQueue;
    private readonly StyleQueue styleQueue;
    private readonly RenderQueue renderQueue;
    private readonly HitTestQueue hitTestQueue;
    private readonly InvalidationTrace trace;
    private const InvalidationFlags ConcreteWorkFlags =
        InvalidationFlags.Inherited |
        InvalidationFlags.Style |
        InvalidationFlags.Measure |
        InvalidationFlags.Arrange |
        InvalidationFlags.Render |
        InvalidationFlags.HitTest;

    private const InvalidationFlags SpecializedWorkFlags =
        InvalidationFlags.Text |
        InvalidationFlags.Image |
        InvalidationFlags.Resource |
        InvalidationFlags.InputVisual |
        InvalidationFlags.Semantics |
        InvalidationFlags.Subtree;

    public UiFrameScheduler(
        LayoutQueue layoutQueue,
        InheritedPropertyQueue inheritedPropertyQueue,
        CommandStateQueue commandStateQueue,
        StyleQueue styleQueue,
        RenderQueue renderQueue,
        HitTestQueue hitTestQueue,
        InvalidationTrace? trace = null)
    {
        this.layoutQueue = layoutQueue ?? throw new ArgumentNullException(nameof(layoutQueue));
        this.inheritedPropertyQueue = inheritedPropertyQueue ?? throw new ArgumentNullException(nameof(inheritedPropertyQueue));
        this.commandStateQueue = commandStateQueue ?? throw new ArgumentNullException(nameof(commandStateQueue));
        this.styleQueue = styleQueue ?? throw new ArgumentNullException(nameof(styleQueue));
        this.renderQueue = renderQueue ?? throw new ArgumentNullException(nameof(renderQueue));
        this.hitTestQueue = hitTestQueue ?? throw new ArgumentNullException(nameof(hitTestQueue));
        this.trace = trace ?? InvalidationTrace.Disabled;
    }

    public bool HasWork =>
        inheritedPropertyQueue.HasWork ||
        commandStateQueue.HasWork ||
        styleQueue.HasWork ||
        layoutQueue.HasWork ||
        renderQueue.HasWork ||
        hitTestQueue.HasWork;

    public FrameStats ProcessFrame(
        FramePhaseProcessors? processors = null,
        FrameBudget budget = default,
        FrameStats? stats = null,
        MotionFrameCoordinator? motion = null,
        MotionFrameReason motionReason = MotionFrameReason.Scheduled)
    {
        processors ??= FramePhaseProcessors.Empty;
        budget = budget == default ? FrameBudget.ProcessAll : budget;

        stats ??= new FrameStats();
        if (!HasWork)
        {
            if (motion is not null)
            {
                stats.CountMotion(motion.BeginFrame(motionReason));
                stats.CountMotion(motion.BeforeRender());
                stats.CountMotion(motion.EndFrame());
                return stats;
            }

            stats.CountNoWorkFrame();
            trace.RecordPhaseSummary(FramePhase.Idle, 0);
            return stats;
        }

        // MVP scheduler contract: each phase processes one deterministic snapshot.
        // Same-phase work enqueued during processing is deferred to a later frame.
        // Downstream phase work may still run in this frame if its snapshot has not been taken yet.
        stats.CountMotion(motion?.BeginFrame(motionReason) ?? default);
        ProcessInheritedProperties(processors, stats);
        ProcessCommandState(processors, stats);
        ProcessStyle(processors, stats);
        ProcessInheritedProperties(processors, stats);
        stats.CountMotion(motion?.BeforeLayout() ?? default);
        ProcessMeasure(processors, stats);
        ProcessArrange(processors, stats);
        stats.CountMotion(motion?.AfterLayout() ?? default);
        stats.CountMotion(motion?.BeforeRender() ?? default);
        ProcessRender(processors, stats);
        ProcessHitTest(processors, stats);
        stats.CountMotion(motion?.EndFrame() ?? default);

        return stats;
    }

    private void ProcessInheritedProperties(FramePhaseProcessors processors, FrameStats stats)
    {
        int processed = 0;
        while (inheritedPropertyQueue.HasWork)
        {
            IReadOnlyList<Elements.UIElement> snapshot = inheritedPropertyQueue.Snapshot();
            if (snapshot.Count == 0)
            {
                break;
            }

            foreach (Elements.UIElement element in snapshot)
            {
                inheritedPropertyQueue.Remove(element);
                InvalidationFlags cleared = ClearProcessedFlags(element, InvalidationFlags.Inherited);
                try
                {
                    processors.Process(FramePhase.InheritedProperties, element);
                }
                catch
                {
                    element.DirtyState.Mark(cleared);
                    inheritedPropertyQueue.Enqueue(element);
                    throw;
                }

                stats.Count(FramePhase.InheritedProperties);
                processed++;
                trace.RecordPhase(FramePhase.InheritedProperties, element, InvalidationFlags.Inherited);
                trace.RecordClear(element, cleared);
            }
        }

        trace.RecordPhaseSummary(FramePhase.InheritedProperties, processed);
    }

    private void ProcessCommandState(FramePhaseProcessors processors, FrameStats stats)
    {
        IReadOnlyList<Elements.UIElement> snapshot = commandStateQueue.Snapshot();
        foreach (Elements.UIElement element in snapshot)
        {
            commandStateQueue.Remove(element);
            try
            {
                processors.Process(FramePhase.CommandState, element);
            }
            catch
            {
                commandStateQueue.Enqueue(element);
                throw;
            }

            stats.Count(FramePhase.CommandState);
            trace.RecordPhase(FramePhase.CommandState, element, InvalidationFlags.None);
        }

        trace.RecordPhaseSummary(FramePhase.CommandState, snapshot.Count);
    }

    private void ProcessStyle(FramePhaseProcessors processors, FrameStats stats)
    {
        IReadOnlyList<Elements.UIElement> snapshot = styleQueue.Snapshot();
        foreach (Elements.UIElement element in snapshot)
        {
            styleQueue.Remove(element);
            InvalidationFlags cleared = ClearProcessedFlags(element, InvalidationFlags.Style);
            try
            {
                processors.Process(FramePhase.Style, element);
            }
            catch
            {
                element.DirtyState.Mark(cleared);
                styleQueue.Enqueue(element);
                throw;
            }

            stats.Count(FramePhase.Style);
            trace.RecordPhase(FramePhase.Style, element, InvalidationFlags.Style);
            trace.RecordClear(element, cleared);
        }

        trace.RecordPhaseSummary(FramePhase.Style, snapshot.Count);
    }

    private void ProcessMeasure(FramePhaseProcessors processors, FrameStats stats)
    {
        IReadOnlyList<Elements.UIElement> snapshot = layoutQueue.SnapshotMeasure();
        foreach (Elements.UIElement element in snapshot)
        {
            layoutQueue.RemoveMeasure(element);
            try
            {
                processors.Process(FramePhase.Measure, element);
            }
            catch
            {
                layoutQueue.EnqueueMeasure(element);
                throw;
            }

            stats.Count(FramePhase.Measure);
            trace.RecordPhase(FramePhase.Measure, element, InvalidationFlags.Measure);
            if (!layoutQueue.ContainsMeasure(element))
            {
                InvalidationFlags cleared = ClearProcessedFlags(element, InvalidationFlags.Measure);
                trace.RecordClear(element, cleared);
            }
        }

        trace.RecordPhaseSummary(FramePhase.Measure, snapshot.Count);
    }

    private void ProcessArrange(FramePhaseProcessors processors, FrameStats stats)
    {
        IReadOnlyList<Elements.UIElement> snapshot = layoutQueue.SnapshotArrange();
        foreach (Elements.UIElement element in snapshot)
        {
            layoutQueue.RemoveArrange(element);
            try
            {
                processors.Process(FramePhase.Arrange, element);
            }
            catch
            {
                layoutQueue.EnqueueArrange(element);
                throw;
            }

            stats.Count(FramePhase.Arrange);
            trace.RecordPhase(FramePhase.Arrange, element, InvalidationFlags.Arrange);
            if (!layoutQueue.ContainsArrange(element))
            {
                InvalidationFlags cleared = ClearProcessedFlags(element, InvalidationFlags.Arrange);
                trace.RecordClear(element, cleared);
            }
        }

        trace.RecordPhaseSummary(FramePhase.Arrange, snapshot.Count);
    }

    private void ProcessRender(FramePhaseProcessors processors, FrameStats stats)
    {
        IReadOnlyList<Elements.UIElement> snapshot = renderQueue.Snapshot();
        foreach (Elements.UIElement element in snapshot)
        {
            renderQueue.Remove(element);
            try
            {
                processors.Process(FramePhase.RenderCache, element);
            }
            catch
            {
                renderQueue.Enqueue(element);
                throw;
            }

            stats.Count(FramePhase.RenderCache);
            trace.RecordPhase(FramePhase.RenderCache, element, InvalidationFlags.Render);
            if (!renderQueue.Contains(element))
            {
                InvalidationFlags cleared = ClearProcessedFlags(element, InvalidationFlags.Render);
                trace.RecordClear(element, cleared);
            }
        }

        trace.RecordPhaseSummary(FramePhase.RenderCache, snapshot.Count);
    }

    private void ProcessHitTest(FramePhaseProcessors processors, FrameStats stats)
    {
        IReadOnlyList<Elements.UIElement> snapshot = hitTestQueue.Snapshot();
        foreach (Elements.UIElement element in snapshot)
        {
            hitTestQueue.Remove(element);
            try
            {
                processors.Process(FramePhase.HitTest, element);
            }
            catch
            {
                hitTestQueue.Enqueue(element);
                throw;
            }

            stats.Count(FramePhase.HitTest);
            trace.RecordPhase(FramePhase.HitTest, element, InvalidationFlags.HitTest);
            if (!hitTestQueue.Contains(element))
            {
                InvalidationFlags cleared = ClearProcessedFlags(element, InvalidationFlags.HitTest);
                trace.RecordClear(element, cleared);
            }
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
