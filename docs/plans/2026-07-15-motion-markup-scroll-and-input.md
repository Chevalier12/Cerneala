# Plan: Scroll timelines, Drag and Gesture from Aspect markup

> Date: 2026-07-15
> Status: completed
> Dependency: `docs/plans/2026-07-15-motion-markup-foundation.md`
> Goal: link semantic input and scroll progress to Motion without `$event`, unnecessary polling or subscription leaks.

## 1. Baselines and relevant defects

`ScrollTimeline` produces vertical/horizontal normalized progress, but requires `Update()`. `ScrollMotionBinding<T>` subscribes to progress in the constructor and keeps listeners without a unbind/dispose contract. `DragMotionController` keeps two subscriptions, but does not implement disposal. These are real lifecycle bugs that block repeatable markup wiring.

## 2. Implementation stages

### Stage 0 - RED and runtime repairs

- [x] Add RED tests for disposal `ScrollMotionBinding`: after unbind/detach, progress no longer writes the property and listener count does not increase on reattach.
- [x] Introduce an idempotent unbind/dispose contract for scroll binding and keep the subscription in progress for release.
- [x] Add RED tests for `DragMotionController` disposal and reattach; confirm that the old `DragX/DragY` no longer write the element.
- [x] Fix the Drag controller to hold and release subscriptions and active handles without changing the Begin/Move/End semantics.
- [x] Checks if `ScrollTimeline` requires disposal for graph values/event wiring and implements the minimum real ownership. (It does not require its own disposal: idle values are not graph work; bindings own and release the only event wiring.)
- [x] Updates all affected public API pages and the manifest if applicable. (The manifest does not require change: no pages have been added or renamed.)
- [x] Reindex the solution.

**Gate Stage 0**

- [x] runtime APIs can be created/destroyed 100 times without listeners or residual graph work.

### Stage 1 - `@scroll`

- [x] Parse source, axis and float range assignments in the `@scroll` declaration.
- [x] Resolve source to an attached `ScrollViewer` and targets to animable `float` properties.
- [x] Generates a single timeline per statement/session, an initial update and updates from the relevant ScrollChanged event; without polling per frame when the offset does not change.
- [x] Map ranges exclusively through `ScrollTimelineProgress.Map(from,to)` and `AllowLayout()` only when `allowLayout=true` is explicit.
- [x] Release event subscription, bindings and timeline at the detachment.
- [x] Rejects pixel ranges, easing, input subranges, keyframe scroll and non-float targets.
- [x] Add tests for vertical/horizontal, clamp, zero extent, opt-in layout and detach.
- [x] Reindex the solution.

**Gate stage 1**
- [x] Scroll render-only does not produce measure/arrange and does not leave the frame requester active when the scroll is idle.

### Stage 2 - `@drag`

- [x] Parse restricted `@drag with spec` without event variables or nonexistent options.
- [x] Generates routed pointer subscriptions for begin/move/end/capture-lost and internally translates args in controller calls.
- [x] Creates the controller only after attach and disposes it upon detach; capture state does not survive the session.
- [x] Accurately maps both translation axes, fixed velocity projection and settle/capture-lost behavior from runtime.
- [x] Rejects axis, bounds, resistance, snapping, separate source/target and Decay release.
- [x] Add tests with input routed real, capture loss, detach mid-drag and reattach.
- [x] Reindex the solution.

**Gate stage 2**

- [x] A single pointer event produces a single update regardless of the number of previous attach/detach cycles.

### Stage 3 - `@gesture press`

- [x] Parse exclusively `@gesture press with spec`.
- [x] Generate pressed/released/capture-lost wiring to `GestureMotionController`, without `$event` in the language.
- [x] Keep runtime endpoints 0.97 and 1 and reject pinch/rotate/custom scale endpoints.
- [x] Add tests for press/release, rapid retarget, detach pressed and reduced motion.
- [x] Reindex the solution.

**Gate stage 3**

- [x] Gesture markup is only an adapter over the existing semantic controller, not a second gesture recognizer.

## 3. Verification and definition ready

- [x] Runs the targeted Motion Input, ScrollViewer and sourcegen Motion tests.
- [x] Run stress click/drag/scroll + attach/detach and check stabilized memory and listener counts. (100 cycles per adapter; handlers remain unique and graph work returns to zero.)
- [x] Runs `dotnet test .\Cerneala.slnx`, `git diff --check` and the final reindex.
- [x] Scroll, Drag and Gesture work without unnecessary polling, `$event` or leaks.