# Relay implementation notes

## Etapa 0 baseline

- `FileTree.md` regenerated on 2026-07-14 before repository inspection.
- Roslyn index: 1,936 documents, 27,843 symbols, 114,062 references, 0 index warnings.
- Baseline suite: 1,769 runtime tests and 158 source-generator tests passed.
- The first baseline attempt was blocked by a stale persistent RoslynRepoIndexer process holding `Cerneala.SourceGen.dll`; terminating that process and repeating the same command produced the green baseline above.
- The focused RED run compiled 12 deterministic tests: 11 failed for the expected missing Relay contract, unguarded attached-property/Aspect mutation, and missing standalone `MotionGraph` constructor; the existing tree-mutation guard test already passed. No test uses `Thread.Sleep`.

## Thread ownership and frame order inventory

- `UIRoot` is constructed and attached on the caller thread, but currently owns no common thread-access authority.
- `MotionSystem` creates its own `MotionThreadGuard` from `Environment.CurrentManagedThreadId`.
- `MotionGraph` standalone construction requires a caller-created `MotionThreadGuard`; `ManualMotionTimeline` creates one internally.
- `WindowApplicationRuntime` separately captures `ownerThreadId` and protects its public window operations with its private `VerifyAccess`.
- `UiHost` and `MonoGameUiHost` currently perform no owner-thread verification.
- `UiHost.UpdateCore` currently applies viewport/initial/time-sensitive invalidations, runs the pre-input scheduler gate, dispatches input, runs the post-input scheduler gate, commits retained render data, then publishes cursor/frame state.
- `UIRoot.ProcessFrame` is called independently by both scheduler gates, so placing Relay drain directly in the existing method would allow two drains in one host update.
- `MonoGameUiHost.Update` calls `GeneratedWindowApplication.PumpHosted` once before delegating to the primary `UiHost.Update`.
- `WindowApplicationRuntime.PumpOnce` wakes for render requests, scheduler work, active motion, or pointer repeat; no external-work backlog participates yet.
- Aspect registry, environment, invalidation, engine, and processor have no shared owner-thread check. Root-owned Aspect operations can currently mutate retained state from a worker.

## Motion API and reference inventory

Roslyn symbol IDs and definitions:

- `T:Cerneala.UI.Motion.Core.MotionThreadGuard` at `UI/Motion/Core/MotionThreadGuard.cs:3`.
- `P:Cerneala.UI.Motion.Core.MotionSystem.ThreadGuard` at `UI/Motion/Core/MotionSystem.cs:51`.
- `M:Cerneala.UI.Motion.Core.MotionGraph.#ctor(Cerneala.UI.Motion.Core.MotionThreadGuard)` at `UI/Motion/Core/MotionGraph.cs:22`.
- `M:Cerneala.UI.Motion.Core.MotionGraph.#ctor(Cerneala.UI.Motion.Core.MotionThreadGuard,Cerneala.UI.Motion.Interpolation.ValueMixerRegistry,Cerneala.UI.Motion.Core.ReducedMotionPolicy,Cerneala.UI.Motion.Diagnostics.MotionDiagnostics)` at `UI/Motion/Core/MotionGraph.cs:27`.

Direct `MotionThreadGuard` references exist in:

- `UI/Motion/Core/ManualMotionTimeline.cs`;
- `UI/Motion/Core/MotionGraph.cs`;
- `UI/Motion/Core/MotionSystem.cs`;
- `tests/Cerneala.Tests/UI/Motion/Core/MotionValueTests.cs`.

`ThreadGuard.VerifyAccess` call sites exist in:

- `UI/Motion/Core/MotionFrameCoordinator.cs`;
- `UI/Motion/Core/MotionGraph.cs`;
- `UI/Motion/Core/MotionSystem.cs`;
- `UI/Motion/Layout/LayoutMotionCoordinator.cs`;
- `UI/Motion/Presence/PresenceCoordinator.cs`;
- `UI/Motion/Properties/MotionPropertyBinding{T}.cs`;
- `UI/Motion/Transactions/MotionTransactionContext.cs`.

