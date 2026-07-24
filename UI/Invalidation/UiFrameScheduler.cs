using System.Diagnostics;
using Cerneala.UI.Diagnostics;
using Cerneala.UI.Motion.Core;

namespace Cerneala.UI.Invalidation;

public sealed class UiFrameScheduler
{
    private readonly LayoutQueue layoutQueue;
    private readonly InheritedPropertyQueue inheritedPropertyQueue;
    private readonly CommandStateQueue commandStateQueue;
    private readonly AspectQueue aspectQueue;
    private readonly RenderQueue renderQueue;
    private readonly HitTestQueue hitTestQueue;
    private readonly InvalidationTrace trace;
    private int layoutProcessingDepth;
    private const InvalidationFlags ConcreteWorkFlags =
        InvalidationFlags.Inherited |
        InvalidationFlags.Aspect |
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
        AspectQueue aspectQueue,
        RenderQueue renderQueue,
        HitTestQueue hitTestQueue,
        InvalidationTrace? trace = null)
    {
        this.layoutQueue = layoutQueue ?? throw new ArgumentNullException(nameof(layoutQueue));
        this.inheritedPropertyQueue = inheritedPropertyQueue ?? throw new ArgumentNullException(nameof(inheritedPropertyQueue));
        this.commandStateQueue = commandStateQueue ?? throw new ArgumentNullException(nameof(commandStateQueue));
        this.aspectQueue = aspectQueue ?? throw new ArgumentNullException(nameof(aspectQueue));
        this.renderQueue = renderQueue ?? throw new ArgumentNullException(nameof(renderQueue));
        this.hitTestQueue = hitTestQueue ?? throw new ArgumentNullException(nameof(hitTestQueue));
        this.trace = trace ?? InvalidationTrace.Disabled;
    }

    public bool HasWork =>
        inheritedPropertyQueue.HasWork ||
        commandStateQueue.HasWork ||
        aspectQueue.HasWork ||
        layoutQueue.HasWork ||
        renderQueue.HasWork ||
        hitTestQueue.HasWork;

    internal bool IsProcessingLayout => layoutProcessingDepth > 0;

    internal FramePhaseTiming LastFrameTiming { get; private set; }

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
        LastFrameTiming = default;
        if (!HasWork)
        {
            if (motion is not null)
            {
                long noWorkPhaseStarted = Stopwatch.GetTimestamp();
                stats.CountMotion(motion.BeginFrame(motionReason));
                stats.CountMotion(motion.BeforeLayout());
                TimeSpan noWorkMotionTime = Stopwatch.GetElapsedTime(noWorkPhaseStarted);
                noWorkPhaseStarted = Stopwatch.GetTimestamp();
                ProcessMeasure(processors, stats);
                TimeSpan noWorkMeasureTime = Stopwatch.GetElapsedTime(noWorkPhaseStarted);
                noWorkPhaseStarted = Stopwatch.GetTimestamp();
                ProcessArrange(processors, stats);
                TimeSpan noWorkArrangeTime = Stopwatch.GetElapsedTime(noWorkPhaseStarted);
                noWorkPhaseStarted = Stopwatch.GetTimestamp();
                stats.CountMotion(motion.AfterLayout());
                stats.CountMotion(motion.BeforeRender());
                noWorkMotionTime += Stopwatch.GetElapsedTime(noWorkPhaseStarted);
                noWorkPhaseStarted = Stopwatch.GetTimestamp();
                ProcessRender(processors, stats);
                TimeSpan noWorkRenderTime = Stopwatch.GetElapsedTime(noWorkPhaseStarted);
                noWorkPhaseStarted = Stopwatch.GetTimestamp();
                ProcessHitTest(processors, stats);
                TimeSpan noWorkHitTestTime = Stopwatch.GetElapsedTime(noWorkPhaseStarted);
                noWorkPhaseStarted = Stopwatch.GetTimestamp();
                stats.CountMotion(motion.EndFrame());
                noWorkMotionTime += Stopwatch.GetElapsedTime(noWorkPhaseStarted);
                LastFrameTiming = new FramePhaseTiming(
                    default,
                    default,
                    default,
                    noWorkMeasureTime,
                    noWorkArrangeTime,
                    noWorkRenderTime,
                    noWorkHitTestTime,
                    noWorkMotionTime);
                return stats;
            }

            stats.CountNoWorkFrame();
            trace.RecordPhaseSummary(FramePhase.Idle, 0);
            return stats;
        }

        // MVP scheduler contract: each phase processes one deterministic snapshot.
        // Same-phase work enqueued during processing is deferred to a later frame.
        // Downstream phase work may still run in this frame if its snapshot has not been taken yet.
        long phaseStarted = Stopwatch.GetTimestamp();
        stats.CountMotion(motion?.BeginFrame(motionReason) ?? default);
        TimeSpan motionTime = Stopwatch.GetElapsedTime(phaseStarted);
        phaseStarted = Stopwatch.GetTimestamp();
        ProcessInheritedProperties(processors, stats);
        TimeSpan inheritedTime = Stopwatch.GetElapsedTime(phaseStarted);
        phaseStarted = Stopwatch.GetTimestamp();
        ProcessCommandState(processors, stats);
        TimeSpan commandStateTime = Stopwatch.GetElapsedTime(phaseStarted);
        phaseStarted = Stopwatch.GetTimestamp();
        ProcessAspect(processors, stats);
        TimeSpan aspectTime = Stopwatch.GetElapsedTime(phaseStarted);
        phaseStarted = Stopwatch.GetTimestamp();
        ProcessInheritedProperties(processors, stats);
        inheritedTime += Stopwatch.GetElapsedTime(phaseStarted);
        phaseStarted = Stopwatch.GetTimestamp();
        stats.CountMotion(motion?.BeforeLayout() ?? default);
        motionTime += Stopwatch.GetElapsedTime(phaseStarted);
        phaseStarted = Stopwatch.GetTimestamp();
        ProcessMeasure(processors, stats);
        TimeSpan measureTime = Stopwatch.GetElapsedTime(phaseStarted);
        phaseStarted = Stopwatch.GetTimestamp();
        ProcessArrange(processors, stats);
        TimeSpan arrangeTime = Stopwatch.GetElapsedTime(phaseStarted);
        phaseStarted = Stopwatch.GetTimestamp();
        stats.CountMotion(motion?.AfterLayout() ?? default);
        stats.CountMotion(motion?.BeforeRender() ?? default);
        motionTime += Stopwatch.GetElapsedTime(phaseStarted);
        phaseStarted = Stopwatch.GetTimestamp();
        ProcessRender(processors, stats);
        TimeSpan renderTime = Stopwatch.GetElapsedTime(phaseStarted);
        phaseStarted = Stopwatch.GetTimestamp();
        ProcessHitTest(processors, stats);
        TimeSpan hitTestTime = Stopwatch.GetElapsedTime(phaseStarted);
        phaseStarted = Stopwatch.GetTimestamp();
        stats.CountMotion(motion?.EndFrame() ?? default);
        motionTime += Stopwatch.GetElapsedTime(phaseStarted);
        LastFrameTiming = new FramePhaseTiming(
            inheritedTime,
            commandStateTime,
            aspectTime,
            measureTime,
            arrangeTime,
            renderTime,
            hitTestTime,
            motionTime);

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

            for (int index = 0; index < snapshot.Count; index++)
            {
                Elements.UIElement element = snapshot[index];
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
        for (int index = 0; index < snapshot.Count; index++)
        {
            Elements.UIElement element = snapshot[index];
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

    private void ProcessAspect(FramePhaseProcessors processors, FrameStats stats)
    {
        IReadOnlyList<Elements.UIElement> snapshot = aspectQueue.Snapshot();
        for (int index = 0; index < snapshot.Count; index++)
        {
            Elements.UIElement element = snapshot[index];
            aspectQueue.Remove(element);
            InvalidationFlags cleared = ClearProcessedFlags(element, InvalidationFlags.Aspect);
            try
            {
                processors.Process(FramePhase.Aspect, element);
            }
            catch
            {
                element.DirtyState.Mark(cleared);
                aspectQueue.Enqueue(element);
                throw;
            }

            stats.Count(FramePhase.Aspect);
            trace.RecordPhase(FramePhase.Aspect, element, InvalidationFlags.Aspect);
            trace.RecordClear(element, cleared);
        }

        trace.RecordPhaseSummary(FramePhase.Aspect, snapshot.Count);
    }

    private void ProcessMeasure(FramePhaseProcessors processors, FrameStats stats)
    {
        layoutProcessingDepth++;
        try
        {
            ProcessMeasureCore(processors, stats);
        }
        finally
        {
            layoutProcessingDepth--;
        }
    }

    private void ProcessMeasureCore(FramePhaseProcessors processors, FrameStats stats)
    {
        IReadOnlyList<Elements.UIElement> snapshot = processors.SupportsIncrementalMeasure
            ? layoutQueue.SnapshotMeasureIncremental()
            : layoutQueue.SnapshotMeasure();
        int processed = 0;
        for (int index = 0; index < snapshot.Count; index++)
        {
            Elements.UIElement element = snapshot[index];
            LayoutQueueEntryKind kind = layoutQueue.GetMeasureKind(element);
            layoutQueue.RemoveMeasure(element);

            if (!Elements.UIElementVisibility.IsEffectivelyParticipatingInLayout(element))
            {
                InvalidationFlags skipped = ClearProcessedFlags(element, InvalidationFlags.Measure);
                trace.RecordClear(element, skipped);
                continue;
            }

            if (processors.SupportsIncrementalMeasure && kind == LayoutQueueEntryKind.Propagated)
            {
                InvalidationFlags skipped = ClearProcessedFlags(element, InvalidationFlags.Measure);
                trace.RecordClear(element, skipped);
                continue;
            }

            bool desiredSizeChanged;
            try
            {
                desiredSizeChanged = processors.ProcessMeasure(element);
            }
            catch
            {
                layoutQueue.EnqueueMeasure(element, kind);
                throw;
            }

            stats.Count(FramePhase.Measure);
            processed++;
            trace.RecordPhase(FramePhase.Measure, element, InvalidationFlags.Measure);
            if (!layoutQueue.ContainsMeasure(element))
            {
                InvalidationFlags cleared = ClearProcessedFlags(element, InvalidationFlags.Measure);
                trace.RecordClear(element, cleared);
            }

            if (processors.SupportsIncrementalMeasure &&
                desiredSizeChanged &&
                !element.IsLayoutBoundary &&
                element.VisualParent is Elements.UIElement parent)
            {
                parent.DirtyState.Mark(InvalidationFlags.Measure | InvalidationFlags.Arrange);
                layoutQueue.RequireMeasure(parent);
                layoutQueue.RequireArrange(parent);
            }
        }

        trace.RecordPhaseSummary(FramePhase.Measure, processed);
    }

    private void ProcessArrange(FramePhaseProcessors processors, FrameStats stats)
    {
        layoutProcessingDepth++;
        try
        {
            ProcessArrangeCore(processors, stats);
        }
        finally
        {
            layoutProcessingDepth--;
        }
    }

    private void ProcessArrangeCore(FramePhaseProcessors processors, FrameStats stats)
    {
        IReadOnlyList<Elements.UIElement> snapshot = layoutQueue.SnapshotArrange();
        int processed = 0;
        for (int index = 0; index < snapshot.Count; index++)
        {
            Elements.UIElement element = snapshot[index];
            LayoutQueueEntryKind kind = layoutQueue.GetArrangeKind(element);
            layoutQueue.RemoveArrange(element);

            if (!Elements.UIElementVisibility.IsEffectivelyParticipatingInLayout(element))
            {
                InvalidationFlags skipped = ClearProcessedFlags(element, InvalidationFlags.Arrange);
                trace.RecordClear(element, skipped);
                continue;
            }

            if (processors.SupportsIncrementalMeasure && kind == LayoutQueueEntryKind.Propagated)
            {
                InvalidationFlags skipped = ClearProcessedFlags(element, InvalidationFlags.Arrange);
                trace.RecordClear(element, skipped);
                continue;
            }

            try
            {
                processors.Process(FramePhase.Arrange, element);
            }
            catch
            {
                layoutQueue.EnqueueArrange(element, kind);
                throw;
            }

            stats.Count(FramePhase.Arrange);
            processed++;
            trace.RecordPhase(FramePhase.Arrange, element, InvalidationFlags.Arrange);
            if (!layoutQueue.ContainsArrange(element))
            {
                InvalidationFlags cleared = ClearProcessedFlags(element, InvalidationFlags.Arrange);
                trace.RecordClear(element, cleared);
            }
        }

        trace.RecordPhaseSummary(FramePhase.Arrange, processed);
    }

    private void ProcessRender(FramePhaseProcessors processors, FrameStats stats)
    {
        IReadOnlyList<Elements.UIElement> snapshot = renderQueue.Snapshot();
        for (int index = 0; index < snapshot.Count; index++)
        {
            Elements.UIElement element = snapshot[index];
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
        for (int index = 0; index < snapshot.Count; index++)
        {
            Elements.UIElement element = snapshot[index];
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

internal readonly record struct FramePhaseTiming(
    TimeSpan InheritedProperties,
    TimeSpan CommandState,
    TimeSpan Aspect,
    TimeSpan Measure,
    TimeSpan Arrange,
    TimeSpan Render,
    TimeSpan HitTest,
    TimeSpan Motion);
