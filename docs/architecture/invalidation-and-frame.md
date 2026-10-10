# Invalidation And Frame Scheduling

> Code: `UI/Invalidation`, `UI/Elements/UIRoot.cs`, `UI/Elements/ElementLifecycle.cs` · Verified at commit `0cb32300` (2026-10-10)

## Responsibility

The invalidation system records which retained work an element needs (measure,
arrange, render cache, hit test, Aspect, inherited values, command state), keeps
that work in per-root queues, and runs it in a fixed phase order once per frame.
It does not decide what a phase does: each phase calls a processor supplied by
`UIRoot`. It does not touch input dispatch, drawing, or the Relay drain; those
run in `UiHost` and `UIRoot.BeginUpdate` (see
[Where The Scheduler Runs](#where-the-scheduler-runs)).

## Components

| Type | File | Role | Owner |
|---|---|---|---|
| `UiFrameScheduler` | `UI/Invalidation/UiFrameScheduler.cs` | Runs the phases in order over queue snapshots and counts work in `FrameStats`. | `UIRoot` (one per root, `UIRoot.cs:103`) |
| `FramePhase` | `UI/Invalidation/FramePhase.cs` | `Input`, `InheritedProperties`, `CommandState`, `Aspect`, `Measure`, `Arrange`, `RenderCache`, `HitTest`, `Idle`. The scheduler never runs `Input`. | — |
| `FramePhaseProcessors` | `UI/Invalidation/FramePhaseProcessors.cs` | One callback per phase. `UIRoot.CreatePhaseProcessors` (`UIRoot.cs:483-503`) supplies the production set; tests pass their own. | The caller of `ProcessFrame` |
| `InvalidationFlags` | `UI/Invalidation/InvalidationFlags.cs` | Kinds of work: `Measure`, `Arrange`, `Render`, `Text`, `Image`, `Resource`, `Aspect`, `InputVisual`, `HitTest`, `Subtree`, `Inherited`, `Semantics`. | — |
| `InvalidationRequest` | `UI/Invalidation/InvalidationRequest.cs` | Target element, flags, reason, optional source property. | The caller |
| `DirtyState` | `UI/Invalidation/DirtyState.cs` | The flags an element still owes. `Mark` adds flags, `Clear` removes them. | One element |
| `DirtyPropagation` | `UI/Invalidation/DirtyPropagation.cs` | Expands request flags (`GetEffectiveFlags`), marks the target, ancestors and optional subtree, and enqueues them. | Process (`DirtyPropagation.Default`) |
| `LayoutQueue` | `UI/Invalidation/LayoutQueue.cs` | Two work queues, measure and arrange. Each entry carries a kind: `Propagated`, `Subtree`, `Required`, `Direct` (internal `LayoutQueueEntryKind`). A second enqueue keeps the stronger kind. | `UIRoot` |
| `InheritedPropertyQueue`, `CommandStateQueue`, `AspectQueue`, `RenderQueue`, `HitTestQueue` | `UI/Invalidation/<Name>.cs` | Thin wrappers over one work queue each. | `UIRoot` |
| `ElementWorkQueue<TMetadata>` (internal) | `UI/Invalidation/ElementWorkQueue.cs` | Shared core of all queues: one entry per element (by reference), an enqueue sequence number, and `Snapshot`. | Its wrapper queue |
| `ElementQueueOrderIndex` (internal) | `UI/Invalidation/ElementQueueOrderIndex.cs` | Preorder ordinal of every element in the visual tree, rebuilt only when `UIRoot.TreeVersion` changes. Shared by all queues of a root. | `UIRoot` (`UIRoot.cs:84`) |
| `FrameStats` | `UI/Invalidation/FrameStats.cs` | Counters for one frame. | One frame (`UiHost` creates one per update, `UiHost.cs:158`) |
| `FrameBudget` | `UI/Invalidation/FrameBudget.cs` | `FrameBudget(int? MaxWorkItems)`. Deferred: `DefersWork` is the constant `false`. | The caller |
| `InvalidationTrace` | `UI/Detective/InvalidationTrace.cs` | Optional record of requests, propagation, queueing, phases and clears. | `UIRoot` |

`UIRoot` constructs all six queues and the scheduler in its constructor
(`UIRoot.cs:84-90`, `:103`).

## Data Flow

1. **A request arrives.** A property change maps its `UiPropertyOptions` to
   flags (`UIElement.MapInvalidationOptions`, `UIElement.cs:1400-1444`), or code
   calls `UIElement.Invalidate(InvalidationFlags, string)`. A detached element
   only marks its own `DirtyState` and queues nothing (`UIElement.cs:1214-1237`).
2. **`UIRoot.Invalidate`** (`UIRoot.cs:355-378`) records the request in the
   trace, marks the root command list invalid when the flags include `Render`,
   marks the input route cache dirty for enable/visibility/handler changes, and
   calls `DirtyPropagation.Default.Propagate`.
3. **`DirtyPropagation.GetEffectiveFlags`** adds implied work. Examples:
   `Measure` adds `Arrange | Render`; `Arrange` adds `Render`; an `Inherits`
   property adds `Inherited | Subtree`.
4. **`Propagate`** (`DirtyPropagation.cs:10-61`):
   - the target gets the effective flags without `Subtree`, as a `Direct` layout
     entry (or `Subtree` when the request itself had `Subtree`);
   - with `Measure`, every visual ancestor gets `Measure | Arrange` as a
     `Propagated` entry, up to and including the first ancestor whose
     `IsLayoutBoundary` is `true` (`UIRoot` sets it, `UIRoot.cs:107`);
   - only when the request flags contain `Subtree`, every visual descendant gets
     the same flags as a `Subtree` entry.
5. **`MarkAndQueue`** (`DirtyPropagation.cs:118-177`) adds the flags to
   `DirtyState` and enqueues one queue per flag: `Measure` and `Arrange` go to
   `LayoutQueue`, `Inherited` to `InheritedPropertyQueue`, `Aspect` to
   `AspectQueue`, `Render` to `RenderQueue`, `HitTest` to `HitTestQueue`.
   `CommandStateQueue` is not fed here: `UIElement.QueueCommandStateRefresh`
   enqueues an `ICommandStateSource` directly (`UIElement.cs:806-820`).
6. **`UiFrameScheduler.ProcessFrame`** runs the phases (order below). Each phase
   takes a `Snapshot`, and for each element: removes it from the queue, calls
   the processor, and counts it. `InheritedProperties` and `Aspect` clear their
   flag before the call. `Measure`, `Arrange`, `RenderCache` and `HitTest`
   clear it after the call, and only when the processor did not enqueue the
   element again (for example `UiFrameScheduler.cs:357-361`, `:481-485`).
   `CommandState` has no flag.

Example. A `Button` inside a `StackPanel`, attached to a root. The code runs
`button.FontSize = 20;`. `Control.FontSizeProperty` has
`Inherits | AffectsMeasure | AffectsRender` (`Control.cs:76-82`).

- The button gets `Measure | Arrange | Render | Inherited` and enters the
  measure, arrange, render and inherited-property queues.
- The `StackPanel` and the root get `Measure | Arrange` as `Propagated` layout
  entries. The root is a layout boundary, so the walk stops there.
- The button's children are not enqueued now: the request flags have no
  `Subtree`. The `InheritedProperties` phase copies the new size to them during
  the frame.

## Where The Scheduler Runs

One `UiHost` update (`UiHost.UpdateCore`, `UI/Hosting/UiHost.cs:145-244`) runs
these steps, in this order:

```text
1. when render time advances: Motion runtime tick and time-sensitive
   render invalidation (ObjectMotionRuntime, TimeSensitiveRenderInvalidator)
2. UIRoot.BeginUpdate: Relay drain (UiRelay.Drain), outside the scheduler
3. scheduler pass, only when queued work exists (MotionFrameReason.Scheduled)
4. input dispatch (ElementInputBridge.Dispatch)
5. scheduler pass for queued work, or for active Motion not yet sampled
   in this update (MotionFrameReason.Input)
6. cursor resolution, then one more scheduler pass if it queued work
7. root command list commit (RetainedRenderer.Commit)
```

Each scheduler pass is one `UiFrameScheduler.ProcessFrame`. Input dispatch
happens between passes, so input sees the layout that step 3 committed.
Drawing happens later, in `UiHost.Draw` (see [Retained Rendering](rendering.md)).

## Phase Order

`ProcessFrame` (`UiFrameScheduler.cs:72-179`) with queued work:

| Step | Phase | Line |
|---|---|---|
| 1 | Motion `BeginFrame` (only with a Motion coordinator) | `:132` |
| 2 | `InheritedProperties` | `:135` |
| 3 | `CommandState` | `:138` |
| 4 | `Aspect` | `:141` |
| 5 | `InheritedProperties` again, for elements enqueued by steps 3-4 | `:144` |
| 6 | Motion `BeforeLayout` | `:147` |
| 7 | `Measure` | `:150` |
| 8 | `Arrange` | `:153` |
| 9 | Motion `AfterLayout`, `BeforeRender` | `:156-157` |
| 10 | `RenderCache` | `:160` |
| 11 | `HitTest` | `:163` |
| 12 | Motion `EndFrame` | `:166` |

Without queued work (`HasWork` is `false`): when a Motion coordinator is present,
the scheduler still runs the Motion hooks and the `Measure`, `Arrange`,
`RenderCache` and `HitTest` phases, but not `InheritedProperties`,
`CommandState` or `Aspect` (`:86-121`). Otherwise it only counts a no-work frame
(`FrameStats.CountNoWorkFrame`) and records an `Idle` summary (`:123-124`).

### Snapshots And Order

`ElementWorkQueue.Snapshot` (`ElementWorkQueue.cs:60-116`):

- calls `ElementQueueOrderIndex.EnsureCurrent`, which rebuilds the preorder
  index only if `UIRoot.TreeVersion` changed since the last build
  (`ElementQueueOrderIndex.cs:22-41`);
- sorts entries by preorder ordinal, then by enqueue sequence. A parent always
  comes before its child, whatever order they were enqueued in;
- puts elements that are attached but playing a presence exit animation (no
  ordinal) last;
- drops entries whose element no longer belongs to this root.

`TreeVersion` grows when children are inserted, moved or removed
(`UIElementCollection`), when the viewport changes (`UIRoot.SetViewport`), and
when a presence exit finishes (`PresenceCoordinator`). All increments go through
`UIRoot.IncrementTreeVersion` (`UIRoot.cs:337-342`).

Measure takes the snapshot in reverse (children first) when the processors
support incremental measure; the production processors do. See
[Layout](layout.md) for the measure passes.

### Work Added During A Phase

- Work added to a later phase runs in the same frame. Example: a measure
  callback enqueues arrange work, and the `Arrange` phase of the same frame
  processes it.
- Work added to the phase that is running is not in its snapshot and waits for
  the next frame. Example: an Aspect callback invalidates its own element for
  `Aspect`; the element is processed again one frame later.
- Exception: `InheritedProperties` loops until its queue is empty
  (`UiFrameScheduler.cs:191`), so inherited work added during that phase runs in
  the same phase.

## Lifecycle And Ownership

- The queues, the order index and the scheduler live as long as their `UIRoot`.
- **Detach.** When an element leaves the tree, `ElementLifecycle.DetachSingle`
  (`ElementLifecycle.cs:105-118`) calls `UIRoot.RemovePendingWork`
  (`UIRoot.cs:344-353`), which removes the element from all seven work queues
  (measure, arrange, and the five others). The call runs after
  `DetachFromRoot`, so work an `OnDetached` override enqueues is removed too.
  `Snapshot` also drops entries whose element has another root or no root.
- **Exceptions.** When a processor throws, the scheduler puts the current
  element back in its queue, restores the flags it cleared before the call
  (only `InheritedProperties` and `Aspect` clear before), and rethrows
  (for example `UiFrameScheduler.cs:213-218`, `:266-271`, `:473-477`). Elements of the
  snapshot not yet reached stay queued. The next frame retries them.

## Frame Integration

This system is the frame. `UIRoot.ProcessFrameCore` calls
`Scheduler.ProcessFrame(processors ?? CreatePhaseProcessors(), ...)`
(`UIRoot.cs:440-456`). The production processors are:

| Phase | Processor |
|---|---|
| `InheritedProperties` | `InheritedPropertyPropagator.PropagateFrom` (see [Property System](property-system.md)) |
| `CommandState` | `ICommandStateSource.RefreshCommandState` with `CommandRouter` (see [Input](input.md)) |
| `Aspect` | `AspectProcessor.Process` (see [Aspect](aspect.md)) |
| `Measure`, `Arrange` | `LayoutManager.CreatePhaseProcessors` (see [Layout](layout.md)) |
| `RenderCache` | `RenderQueueProcessor.Process` (see [Rendering](rendering.md)) |
| `HitTest` | `ElementInputCache.EnsureCurrent` (see [Input](input.md)) |

### FrameStats

- Element counters, one per element a phase processed: `InheritedElements`,
  `CommandStateElements`, `AspectElements`, `MeasuredElements`,
  `ArrangedElements`, `RenderedElements`, `HitTestElements`.
- Call counters, one per `UIElement.Measure` / `Arrange` call that misses the
  element's layout cache, including recursive calls made by panels:
  `MeasureCalls`, `ArrangeCalls` (`UIElement.cs:839`, `:915`). One scheduled
  element can produce many calls.
- `NoWorkFrames`, `ReusedCaches`, the `Motion*` counters and the `Relay*`
  counters (filled by `UIRoot.BeginUpdate`).

### FrameBudget

`FrameBudget` exists but is not enforced. `ProcessFrame` replaces `default` with
`FrameBudget.ProcessAll` (`UiFrameScheduler.cs:80`) and never reads the budget
again. `MaxWorkItems` is not read anywhere. `new FrameBudget(1)` processes all
queued work.

## Invariants

- A queue holds an element once; a second enqueue keeps one entry. Test:
  `tests/Cerneala.Tests/UI/Invalidation/ElementQueueContractTests.cs:SimpleWrappersShareDeduplicationOrderingAndReenqueueContract`,
  `ElementWorkQueueTests.cs:DeduplicatesByReferenceAndKeepsEqualInstancesDistinct`.
- A snapshot follows tree preorder, not enqueue order. Test:
  `ElementQueueContractTests.cs:SimpleWrappersShareDeduplicationOrderingAndReenqueueContract`
  (child enqueued first, snapshot is `[parent, child]`).
- The order index is built once per `TreeVersion` for all queues, and a tree
  change rebuilds it once with the new order. Test:
  `ElementQueueOrderIndexTests.cs:QueuesShareOneIndexBuildForSameTreeVersion`,
  `ElementQueueOrderIndexTests.cs:TreeMutationRebuildsOnceAndReflectsNewVisualOrder`
  (covers a move; insert and reparent are netestat).
- A detached subtree never appears in any queue snapshot. Test:
  `ElementQueueContractTests.cs:DetachActivelyRemovesWholeSubtreeFromEveryQueue`.
  The test reads through `Snapshot`, which also drops stale entries, so it
  passes with either mechanism; that `RemovePendingWork` alone clears the
  queues is netestat.
- Same-phase work added during `Measure` waits for the next frame. Test:
  `FrameSchedulerStabilityTests.cs:SamePhaseWorkQueuedDuringMeasureRunsOnLaterFrame`.
  Same for `Aspect`: `UiFrameSchedulerTests.cs:AspectCallbackCanRequeueTheSameElementForTheNextFrame`.
- Arrange work added during `Measure` runs in the same frame. Test:
  `QueueSchedulerContractTests.cs:ArrangeQueuedDuringMeasureRunsInSameFrame`.
- Inherited work added during `InheritedProperties` runs in the same phase:
  netestat.
- A failing processor keeps the current and the unprocessed snapshot entries
  queued, and keeps the dirty flag. Test:
  `QueueSchedulerContractTests.cs:ExceptionKeepsCurrentAndUnprocessedSnapshotEntries`,
  `UiFrameSchedulerTests.cs:FailedPhaseKeepsDirtyFlagsAndQueuedWork` (render),
  `UiFrameSchedulerTests.cs:FailedAspectPhaseKeepsDirtyFlagsAndQueuedWork`.
  For `InheritedProperties` see
  `tests/Cerneala.Tests/UI/Core/InheritedPropertyTreePropagationTests.cs:PropagationFailureKeepsSubtreeRetryable`.
  `CommandState`, `Measure`, `Arrange` and `HitTest` failures: netestat.
- An unchanged second frame does no measure, arrange, render or hit-test work
  and counts one no-work frame. Test:
  `tests/Cerneala.Tests/UI/Hosting/UiHostFrameContractTests.cs:SecondUnchangedFrameReportsNoRetainedWork`,
  `UiFrameSchedulerTests.cs:SchedulerNoOpsWhenNothingIsDirty`.
- Phases run in the order `Measure`, `Arrange`, `RenderCache`, `HitTest`. Test:
  `UiFrameSchedulerTests.cs:ProcessesPhasesInOrder`. `Aspect` before layout:
  `UiFrameSchedulerTests.cs:ProcessesAspectBeforeLayoutAndRender`.
  `InheritedProperties` before `Aspect`:
  `UiFrameSchedulerTests.cs:ProcessesInheritedPropertiesBeforeAspect`.
  `CommandState` before `Aspect` and the second `InheritedProperties` run:
  netestat.
- A command change refreshes command state once, and an unchanged next frame
  does not. Test:
  `tests/Cerneala.Tests/Input/CommandStateSchedulerTests.cs:CommandPropertyChangeQueuesSingleCommandStateRefresh`.
- `FrameBudget` does not limit work. Test:
  `UiFrameSchedulerTests.cs:MvpProcessesAllQueuedWorkEvenWithBudget`.
- `FrameStats` keeps element counts and call counts apart. Test:
  `FrameStatsTests.cs:CountsActualLayoutCallsSeparatelyFromQueuedElements`
  (counter level only; a scheduler frame where the two differ is netestat).

Test paths without a folder are in `tests/Cerneala.Tests/UI/Invalidation/`.

## Diagnostic

`root.Detective.Invalidation` is the root's `InvalidationTrace`. It records only
when the root was created with an enabled trace; the default `UIRoot`
constructor uses `InvalidationTrace.Disabled` (`UIRoot.cs:31-47`). Entries cover
requests (`UIRoot.cs:359`), propagation and queueing (`DirtyPropagation.cs`),
phases, clears and phase summaries (`UiFrameScheduler.cs`). `FrameStats` is
returned by every frame and is captured by `Detective.Capture`.

## Known Limitations

- `FrameBudget` is accepted but ignored (see above).
- `LayoutQueueEntryKind` behavior (`Propagated` and `Subtree` entries skipped in
  incremental passes 0 and 2) has no direct test; tests check counts only.