## Documentation inventory

- Manifest entry: `docs-site/documentation/manifest.json:2888` through `:2890`.
- Dedicated page: `docs-site/documentation/classes/Cerneala.UI.Motion.Core.MotionThreadGuard.md`.
- Constructor or guard examples/contract text: `DerivedMotionValue_T_`, `ManualMotionTimeline`, `MotionCompletedEventArgs`, `MotionFrame`, `MotionGraph`, `MotionHandle`, `MotionNode`, `MotionSystem`, `MotionValue_T_`, `MotionValue_T_.ValueNode`, `MotionValue`, `MotionValueChanged_T_`, and `MotionTransactionContext` class pages.
- Historical references under `docs/superpowers/plans/2026-07-07-modern-motion-system.md` are implementation history and are not live API documentation.

## Etapa 1 verification

- Added the root-owned `UiRelay`, strict `UiRelayOptions`, an atomic MPSC pending count, snapshot/budget drain semantics, `ExecutionContext` flow, race-safe cancellation, and async-first completion propagation.
- `UIRoot` constructs `Relay` before the remaining root-owned services and therefore captures the root construction thread as the UI owner.
- The focused core suite passed 17 tests, then passed three repeated runs with the same result. The runtime suite excluding the intentional stage-zero RED specification passed 1,786 tests.
- Concurrency coverage uses deterministic barriers rather than timing sleeps and checks per-producer FIFO, exactly-once execution, end-of-drain enqueue visibility, self-reposting deferral, and the default 1,024 callback budget.
- The public surface has no Relay constructor, blocking invoke, owned thread, or nested pump. Relay enqueue and pending reads do not inspect the retained tree.
- API documentation now includes `UiRelay`, `UiRelayOptions`, and the updated `UIRoot.Relay` ownership contract; the documentation manifest resolves both new class pages.

## Etapa 2 verification

- Added the internal `UiRelaySynchronizationContext`: `Post` routes through Relay, while `Send` runs only on the owner thread and directs off-thread callers to `InvokeAsync`.
- Relay drain enters an idempotent, nestable context scope and restores the exact previous context through `Dispose`, including the `AggregateException` path.
- `Task.Yield` continuations return to a later Relay drain, and two roots constructed on one thread retain separate continuation queues.
- Ambient `AsyncLocal`, Romanian culture, and `Activity` tracing state survive `Post`, `InvokeAsync`, and async continuations.
- A stage-one cancellation test exposed its now-invalid assumption that an async continuation would escape to the thread pool. The test was corrected to perform the required later drain; no owner-thread `.Wait()` or `.Result` was introduced.
- The focused Relay suite passed 25 tests and three repeated runs; the runtime suite excluding the intentional stage-zero RED specification passed 1,794 tests.

## Etapa 3 verification

- Added one root update gate that verifies Relay ownership, installs the root-specific synchronization context, drains one stable snapshot, records dispatch statistics, and restores the previous context.
- `UIRoot.ProcessFrame` owns the public drain path; `UiHost` reuses the internal frame core for both scheduler gates without a second drain. Relay invalidations are processed pre-input in the same update, while drain and input reposts wait for the next update.
- `UiHost` and `MonoGameUiHost` expose the root-owned Relay by reference. Update, draw, and root replacement verify access before retained mutations or backend calls; a replacement root from another owner thread is rejected without changing the host.
- The Windows wake predicate now includes Relay backlog. An otherwise idle fake standalone window rendered and executed its queued callback on the next `PumpOnce`, with runtime and root ownership verified during context creation.
- `FrameStats` now records snapshot, dequeue, execution, cancellation, fault, deferral, and backlog counters. Relay dequeue participates in `HasWork`; an idle pending check allocates zero bytes and does not start the scheduler.
- The hosting-focused suite passed 94 tests and three repeated runs. The full runtime suite excluding the intentional stage-zero RED specification passed 1,806 tests, including existing scheduler/input/render ordering and idle-frame contracts.
- Updated the `UIRoot`, `UiHost`, `MonoGameUiHost`, `UiRelay`, and `FrameStats` API pages for the new public surface and update ordering.

