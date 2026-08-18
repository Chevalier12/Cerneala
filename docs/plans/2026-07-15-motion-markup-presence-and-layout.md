# Plan: Presence and Layout Motion from Aspect markup

> Date: 2026-07-15
> Status: completed
> Dependency: `docs/plans/2026-07-15-motion-markup-foundation.md`
> Purpose: we configure the existing coordinators through Aspect, with timing and lifecycle identical to the current runtime API.

## 1. Baselines and relevant defects

`PresenceOptions.FadeAndScale` accepts float specs for enter/exit and fixed endpoints. `LayoutMotionOptions.Spring` accepts a `MotionSpec<Transform>` for correction. Presence must be set before attach.

The audit identified a real runtime risk: `PresenceCoordinator.MarkAttached` creates subscriptions for opacity/scale without retaining them for disposal. The plan is not allowed to cover this by generated cleanup impossible; the runtime must be repaired RED/GREEN before the Presence markup.

## 2. Implementation stages

### Stage 0 - RED for lifecycle runtime

- [x] Add RED test in `PresenceCoordinatorTests` for repeated attach/detach/reattach and demonstrate that subscriptions/graph values of enter do not accumulate.
- [x] Fix `PresenceCoordinator` so that enter handles and subscriptions have an owner, to be canceled/released at detach, replacement and exit handoff.
- [x] Check re-add during exit and coexistence with layout correction.
- [x] Updates API docs if any public member changes and reindexes the solution. (It wasn't necessary: the public API remained unchanged.)

**Gate Stage 0**

- [x] Presence runtime is stable without markup after stress attach/detach.

### Stage 1 - `@presence`

- [x] Extends Aspect AST with exactly one declaration `@presence` and fields `enter`, `exit`, `excludeInputWhileExiting`.
- [x] Specializes enter/exit at `MotionSpec<float>` and maps exclusively to `PresenceOptions.FadeAndScale`.
- [x] Issue the Presence assignment before the element enters the retained tree.
- [x] Rejects custom endpoints, custom bodies, initial mode and Presence retroactively applied to an already attached element.
- [x] Adds sourcegen and runtime tests for enter, exit, input exclusion, removal once and reduced motion.
- [x] Reindex the solution.

**Gate stage 1**

- [x] Markup Presence produces the same state machine as the runtime API and does not have a second copy of the lifecycle.

### Stage 2 - `@layout`

- [x] Parse `@layout id expression with spec` as a unique Aspect-owned declaration.
- [x] Resolve the ID through the existing reactive grammar and specialize the spec to `MotionSpec<Transform>`.
- [x] Issue `LayoutMotionId` and `LayoutMotionOptions` before layout/attach, using the existing coordinator for snapshots and correction.
- [x] Add tests for layout rect change, mid-flight retarget, reparent with the same element and detach cleanup.
- [x] Add idle-frame assertions: correction ticks do not enqueue measure/arrange.
- [x] Reject position/size modes, crossfade, shared element between distinct controls and custom layout sequences.
- [x] Reindex the solution.

**Gate stage 2**

- [x] Layout markup only produces render correction and returns to identity without layout storm.

## 3. Verification and definition ready

- [x] Run targeted Presence/Layout and sourcegen Motion tests.
- [x] Run stress attach/detach/reparent with diagnostics counters.
- [x] Runs `dotnet test .\Cerneala.slnx`, `git diff --check` and the final reindex.
- [x] Presence and Layout are declarative, coordinator-owned and without the crazy extensions excluded from the proposal.