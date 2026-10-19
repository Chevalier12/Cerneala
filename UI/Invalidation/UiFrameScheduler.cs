using System.Diagnostics;
using Cerneala.UI.Detective;
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
    private LayoutProcessingPhase layoutProcessingPhase;
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

    internal bool IsProcessingMeasure => layoutProcessingPhase == LayoutProcessingPhase.Measure;

    internal bool IsProcessingArrange => layoutProcessingPhase == LayoutProcessingPhase.Arrange;

    internal long CurrentMeasurePass { get; private set; }

    internal long CurrentArrangePass { get; private set; }

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

    internal void BeginInheritedDescendantPropagation(Elements.UIElement element)
    {
        inheritedPropertyQueue.Remove(element);
        InvalidationFlags cleared = ClearProcessedFlags(element, InvalidationFlags.Inherited);
        trace.RecordClear(element, cleared);
    }

    // An element that measures or arranges queued elements itself takes over their queued work, so the work is
    // completed like the phase completes it: the queued entry is removed and its dirty flag cleared.
    internal void CompleteQueuedMeasure(Elements.UIElement element)
    {
        if (!layoutQueue.ContainsMeasure(element))
        {
            return;
        }

        layoutQueue.RemoveMeasure(element);
        InvalidationFlags cleared = ClearProcessedFlags(element, InvalidationFlags.Measure);
        trace.RecordClear(element, cleared);
    }

    // A propagated entry only stands for the measure of a descendant, which the phase skips, so a descendant that
    // measured itself completes it. The element's own queued measure stays queued.
    internal void CompletePropagatedQueuedMeasure(Elements.UIElement element)
    {
        if (layoutQueue.ContainsMeasure(element) &&
            layoutQueue.GetMeasureKind(element) == LayoutQueueEntryKind.Propagated)
        {
            CompleteQueuedMeasure(element);
        }
    }

    internal void CompleteQueuedArrange(Elements.UIElement element)
    {
        if (!layoutQueue.ContainsArrange(element))
        {
            return;
        }

        layoutQueue.RemoveArrange(element);
        InvalidationFlags cleared = ClearProcessedFlags(element, InvalidationFlags.Arrange);
        trace.RecordClear(element, cleared);
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
                if (!inheritedPropertyQueue.Contains(element))
                {
                    continue;
                }

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
        int processed = 0;
        for (int index = 0; index < snapshot.Count; index++)
        {
            Elements.UIElement element = snapshot[index];
            if (!aspectQueue.Contains(element))
            {
                continue;
            }

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
            processed++;
            trace.RecordPhase(FramePhase.Aspect, element, InvalidationFlags.Aspect);
            trace.RecordClear(element, cleared);
        }

        trace.RecordPhaseSummary(FramePhase.Aspect, processed);
    }

    private void ProcessMeasure(FramePhaseProcessors processors, FrameStats stats)
    {
        layoutProcessingDepth++;
        layoutProcessingPhase = LayoutProcessingPhase.Measure;
        CurrentMeasurePass++;
        try
        {
            ProcessMeasureCore(processors, stats);
        }
        finally
        {
            layoutProcessingDepth--;
            layoutProcessingPhase = LayoutProcessingPhase.None;
        }
    }

    private void ProcessMeasureCore(FramePhaseProcessors processors, FrameStats stats)
    {
        IReadOnlyList<Elements.UIElement> snapshot = processors.SupportsIncrementalMeasure
            ? layoutQueue.SnapshotMeasureIncremental()
            : layoutQueue.SnapshotMeasure();
        int processed = 0;
        int passCount = processors.SupportsIncrementalMeasure ? 3 : 1;
        for (int pass = 0; pass < passCount; pass++)
        {
            for (int offset = 0; offset < snapshot.Count; offset++)
            {
                int index = pass == 1 ? snapshot.Count - offset - 1 : offset;
                Elements.UIElement element = snapshot[index];
                if (!layoutQueue.ContainsMeasure(element))
                {
                    continue;
                }

                LayoutQueueEntryKind kind = layoutQueue.GetMeasureKind(element);
                if (processors.SupportsIncrementalMeasure && pass != 1 &&
                    kind is LayoutQueueEntryKind.Propagated or LayoutQueueEntryKind.Subtree)
                {
                    continue;
                }

                layoutQueue.RemoveMeasure(element);

                bool wasMeasuredRecursively = processors.SupportsIncrementalMeasure && WasMeasuredInCurrentPass(element);
                if (!Elements.UIElementVisibility.IsEffectivelyParticipatingInLayout(element) ||
                    wasMeasuredRecursively ||
                    DeferMeasureToSkippingAncestor(element))
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
                    // The parent's LayoutVersion is unchanged, so an ancestor that measured or arranged it
                    // earlier in this phase, or does so later with the same constraint or slot, would
                    // otherwise reuse the stale result and mark it as already measured or arranged.
                    parent.InvalidateMeasureCacheKeepingConstraint();
                    parent.InvalidateArrangeCache();
                    layoutQueue.RequireMeasure(parent);
                    layoutQueue.RequireArrange(parent);
                }
            }
        }

        if (processors.SupportsIncrementalMeasure)
        {
            CompleteOwnMeasuresQueuedDuringPhase();
        }

        trace.RecordPhaseSummary(FramePhase.Measure, processed);
    }

    private bool WasMeasuredInCurrentPass(Elements.UIElement element)
    {
        return ReferenceEquals(element.LastScheduledMeasureRoot, element.Root) &&
            element.LastScheduledMeasurePass == CurrentMeasurePass &&
            element.LastScheduledMeasureLayoutVersion == element.LayoutVersion;
    }

    // Measure work queued during this phase is not in its snapshot, for example the measure that a ScrollViewer's
    // measure queues for its ItemsControl when the viewport changes, right before it measures that ItemsControl. An
    // element that this pass measured after the request, at its current layout version, has completed its own queued
    // measure, as the snapshot loop completes it. A propagated entry stays queued: it stands for a descendant whose
    // queued measure can still be pending.
    private void CompleteOwnMeasuresQueuedDuringPhase()
    {
        if (layoutQueue.MeasureCount == 0)
        {
            return;
        }

        IReadOnlyList<Elements.UIElement> remaining = layoutQueue.SnapshotMeasure();
        for (int index = 0; index < remaining.Count; index++)
        {
            Elements.UIElement element = remaining[index];
            if (layoutQueue.GetMeasureKind(element) == LayoutQueueEntryKind.Propagated ||
                !WasMeasuredInCurrentPass(element))
            {
                continue;
            }

            layoutQueue.RemoveMeasure(element);
            InvalidationFlags cleared = ClearProcessedFlags(element, InvalidationFlags.Measure);
            trace.RecordClear(element, cleared);
        }
    }

    private void ProcessArrange(FramePhaseProcessors processors, FrameStats stats)
    {
        layoutProcessingDepth++;
        layoutProcessingPhase = LayoutProcessingPhase.Arrange;
        CurrentArrangePass++;
        try
        {
            ProcessArrangeCore(processors, stats);
        }
        finally
        {
            layoutProcessingDepth--;
            layoutProcessingPhase = LayoutProcessingPhase.None;
        }
    }

    private void ProcessArrangeCore(FramePhaseProcessors processors, FrameStats stats)
    {
        IReadOnlyList<Elements.UIElement> snapshot = layoutQueue.SnapshotArrange();
        int processed = 0;
        int passCount = processors.SupportsIncrementalMeasure ? 3 : 1;
        for (int pass = 0; pass < passCount; pass++)
        {
            for (int index = 0; index < snapshot.Count; index++)
            {
                Elements.UIElement element = snapshot[index];
                if (!layoutQueue.ContainsArrange(element))
                {
                    continue;
                }

                LayoutQueueEntryKind kind = layoutQueue.GetArrangeKind(element);
                if (processors.SupportsIncrementalMeasure && pass != 1 &&
                    kind is LayoutQueueEntryKind.Propagated or LayoutQueueEntryKind.Subtree &&
                    !(pass == 0 && kind == LayoutQueueEntryKind.Subtree && element is Elements.UIRoot))
                {
                    continue;
                }

                layoutQueue.RemoveArrange(element);

                bool wasArrangedRecursively = processors.SupportsIncrementalMeasure &&
                    ReferenceEquals(element.LastScheduledArrangeRoot, element.Root) &&
                    element.LastScheduledArrangePass == CurrentArrangePass &&
                    element.LastScheduledArrangeLayoutVersion == element.LayoutVersion &&
                    element.ArrangeDesiredSizesAreCurrent;
                if (!Elements.UIElementVisibility.IsEffectivelyParticipatingInLayout(element) ||
                    wasArrangedRecursively)
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

                // A required entry arranges the element with its last slot and reuses a valid arrange cache
                // (UIElement.RequireArrangeForDesiredSizeChange). Every request for the element's own arrange also clears
                // that cache, so its Arrange flag can only add a measure ancestor propagation, which stands for the
                // arrange of a queued descendant, as a propagated entry does. Kept, the flag would make the arrange skip
                // the cache and run ArrangeCore again, and an overlay layer that measures its content there would queue
                // the content's measure for the next frame (#514).
                InvalidationFlags propagatedArrange = InvalidationFlags.None;
                if (processors.SupportsIncrementalMeasure && kind == LayoutQueueEntryKind.Required)
                {
                    propagatedArrange = element.DirtyState.Flags & InvalidationFlags.Arrange;
                    element.DirtyState.Clear(propagatedArrange);
                }

                try
                {
                    processors.Process(FramePhase.Arrange, element);
                }
                catch
                {
                    element.DirtyState.Mark(propagatedArrange);
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

    // An element that an ancestor's measure leaves unmeasured on purpose, such as a row outside the realization
    // window of a VirtualizingStackPanel, is measured by that ancestor once it realizes it, never by the frame alone.
    // A frame measure would have reset the caches of the element's ancestors when its desired size changed; without
    // it they stay valid, so they are reset here up to the skipped child, whose measure then reaches the element.
    private static bool DeferMeasureToSkippingAncestor(Elements.UIElement element)
    {
        for (Elements.UIElement current = element; current.VisualParent is Elements.UIElement parent; current = parent)
        {
            if (parent.SkipsChildMeasure(current))
            {
                for (Elements.UIElement path = element; ; path = (Elements.UIElement)path.VisualParent!)
                {
                    path.InvalidateMeasureCacheKeepingConstraint();
                    if (ReferenceEquals(path, current))
                    {
                        break;
                    }
                }

                return true;
            }
        }

        return false;
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

internal enum LayoutProcessingPhase
{
    None,
    Measure,
    Arrange
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