## Etapa 4 verification

- Added the `UiObject.VerifyMutationAccess` hook before every typed and untyped property-store mutation. `UIElement` delegates attached access to `Root.Relay`; detached elements still accept configuration on any thread and adopt root ownership only when attached.
- `UIElementCollection`, `ElementLifecycle`, root resource/platform/image/viewport/theme methods, and root invalidation now fail before tree, version, dirty-state, or queue mutation when called off-thread.
- Added the minimal internal `IUiThreadAccess` contract. `UiRelay` implements it, while standalone Motion and Aspect objects use an internal captured-thread implementation without exposing another public guard.
- `MotionSystem`, its coordinators, bindings, transactions, and root-owned `MotionGraph` now delegate to the root Relay. Standalone graphs and `ManualMotionTimeline` capture their construction thread internally.
- Deleted the legacy motion guard source, public `MotionSystem` property, guard-taking graph constructors, API page, and manifest entry. Roslyn searches found no C# symbol references or live API-documentation references; a compiled-surface test also verifies their absence.
- Aspect registry, environment, engine, invalidation, and processor mutations now share either their standalone construction thread or the owning root Relay. Tests cover owner and worker calls for registration, environment sets, tracking/recompute/untracking, apply/clear, and root processor operations.
- Public root mutation audit: direct `UIRoot` resource, platform, image, viewport, theme, invalidation, frame, host, and Relay entry points are guarded. Remaining low-level surfaces are deliberately UI-thread-only through their owning host/scheduler contract: direct `Measure`/`Arrange`, `GetSemanticsTree` cache refresh, mutable element resources/handler/binding collections, exposed retained queues/caches, and direct renderer/layout subsystem calls. They are not auto-marshaled and callers must enter through the host or `UIRoot.Relay`.
- Focused Motion/Aspect/affinity coverage passed 248 tests, all 12 stage-zero contract tests passed, and the full runtime suite passed 1,826 tests. No locks were added to layout, render, or the property store.

## Etapa 8 verification

- The public API audit confirmed the intentional Relay additions on `UIRoot`, `UiHost`, `MonoGameUiHost`, `FrameStats`, `BindingOperations`, and the new `UiRelay`/`UiRelayOptions` types, together with the intentional removal of the legacy Motion guard surface. The audit caught and corrected the host Relay convenience properties to the approved nullable contract.
- API documentation uses `docs-site/documentation/classes/` as its single source of truth. The 859-entry manifest has no missing files, missing sources, duplicate names, or duplicate files; it contains the two Relay pages and no legacy Motion guard entry.
- Conceptual hosting and markup-binding documentation now describes Relay ordering, coalesced CLR notifications, coherent source publication, direct UI mutation rejection, explicit collection dispatch, cancellation, exceptions, and synchronous-wait deadlocks.
- `dotnet format`, the complete solution build, 1,844 runtime tests, and 158 source-generator tests passed. The 77 Relay-targeted tests passed, and the three 100,000-item stress/allocation tests passed three consecutive runs without sleeps.
- The final Release benchmark run executed all 13 Relay cases and archived the raw BenchmarkDotNet reports, environment, allocations, Gen0 observations, throughput, baseline comparison, and interpretation under `benchmarks/results/2026-07-14-relay/`.
- `FileTree.md` was regenerated. The final Roslyn index contains 1,995 documents, 28,964 symbols, and 118,213 references with zero warnings; `doctor` and `status` report a valid index with zero dirty files.
