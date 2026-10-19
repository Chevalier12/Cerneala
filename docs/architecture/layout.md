# Layout

> Code: `UI/Layout`, layout members of `UI/Elements/UIElement.cs`, measure and arrange phases of `UI/Invalidation/UiFrameScheduler.cs` · Verified at commit `0cb32300` (2026-10-10)

## Responsibility

Layout computes each element's desired size (measure) and final rectangle
(arrange), only for elements whose layout was invalidated, and reuses cached
results otherwise. It does not decide when it runs (the frame scheduler does)
and does not draw; a changed rectangle only schedules render and hit-test work.

## Components

| Type | File | Role | Owner |
|---|---|---|---|
| `LayoutManager` | `UI/Layout/LayoutManager.cs` | Measure and arrange processors for the scheduler; picks the constraint and slot for a queued element. | `UIRoot` (`UIRoot.LayoutManager`) |
| `ILayoutElement` | `UI/Layout/ILayoutElement.cs` | Measure/arrange contract; `UIElement` implements it. | — |
| `MeasureContext`, `ArrangeContext` | `UI/Layout/MeasureContext.cs`, `UI/Layout/ArrangeContext.cs` | Available size or final rectangle, plus `LayoutRounding`. | One call |
| `LayoutRounding` | `UI/Layout/LayoutRounding.cs` | Rounds to physical pixel steps; `LayoutRounding.ForScale(scale)`. | One call |
| `LayoutResult` | `UI/Layout/LayoutResult.cs` | Result of a `LayoutManager` call: `UsedMeasureCache`, `UsedArrangeCache`, `BoundsChanged`. | Caller |
| `LayoutBoundary` | `UI/Layout/LayoutBoundary.cs` | Reads and sets `UIElement.IsLayoutBoundary`. | Static |
| `LayoutQueue` | `UI/Invalidation/LayoutQueue.cs` | Measure and arrange work, see [Invalidation And Frame Scheduling](invalidation-and-frame.md). | `UIRoot` |
| `Panel`, `StackPanel`, `Canvas`, `Grid`, `VirtualizingStackPanel` | `UI/Layout/Panels/<Name>.cs` | Panels that measure and arrange their children. | Application tree |
| `IItemsVirtualizingPanel`, `VirtualizationContext`, `RealizationWindow`, `ItemsVirtualizationViewport` | `UI/Layout/Virtualization/<Name>.cs` | Virtualization contract and window math. | The virtualizing panel |
| Value types | `UI/Layout/` | `LayoutSize`, `LayoutRect`, `LayoutPoint`, `Thickness`, `Visibility`, `Orientation`, `HorizontalAlignment`, `VerticalAlignment`. | — |

## Data Flow

