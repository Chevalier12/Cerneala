# Plan: keyframes, repeat, ping-pong, stagger and advanced specs

> Date: 2026-07-15
> Status: completed
> Dependency: `docs/plans/2026-07-15-motion-markup-foundation.md`, `docs/plans/2026-07-15-motion-markup-composition-and-clips.md`
> Purpose: we expose only timeline/spec semantics actually supported by the runtime and reject combinations that would deceive the user.

## 1. Contract

`KeyframesSpec<T>` requests frames at offset 0 and 1 and advances automatically for a finite duration. It has no seek, reverse or external progress. Repeat/PingPong wraps only `TweenSpec<T>`. `MotionStagger` calculates exclusively `offset * index`.

Decay remains an explicit problem: `DecaySpec<T>` uses velocity and ignores the destination `to` received by the sampler. A form `@animate` that displays a false `@to` will not be entered. The Decay declarative contract must be closed through a documented decision and tests before issuance.

**Decay decision:** the Decay markup remains deferred. We do not accept either resource `Decay` or inline constructor in this vertical, because all available property executions require `@to`, and `DecaySpec<T>` ignores that endpoint. A future syntax must explicitly start from the current visual value and a typed velocity, without `@to`, after the runtime provides a dedicated execution contract.

## 2. Implementation stages

### Stage 0 - Runtime characterization and RED diagnostics

- [x] Adds/completes runtime tests for boundaries Keyframes, gap retention, duplicate offsets, Hold, StepEasing and completion value.
- [x] Keep the Repeat/PingPong tests for cycle count and final value even/odd as a safety net.
- [x] Add sourcegen RED tests for invalid ranges, overlap on the same property, legal overlap on different properties, Spring/Decay in keyframes and illegal nested groups.
- [x] Add RED tests for Repeat/PingPong restrictions to Tween and Stagger to a single Tween `@animate`.
- [x] Write in this plan, before implementing Decay markup, the accepted form and the reason why it does not ask for an ignored endpoint; if the decision does not exist, keep Decay markup deferred. (Decay markup remains deferred: the runtime has no execution without an endpoint.)
- [x] Reindex the solution.

**Gate Stage 0**

- [x] Each construct can be downgraded to a real runtime API without decorative field or invented behavior.

### Stage 1 - Keyframes timeline

- [x] Parses `@keyframes duration` with exclusively ranged children `@animate start%..end%`.
- [x] Group the segments per target property and build a single `KeyframesSpec<T>` per property.
- [x] Inserts synthetic frames at 0/1 and at the edges of the gaps so that the runtime retains the last value exactly as the proposal says.
- [x] Rejects empty, inverted, out-of-range ranges and overlaps on the same target property; allow common boundary.
- [x] Allows Tween easing and `Step(...)`; rejects Spring, Decay, Repeat and PingPong in ranged children. (`Step(...)` is completely closed in stage 2.)
- [x] Emits the timeline as an execution body compatible with composition and MotionClip.
- [x] Reindex the solution.

**Gate stage 1**

- [x] The timelines in the proposal have exact values at 0%, boundaries, gaps and 100% under `ManualMotionTimeline`/test clock.

### Stage 2 - Hold and steps

- [x] Maps `hold` to `MotionKeyframe<T>.Hold` on the correct segment, without confusing it with `holdOnComplete`.
- [x] Map `Step(count, JumpStart|JumpEnd|JumpBoth|JumpNone)` to `StepEasing` and validate count/options at build.
- [x] Add tests that differentiate sampling hold from persistence after completion.
- [x] Add diagnostics for steps/hold outside keyframes.
- [x] Reindex the solution.

**Gate stage 2**

- [x] `hold` only changes the sampling of the segment; `holdOnComplete` remains the only control of value-source persistence.

### Stage 3 - Repeat and PingPong

- [x] Parse `Repeat(Tween(...), count|forever)` and `PingPong(Tween(...), cycles)` as spec constructors, not execution nodes.
- [x] Specializes wrappers to `TweenSpec<T>` and rejects Spring/Decay/clip/group arguments.
- [x] Validates positive count and finished PingPong; document reduced-motion for repeat forever.
- [x] Add generated-code + runtime tests for odd/even completion and infinite cancellation.
- [x] Reindex the solution.

**Gate stage 3**

- [x] Even PingPong ends at `@from`, odd at `@to`, including through property binding.

### Stage 4 - Stagger

- [x] Parse restricted `@stagger target ... each ...` with exactly one Tween-based `@animate`.
- [x] Resolves the static collection and item type, takes a snapshot at execution start and applies `WithDelay(offset * index)`.
- [x] Rejects reverse/center ordering, Spring, arbitrary sequence and mutation-driven rescheduling.
- [x] Add tests for empty collection, snapshot mutation, cancellation and cleanup.
- [x] Reindex the solution.

**Gate Stage 4**

- [x] Stagger does not introduce a parallel scheduler and does not enumerate the collection on each frame.

### Stage 5 - Spec options and Decay gate

- [x] Parse Tween `Delay`, `FillMode` and Spring `RestSpeed`, `RestDelta`, `VelocityMode`, keeping in mind that property retarget does not use sampler retarget today for velocity preservation.
- [x] Validate Decay `ValueType`, typed `InitialVelocity`, `Deceleration`, paired bounds, comparability and Bounce spec type. (Not applicable after Gate 0: the entire form is rejected before the options are validated, not partially accepted.)
- [x] Implements Decay execution only if Gate stage 0 has established a syntax without false `@to`; otherwise, it documents the declaration/execution as deferred and does not accept unnecessary resources in the markup.
- [x] Updates the proposal if the Decay decision changes the grammar, in the same change with tests.
- [x] Reindex the solution.

**Gate Stage 5**

- [x] No option accepted by the parser is silently ignored by the generated code.

## 3. Verification and definition ready

- [x] Run the Specs/Core Motion and sourcegen Motion tests.
- [x] Runs `dotnet test .\Cerneala.slnx`, `git diff --check` and the final reindex.
- [x] Keyframes, Hold, Step, Repeat, PingPong and Stagger have deterministic semantics demonstrated with manual clock.
- [x] Seek/reverse/scrubbing and unsupported combinations receive diagnostics, not pseudo-support.