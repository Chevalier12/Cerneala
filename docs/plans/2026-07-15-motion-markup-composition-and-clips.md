# Plan: composition, MotionClip and execution handles

> Date: 2026-07-15
> Status: completed
> Dependency: `docs/plans/2026-07-15-motion-markup-foundation.md`
> Purpose: we add explicit composition, reusable recipes and cancellation per Aspect without inventing a `MotionClip` runtime.

## 1. Baseline and real constraint

The runtime has `MotionGroup.Parallel(MotionHandle[])`, `MotionSequence.Start(Func<MotionHandle>[])` and `MotionGroupHandle`. These APIs cannot directly represent an arbitrary tree in which group handles are children of other groups. The generator must have its own execution adapter, with a unified contract over leaf and group handles, without claiming that `MotionGroupHandle` has `Complete()` public or cancellation modes.

## 2. The proposed architecture

- `MarkupMotionExecution` from the bridge `GeneratedMarkup` unifies `Completion`, `Cancel` and terminal state for the generated code, adapting `MotionHandle` and `MotionGroupHandle`.
- `@parallel` and `@sequence` compose this adapter; do not change the general Motion API if the generator-owned adapter is sufficient.
- `MotionClip` is compiled as a factory/recipe typed in generated code. The resource declaration is immutable and does not have subscriptions or handles.
- Each `@run` creates a new execution. `@handle` is a per-session Aspect slot, not a global name.

## 3. Implementation stages

### Stage 0 - RED for composition

- [x] Add RED tests for `@parallel`, `@sequence` and nesting in both directions.
- [x] Add RED tests for completion ordering, cancel in the middle of the sequence and zero-step/one-step edge cases.
- [x] Add a test that demonstrates that the current runtime APIs cannot be forced by casts or polling; use the adapter explicitly.
- [x] Add lifecycle tests: detach cancels active children and does not start future steps of a sequence.
- [x] Reindex the solution.

**Gate Stage 0**

- [x] The execution model has only one semantic for leaf/group and does not expose non-existent operations like generic `Complete`.

### Stage 1 - Execution tree

- [x] Extend the AST with a recursive `execution-body` for `@animate`, `@parallel` and `@sequence`.
- [x] Requires at least one child for groups and precise diagnostics for siblings placed without explicit composition.
- [x] Implements the runtime adapter with idempotent cancel, completion exactly once and without continuations that keep the session alive after detach.
- [x] Issue in parallel so that completion waits for all children; emits sequence so that the next child starts only after natural completion.
- [x] Propagate cancellation without inventing selectable cancel behavior for `MotionGroupHandle`.
- [x] Reindex the solution.

**Gate stage 1**

- [x] Nested trees work and clean up deterministically.
- [x] Existing `MotionGroupTests` and all Motion core tests remain GREEN.

### Stage 2 - MotionClip resources

- [x] Parse `<MotionClip Name TargetType>` in resource scopes and ask for exactly one top-level execution body.
- [x] Rejects `@when`, `@on`, `@run` and the second body inside the clip.
- [x] Resolve target properties and `$part` for each application/run site, with assignability to `TargetType`.
- [x] Issue the recipe as factory without runtime class `MotionClip`, without subscriptions and without shared state.
- [x] Implements `@run $Clip` as execution leaf only in Aspect.
- [x] Add diagnostics for missing clip, wrong target, recursive invocation and direct assignment on control.
- [x] Reindex the solution.

**Gate stage 2**

- [x] Two instances and two simultaneous runs of the same clip do not share handles or mutable values.
- [x] Resource lookup is solved at build, not by dictionary lookup on each run.

### Stage 3 - Typed parameters

- [x] Parse `@parameter Name: Type = default` only at the beginning of a MotionClip.
- [x] Limits types to values/specs that the resolver can statically validate; rejects duplicates, incompatible defaults and unusable parameters.
- [x] Issue immutable parameters per execution and validate named arguments, required arguments and duplicate arguments.
- [x] Allows parameters in values, specs, counts, ranges and options only where the resulting type remains known.
- [x] Add tests for spec parameter XML-safe `MotionSpec[float]`, numeric parameter, default and diagnostics.
- [x] Reindex the solution.

**Gate stage 3**

- [x] No parameter becomes `object` or dynamic in the generated code.

### Stage 4 - Handles and cancellation

- [x] Parse `@handle Name`, `@run $Clip as Name` and `@cancel Name` in Aspect only.
- [x] Creates the slots per session; a new `@run ... as` cancels the previous execution before replacement.
- [x] Cancels all slots at detach and removes references to finished execution.
- [x] Issue diagnostics for undeclared handles, duplicates, use-before-declaration and `@cancel` in MotionClip.
- [x] Add stress test with repeated restart/cancel and check Motion graph + stabilized memory after GC.
- [x] Reindex the solution.

**Gate Stage 4**

- [x] Handles do not come out of the Aspect instance and there is no generic `@complete`.

## 4. Verification and definition ready

- [x] Runs targeted sourcegen and runtime Motion suites.
- [x] Inspect the generated code for a nested parameterized clip and confirm new factory per run.
- [x] Runs `dotnet test .\Cerneala.slnx`, `git diff --check` and the final reindex.
- [x] Composition nested, MotionClip single-body, parameters and handles exactly respect the grammar of the proposal.
- [x] API docs are updated for any new public bridge.