1. **Invalidation.** A property with `AffectsMeasure` or `AffectsArrange`
   increments the element's `LayoutVersion` and invalidates `Measure` or
   `Arrange` (`UIElement.cs:1245-1248`). `Measure` implies `Arrange` and
   `Render`. Ancestors get `Measure | Arrange` up to the first layout boundary
   (see [Invalidation And Frame Scheduling](invalidation-and-frame.md#data-flow)).
2. **Measure phase.** The production processors support incremental measure,
   so `UiFrameScheduler.ProcessMeasureCore` (`UiFrameScheduler.cs:297-376`):
   - takes the measure snapshot in reverse preorder (children before parents);
   - runs 3 passes over it. Passes 0 and 2 go children first and skip entries
     of kind `Propagated` and `Subtree`. Pass 1 goes parents first, measures
     `Subtree` entries, and clears `Propagated` entries without measuring them;
   - after measuring an element whose desired size changed, and which is not a
     layout boundary, enqueues its parent as a `Required` entry and marks it
     `Measure | Arrange`, so a later pass of the same phase measures it. It
     also invalidates the parent's measure cache, keeping its constraint, and
     clears its scheduled-measure visit
     (`UIElement.InvalidateMeasureCacheKeepingConstraint`). A measure of the
     parent that ran earlier in this phase, for example by an ancestor that
     measured it from its cache, therefore does not make the scheduler skip the
     parent's `Required` entry, and an ancestor that measures the parent with
     the same constraint runs the parent's `MeasureCore`. It also invalidates
     the parent's arrange cache (`UIElement.InvalidateArrangeCache`)
     and keeps its slot. An ancestor that arranges the parent with the same slot
     then runs the parent's `ArrangeCore`, instead of reusing the stale result and
     marking the parent as already arranged in this pass.
   - does not measure an element when an ancestor's measure leaves the child on
     the path to it unmeasured on purpose (`UIElement.SkipsChildMeasure`), as a
     `VirtualizingStackPanel` does with a child outside its realization window.
     The scheduler clears the element's entry and invalidates the measure
     caches, keeping their constraints, of the element and of its ancestors up
     to that skipped child. When the panel later measures the child, the
     measure therefore reaches the element, as the desired-size step above
     would have made it do. For example, a leaf two levels below a row outside
     the window that changes `Height` from 20 to 60 makes the row desire 60
     once the panel realizes it again. The arrange work is scheduled by that
     later measure, not here: each element whose `DesiredSize` it changes
     makes its own and its parent's arrange cache invalid and queues both as
     `Required` arrange work (see "Measure And Arrange Caches"). Until then an
     arrange of the skipped row, which reads the old desired sizes, can make
     those caches valid again. Elements between the row and that parent whose
     desired size and slot did not change keep their arrange caches; the queue
     entries arrange the element and its parent even when such an ancestor
     returns its cached result. For example, in a row
     `Border > Canvas > Border > StackPanel > A, B`, A and B swap heights 20
     and 40 outside the window. The inner `Border` and the `StackPanel` get the
     slot `0, 0, 0, 60` both while the row is skipped and after it is realized,
     so the inner `Border` reuses its arrange; the `StackPanel`'s queue entry
     still arranges A at Y 0 with height 40 and B at Y 40.
3. **Constraint.** `LayoutManager.GetAvailableSize` (`LayoutManager.cs:119-145`)
   gives the root and its direct children the viewport size. Any other element
   gets the constraint it was last measured with, because its parent owns that
   constraint. Without one it gets the parent's arranged size, then the
   viewport.
4. **Measure call.** `LayoutManager.Measure` (`LayoutManager.cs:27-45`): an
   element without the `Measure` flag returns its cached size when
   `TryUseCachedMeasure` hits. A dirty element first clears its measure cache.
   Then `UIElement.Measure` runs with `LayoutRounding.ForScale(root.Scale)`.
5. **Arrange phase.** The arrange snapshot is in preorder (parents first),
   with the same 3-pass skip rules and no parent requeue
   (`UiFrameScheduler.cs:394-460`). `LayoutManager.GetFinalRect`
   (`LayoutManager.cs:154-183`) gives the root the viewport rectangle, a
   `Canvas` child its `Canvas.GetLeft`/`GetTop` position with its desired size,
   and any other element the slot its parent last gave it.
6. **Arrange call.** `LayoutManager.Arrange` (`LayoutManager.cs:47-76`) reuses
   the result when the element is not dirty and its arrange cache is valid for
   the rectangle (`UIElement.HasValidArrangeCache`). When the arranged bounds change on an attached
   element, it invalidates `Render | HitTest` with the reason
   `"Layout bounds changed"`.

Example. A `StackPanel` holds two `Border` elements, each with `Height = 40`.
The code sets `first.Visibility = Visibility.Collapsed;`.

- The change of layout participation makes `UIElement.OnPropertyInvalidated`
  increment the panel's `LayoutVersion` and enqueue the panel as `Required`
  measure and arrange work (`UIElement.cs:1293-1318`).
- In the next frame the panel measures again: the collapsed border reports
  `LayoutSize.Zero`, so the panel's desired height goes from 80 to 40.
- The second border is arranged at Y = 0 instead of Y = 40.

## Measure And Arrange Caches

- `UIElement.TryUseCachedMeasure` (`UIElement.cs:852-892`) hits when the
  available size and `LayoutVersion` equal the last measure. An element without
  children also keeps three older constraints (four in total) and reuses any
  matching one. An element with children keeps only the last one.
- `UIElement.Measure` itself does not look at `DirtyState`; the dirty check is
  in `LayoutManager.Measure`. A panel that measures a child directly gets the
  cached size when constraint and `LayoutVersion` match.
- When a request queues `Measure` for its target, `DirtyPropagation.Propagate`
  invalidates the target's measure cache and keeps its last constraint
  (`LastMeasureAvailableSize`), which `LayoutManager.GetAvailableSize` and
  `ItemsPresenter` still read. With `Subtree`, it does the same for every
  visual descendant it queues. A parent that measures one of these
  elements with the same constraint in the same pass then runs its
  `MeasureCore`, instead of reusing the stale size and marking it as already
  measured. Ancestors queued as `Propagated` keep their measure cache.
- The same reset clears the element's scheduled-measure visit
  (`LastScheduledMeasureRoot`). The measure phase skips a queued element that
  was already measured in the current pass with its current `LayoutVersion`.
  A request raised after that measure, for example from a sibling's
  `MeasureCore`, is therefore not skipped: the element is measured again later
  in the same phase, or in the next frame when it is no longer in the phase's
  snapshot.
- `UIElement.Measure` writes the measure cache stamp and the scheduled-measure
  visit after `MeasureCore`. When the element's measure cache was invalidated
  during that measure (its own `MeasureCore` calls `Invalidate(Measure)`), it
  writes neither: the cache stays invalid and the visit stays cleared. The
  element's queue entry then measures it again later in the same measure phase,
  or in the next frame, and a parent that measures it with the same constraint
  runs its `MeasureCore`. `LayoutManager.Measure` does not write the stamp again
  after `UIElement.Measure` returns.
- `UIElement.Arrange` reuses its result when the final rectangle and
  `LayoutVersion` equal the last arrange and the desired sizes it read are
  unchanged (`UIElement.HasValidArrangeCache`).
- A `UIElement.Measure` that runs (does not return a cached size) makes the
  element's own arrange cache invalid, as WPF's `Measure` calls
  `InvalidateArrange`, also when its `DesiredSize` does not change: its
  `ArrangeCore` can read measure state other than desired sizes. For example,
  a `Grid` with two `*` rows holding children 60 and 20 high shares a measured
  height of 80 by weight (40 and 40), while measured unbounded each row keeps
  its content height (60 and 20). Both measures give a `DesiredSize` of
  100 x 80; after the unbounded measure, an arrange in the same slot
  `0, 0, 100, 80` runs `ArrangeCore` and places the second child at Y 60
  (#516). The visual parent's arrange cache still depends only on the
  element's desired size.
- An arrange reads desired sizes: the element aligns itself in its slot by its
  own, and it places its visual children by theirs. When an arrange starts, it
  records its own `DesiredSize` and starts a new count of children whose
  `DesiredSize` changed (`UIElement.ArrangeDesiredSizesAreCurrent`). A measure
  that changes a `DesiredSize` (`UIElement.SetDesiredSize`,
  `SetUnmeasuredDesiredSize`) compares it with the value the element's own and
  its parent's last arrange read. While either differs, a same-slot arrange of
  that element runs its `ArrangeCore` again, and the scheduler does not skip
  its queue entry as already arranged. This also covers a measure from inside
  the parent's `MeasureCore`, which the frame's desired-size step does not see.
  A measure that gives the same desired size, or a later measure that gives
  the old value back, keeps the parent's cache, and the element's own cache
  when that measure returns a cached size. For example, the overlay layer
  measures an `Overlay`'s content unbounded and then clamped to the space
  beside its target: content without visual children that desires 50 x 300
  with 80 px below the target desires 50 x 300 and then 50 x 80 again, in the
  layer's measure and in its arrange. Its measures return cached sizes, so it
  is not arranged again when its slot stays `20, 120, 50, 80`. The
  `ContentPresenter` around it has a child and keeps only its last
  constraint, so its measures run, and it is arranged again.
- `UIElement.SetDesiredSize` also queues the element and its visual parent as
  `Required` arrange work when the element is attached and its `DesiredSize`
  changes. Only these two caches depend on the changed value; the ancestors
  above the parent keep theirs. An ancestor whose slot and desired sizes did
  not change therefore returns its cached arrange without arranging its
  children, and the queue entry is what arranges the parent: the arrange phase
  arranges it with its last slot (`LayoutManager.GetFinalRect`), as WPF's
  `InvalidateArrange` puts an element in the layout manager's arrange queue.
  The queued arrange goes through `LayoutManager.Arrange`, so it still reuses
  the cache when the desired sizes it read have come back. The scheduler clears
  the element's `Arrange` flag before it processes a `Required` entry, so a
  flag that only a descendant's measure ancestor propagation set does not make
  `LayoutManager.Arrange` skip that cache: every request for the element's own
  arrange also invalidates its arrange cache. For example, the layer of an open
  `Overlay` whose `ScrollViewer` (500 px of content) gets the 80 px below its
  target reuses its arrange in the second frame; an `ArrangeCore` there would
  measure the `ScrollViewer` unbounded and clamped again, and the scroll bar it
  hides and shows would queue measure work for a third frame (#514). When the
  measure runs during the arrange phase, the entry is not in that phase's
  snapshot and the next frame processes it. A measure that gives the same
  desired size queues nothing.
- An attached element's `Invalidate` does not change its `LayoutVersion`. When
  the request queues `Arrange` for its target, `DirtyPropagation.Propagate`
  invalidates the target's arrange cache and keeps its slot. With `Subtree`, it
  does the same for every visual descendant it queues. A parent that arranges
  one of these elements with the same slot in the same pass then runs its
  `ArrangeCore`, instead of reusing the stale result and marking it as already
  arranged. Ancestors queued as `Propagated` keep their arrange cache.
- The same reset clears the element's scheduled-arrange visit
  (`LastScheduledArrangeRoot`). The arrange phase skips a queued element that
  was already arranged in the current pass with its current `LayoutVersion`.
  A request raised after that arrange, for example from a sibling's
  `SizeChanged` handler, is therefore not skipped: the element is arranged
  again later in the same phase, or in the next frame when it is no longer in
  the phase's snapshot.
- `UIElement.Arrange` writes the arrange cache stamp and the scheduled-arrange
  visit after `ArrangeCore` and `SizeChanged`. When the element's arrange cache
  was invalidated during that arrange (for example, its own `SizeChanged`
  handler or `ArrangeCore` calls `Invalidate(Arrange)`), it writes neither: the
  cache stays invalid and the visit stays cleared. The element's queue entry
  then arranges it again later in the same arrange phase, or in the next frame,
  and a parent that arranges it with the same slot runs its `ArrangeCore`.
  `LayoutManager.Arrange` does not write the stamp again after
  `UIElement.Arrange` returns.
- An element that does not take part in layout (`Visibility.Collapsed`)
  measures to `LayoutSize.Zero` and arranges to a zero-size rectangle at the
  slot position.

## Lifecycle And Ownership

- `LayoutManager` lives as long as its `UIRoot`. Layout state
  (`DesiredSize`, `ArrangedBounds`, `LayoutVersion`, the measure cache slots,
  `LastArrangeFinalRect`) lives on each `UIElement`.
- `UIRoot` is a layout boundary (`UIRoot.cs:107`). Any element can become one
  through `IsLayoutBoundary` or `LayoutBoundary.SetIsBoundary`; no production
  element other than `UIRoot` does.
- Expanding an element from `Collapsed` increments the `LayoutVersion` of its
  whole subtree, so stale cache slots are not reused (`UIElement.cs:1299-1305`).

## Frame Integration

- Phases `Measure` and `Arrange`, after `Aspect` and the second
  `InheritedProperties` run, before `RenderCache`.
- Queue: `LayoutQueue` (measure and arrange halves).
- Output: `Render | HitTest` invalidation for elements whose bounds changed.
  `LayoutManager.Arrange` also removes descendants from the `RenderQueue` when
  their cached commands can be reused at a translated position
  (`LayoutManager.cs:78-117`).
- Counters: `FrameStats.MeasuredElements` and `ArrangedElements` count queued
  elements; `MeasureCalls` and `ArrangeCalls` count cache-missing calls.

## Invariants

Test paths are under `tests/Cerneala.Tests/`.

- **Idle frame:** an unchanged tree does no layout work on the next frame:
  `MeasuredElements` and `ArrangedElements` are 0 and `NoWorkFrames` is 1.
  Test:
  `UI/Hosting/UiHostFrameContractTests.cs:SecondUnchangedFrameReportsNoRetainedWork`,
  `UI/Invalidation/RetainedNoWorkFrameTests.cs:UnchangedTreeDoesNotArrangeOnSecondFrame`,
  `UI/Invalidation/RetainedNoWorkFrameTests.cs:UnchangedTreeDoesNotMeasureOnSecondFrame`.
- Measuring again with the same constraint and `LayoutVersion` reuses the cache.
  Test: `UI/Layout/LayoutManagerTests.cs:MeasureCacheIsReusedForSameConstraintAndVersion`.
- Measure propagation stops at a layout boundary. Test:
  `UI/Layout/LayoutInvalidationTests.cs:LayoutBoundaryStopsMeasurePropagation`.
- An arrange that changes bounds queues render and hit-test work. Test:
  `UI/Layout/LayoutInvalidationTests.cs:ChangedArrangeBoundsSchedulesRenderAndHitTest`.
- A render-only invalidation does not run measure. Test:
  `UI/Invalidation/RetainedNoWorkFrameTests.cs:RenderOnlyInvalidationDoesNotRunMeasure`.
- A collapsed element measures to zero; collapse and restore reflow the nested
  ancestors and siblings, also when the changed element is a layout boundary.
  Test: `UI/Layout/VisibilityCombinationTests.cs:VisibilityCombinationControlsLayoutParticipation`,
  `UI/Layout/CollapsedLayoutBreakerRegressionTests.cs:CollapseAndRestoreReflowsNestedAncestors`.
- A child whose desired size changed makes its parent measure again in the
  same frame; a child with an unchanged size does not. Test:
  `UI/Layout/LayoutManagerTests.cs:DirtyMeasureBypassesParentMeasureCacheWhenChildLayoutChanges`,
  `UI/Layout/LayoutManagerTests.cs:StableChildDesiredSizeDoesNotRemeasureAncestors`,
  `UI/Layout/LayoutManagerTests.cs:DirectParentInvalidationIsNotPrunedByStableChildMeasure`.
- That parent, and each ancestor up to the first layout boundary whose desired
  size changes in turn, is measured again in the same frame also when an
  ancestor already measured it earlier in the same measure phase. Test:
  `UI/Layout/ParentRequeuedAfterAncestorMeasuredItTests.cs:ParentIsMeasuredAgainWhenChildSizeChangesAfterAnAncestorMeasuredIt`.
- That parent is arranged again in the same frame, also when a sibling's change
  makes its own parent arrange it first with the same slot. Test:
  `UI/Layout/SiblingLayoutSameFrameRegressionTests.cs:AutoColumnChildIsRearrangedWhenGridSiblingChangesInTheSameFrame`.
- An element invalidated for `Arrange` is arranged again when its parent is
  arranged in the same frame with the same slot, in both invalidation orders.
  Test: `UI/Layout/DirectArrangeInvalidationSameFrameRegressionTests.cs:DirtyChildArrangeRunsWhenParentArrangesItWithTheSameSlotInTheSameFrame`.
- After `Invalidate(Measure | Subtree)` on a parent, a child and a grandchild
  measured with the same constraint are measured again in the same frame. The
  target of such a request is measured again also when its own parent measures
  it first in the same frame. Tests:
  `UI/Layout/SubtreeMeasureInvalidationSameFrameRegressionTests.cs`.
- After `Invalidate(Arrange | Subtree)` on a parent, a child and a grandchild
  arranged with the same slot are arranged again in the same frame. Tests:
  `UI/Layout/SubtreeArrangeInvalidationSameFrameRegressionTests.cs`.
- An element that calls `Invalidate(Arrange)` during its own arrange (from
  `SizeChanged` or `ArrangeCore`) is arranged again later, also when its parent
  first arranges it again with the same slot, also when it is queued in the
  arrange snapshot (whether its own queue entry or its parent arranges it
  first), and also after `LayoutManager.Arrange`. Tests:
  `UI/Layout/SelfArrangeInvalidationDuringArrangeRegressionTests.cs`.
- An `Arrange` request raised after the element was already arranged in the
  same arrange phase is not dropped, whether a sibling's `SizeChanged` handler
  or a sibling's `ArrangeCore` raises it. Test:
  `UI/Layout/ArrangeInvalidationAfterArrangeSameFrameRegressionTests.cs:ArrangeInvalidationRaisedAfterTheElementWasArrangedInTheSamePhaseRuns`.
- A `Measure` request raised after the element was already measured in the
  same measure phase, for example from a sibling's `MeasureCore`, is not
  dropped. Test:
  `UI/Layout/MeasureInvalidationAfterMeasureSameFrameRegressionTests.cs:MeasureInvalidationRaisedAfterTheElementWasMeasuredInTheSamePhaseRuns`.
- An element that calls `Invalidate(Measure)` during its own `MeasureCore`
  while it is queued in the measure snapshot is measured again, whether or not
  its parent measures it again with the same constraint in that frame, and also
  after `LayoutManager.Measure`. Tests:
  `UI/Layout/SelfMeasureInvalidationDuringMeasureRegressionTests.cs`.
- A failed measure keeps the element dirty and queued. Test:
  `UI/Layout/LayoutManagerTests.cs:FailedMeasureKeepsDirtyFlagsAndQueue`.
  A failed arrange: netestat.
- `LayoutRounding.ForScale` rounds to physical pixel steps. Test:
  `UI/Layout/LayoutPrimitiveTests.cs:LayoutRoundingUsesPhysicalPixelStepsAtDpiScale`.
  That frame layout passes `root.Scale` to it: netestat.
- `VirtualizingStackPanel` measures only realized children. Test:
  `UI/Layout/VirtualizingStackPanelTests.cs:VirtualizingStackPanelMeasuresRealizedChildrenOnly`.
- `MeasureCalls` and `ArrangeCalls` include recursive panel calls. Test:
  `UI/Layout/LayoutDiagnosticsAccuracyTests.cs:FrameStatsCountActualRecursiveMeasureAndArrangeCalls`
  (lower bounds only).
- The 3-pass structure and the `LayoutQueueEntryKind` skip rules: netestat
  (tests check resulting counts only).

## Diagnostic

`Detective.CaptureLayout` describes an element's layout state. Layout requests,
propagation and phase work appear in the root's `InvalidationTrace` when it is
enabled. `FrameStats` gives the counters above.
