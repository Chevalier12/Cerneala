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
     `Measure | Arrange`, so a later pass of the same phase measures it.
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
   the result when the element is not dirty and the rectangle and
   `LayoutVersion` match. When the arranged bounds change on an attached
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
- `UIElement.Arrange` reuses its result when the final rectangle and
  `LayoutVersion` equal the last arrange (`UIElement.cs:906-931`).
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
