# Plan: Relay - modern UI auto-marshaling

> Date: 2026-07-14
> Status: completed
> Dependency: the core of Relay is independent; generator integration
> of binding depends on `docs/plans/2026-07-14-markup-data-bindings.md`
> Goal: introducing an async-first, deterministic and observable Relay UI,
> integrated with the frame loop Cerneala and with external reactive sources, without thread
> Secondary UI, blocking invoke or concurrent mutations of the retained tree

## 1. Summary

Cerneala today does not have a general mechanism by which a signal coming from a
worker thread to be executed safely on the `Update`/UI thread. C# Events
are synchronous, such that `INotifyPropertyChanged`, `ObservableValue<T>`,
`CanExecuteChanged`, theme changes and resource changes are running
handler on the issuing thread. If that handler writes a UI property,
invalidates an element or modifies a retained queue, the UI is reached from
wrong thread.

The target solution is a `UiRelay` owned by each `UIRoot`, with:

- multi-producer/single-consumer thread-safe queue;
- Public API async-first: `Post`, `InvokeAsync`, `CheckAccess` and `VerifyAccess`;
- drain once at the beginning of each update, before the scheduler and
  input;
- snapshot and deterministic budget, so that a callback that reposts itself
  don't eat the frame with everything with a plate;
- integration with `SynchronizationContext` only during UI execution;
- cancellation, propagation of exceptions, statistics and competition tests;
- specialized coalescing for bindings and other "re-query" type signals
  current state";
- fail-fast for direct off-thread UI mutations, because the auto-marshaling
  do not turn the retained tree into a concurrent collection.

### 1.1 Branding and vocabulary

The subsystem is called **Relay**: it takes work from any thread and hands it over
to the thread owning the root, without hiding concurrent mutations and without
to invent a second UI thread. The short formula is: **Relay moves execution,
not the data**.

| Subsystem | Role | Representative API |
| --- | --- | --- |
| Aspect | style, theme and waterfall | `AspectEngine`, `AspectRegistry` |
| Motion | storyboard and animation | `MotionSystem`, `MotionGraph` |
| Relay | handoff between threads, async continuations and auto-marshaling | `UiRelay`, `UiRelayOptions` |

Public branding uses the `Cerneala.UI.Relay` namespace, property
`UIRoot.Relay` and types `UiRelay`/`UiRelayOptions`. Familiar technical verbs
remain `Post`, `InvokeAsync`, `CheckAccess` and `VerifyAccess`; we do not expose in
parallel types or public properties called `Dispatcher`, because they are two names
for the same madness would only produce confusion.

## 2. Established decisions and assumptions
- The owning thread of a `UIRoot` is the thread the root is on
  built. Relay becomes the only source of truth for thread affinity in
  root-owned services, including Aspect and Motion.
- `UiRelay` does not create or own a thread. `UiHost.Update`,
  `MonoGameUiHost.Update`, `WindowApplicationRuntime.PumpOnce` and the calls
  direct `UIRoot.ProcessFrame` pumps the work on the existing UI thread.
- Each root has its own Relay and its own queue. There isn't one
  `UiRelay.Current` global static, because Cerneala supports more
  roots and windows.
- `Post` and `InvokeAsync` always put the work in the queue, including when they are
  called from the UI thread, for predictable FIFO order. Code already found
  the UI thread can directly call the operation if it wants immediate execution.
- There is no `Invoke`/`Send` public blocker, nested message pump or waiting
  synchronous. `.Wait()`, `.Result` and `GetAwaiter().GetResult()` on the UI thread
  wrong uses remain and must be documented as potential deadlock.
- The existing callbacks at the beginning of the drain form a stable snapshot.
  The work posted during the drain is postponed for the next update.
- The order is FIFO after the linearization of the enqueue; each producer himself
  keep order. We do not promise an arbitrary order between two threads that
  post simultaneously.
- The default budget is 1,024 callbacks per update and is configurable
  through `UiRelayOptions.MaxCallbacksPerUpdate`. The budget is numerical, no
  timer based, for tests and deterministic frames.
- The public scheduling APIs capture `ExecutionContext`, so that
  culture, `AsyncLocal` and the tracing context to follow the callback. The code of
  drain always restores the previous context.
- `InvokeAsync` captures the exception or cancellation in `Task`; an exception from
  `Post` is not swallowed. The relay processes the rest of the snapshot, then
  throw a `AggregateException` from update for fire-and-forget callbacks
  failed.
- Canceling before execution prevents the callback from being called. After one
  synchronous callback has started, the token cannot interrupt it. The async overload
  receives the token and cooperatively controls its cancellation.
- `SynchronizationContext.Send` executes inline only on the owner thread;
  off-thread throws `NotSupportedException` and indicates `InvokeAsync`. `Post`
  delegate to Relay.
- An off-thread `INotifyPropertyChanged` is not evaluated on the worker. The handler
  filter only the property name, mark an atomic version and schedule
  a reevaluation of the current state on the UI thread.
- Bindings with attached target make fast-path synchronous for notifications already
  raised on UI thread. Only off-thread notifications are coalesced and deferred.
- `UiObject.PropertyChanged` raised off-thread cannot be "fixed" after the fact:
  the UI property has already been moved. Direct mutation of an attached `UIElement`
  remains prohibited and must be stopped before writing.
- Auto-marshaling protects the UI, it does not automatically make the ViewModel thread-safe,
  collections or source objects. The source must allow a coherent reading on
  UI thread after notification.
- `ObservableList<T>` and the incremental mutations of the collections remain UI-affine
  in this plan. It is used explicitly for them
  `await root.Relay.InvokeAsync(() => items.Add(item))`; I'm not pretending
  thread-safety over a `List<T>` that doesn't have it.
- Until the implementation of the integration stage with the bindings, the plan
  `2026-07-14-markup-data-bindings.md` keeps fail-fast strict for everything
  `PropertyChanged` off-thread.

## 3. Baseline and the current problem

### 3.1 Frame loop and ownership

- `UIRoot` owns `UiFrameScheduler`, the retained queues, `MotionSystem`, the cache of
  render and root services.
- `UiHost.UpdateCore` can call `UIRoot.ProcessFrame` once before input and
  once more after input. A relay integrated naively in `ProcessFrame` could
  it drains twice in the same update and would spoil the semantics of the snapshot.
- `MonoGameUiHost.Update` pumps `GeneratedWindowApplication`, then delegates to
  `UiHost.Update`.
- `WindowApplicationRuntime.PumpOnce` renders a window only if it exists
  `RenderRequested`, work in `UiFrameScheduler`, motion active or pointer repeat.
  A Relay with backlog must be explicitly added to this wake predicate.
- `WindowApplicationRuntime` already has a `ownerThreadId` and `VerifyAccess`, and
  Motion has its own `MotionThreadGuard`; the two parallel mechanisms are
  the baseline that needs to be removed, not the architecture we are preserving.

### 3.2 Reactive sources

- `UiObject.OnPropertyChanged` invokes handlers synchronously.
- `GeneratedMarkupConditions.Subscribe` listen directly
  `UiObject.PropertyChanged` or `INotifyPropertyChanged` on the issuing thread.
- `UiPropertyBinding<T>` listen directly to `ObservableValue<T>.ValueChanged` and
  immediately write the target.
- `ObservableValue<T>` and `ObservableList<T>` are not competing collections.
- `UIRoot.OnResourceChanged`, `ThemeChangedSubscription` and
  `ButtonBase.OnCanExecuteChanged` end up in invalids or retained queues without one
  marshal general.

### 3.3 Existing reusable infrastructure

- Queue Engine 2.0 offers fast queues for retained elements, but these are
  UI-thread-only and visually ordered; they must not be forcibly reused as an MPSC queue
  of delegates.
- `FrameStats` and `InvalidationTrace` are the existing points for
  observability of a frame.
- `IElementLifecycleBehavior`, attach/detach and generation guards from markup are
the right points for canceling stale reactive callbacks.

## 4. Objectives

- [x] An operation posted from any worker thread runs exclusively on the thread
  owner of the root, at the beginning of the next eligible update.
- [x] `UiRelay.CheckAccess` and `VerifyAccess` give the same truth for
  hosting, bindings, motion and UI changes attached.
- [x] Enqueue is thread-safe and O(1) amortized, and backlog checking is not
  traverse the tree and do not allocate after warmup.
- [x] Drain is FIFO, budgeted and based on snapshot; there is no starvation
  caused by auto-repost in the same update.
- [x] The Relay job runs before the retained and input scheduler,
  and the invalidations produced are processed in the same update as the budgets
  scheduler allow.
- [x] `UiHost.Update` drains only once, even if the scheduler processes
  pre-input and post-input.
- [x] Standalone Windows wakes up for the Relay backlog even if
  there are no invalidations or motions.
- [x] `await` continuations started from UI callbacks return via Relay without
  the permanent installation of a global context.
- [x] Off-thread bursts by `PropertyChanged` produce at most a re-evaluation
  pending per active binding, without losing the last change.
- [x] Detach, template swap, root replacement and disposal do not allow a callback
  old to write to an inactive target.
- [x] Direct off-thread UI mutations are rejected before state change.
- [x] The public API has complete documentation and async examples without blocking.
- [x] There are statistics for enqueue, execute, cancel, fault, deferred and
  backlog, plus multi-producer benchmarks.

## 5. Non-objectives

- We don't create a new UI thread and we don't take over the main MonoGame loop.
- We do not do layout, render, hit testing or motion in parallel.
- We do not add priorities, delayed dispatch, timers, cron jobs or a task scheduler
  general. I can come later only on the basis of a real case.
- We do not implement a `Relay.Invoke` blocker and do not emulate nested message loops
  from old desktop frameworks.
- We do not `ObservableList<T>`, ViewModels or user collections
  magically thread-safe. Incremental collection mutations are posted explicitly
  on Relay.
- We do not automatically intercept any arbitrary event from the application. Automatic migrations
  it is limited to first-party points that touch the UI and have defined semantics
  in this plan.
- We do not change the order of retained phases from `UiFrameScheduler`; The relay is
  a pre-frame gate, not a new queue of visuals.
- We do not move an already built `UIRoot` to another thread. Such a migration would require
  a separate contract for motion, graphic backend and native resources.
- We do not guarantee atomic snapshot for a CLR path whose objects are modified
simultaneously without synchronizing the author of the ViewModel.

## 6. Proposed public contract

### 6.1 `UiRelay`

Target surface from `Cerneala.UI.Relay`:
```csharp
public sealed class UiRelay
{
    public bool CheckAccess();
    public void VerifyAccess();

    public bool HasPendingWork { get; }
    public int PendingCount { get; }

    public void Post(Action callback);

    public Task InvokeAsync(
        Action callback,
        CancellationToken cancellationToken = default);

    public Task<T> InvokeAsync<T>(
        Func<T> callback,
        CancellationToken cancellationToken = default);

    public Task InvokeAsync(
        Func<CancellationToken, Task> callback,
        CancellationToken cancellationToken = default);
}
```
Rules:

- all overloads validate `null` synchronously;
- all enqueues are FIFO and asynchronous to the caller;
- `TaskCompletionSource` uses `RunContinuationsAsynchronously`;
- `InvokeAsync(Func<CancellationToken, Task>)` starts the delegate on the UI thread,
  it does not block the drain until completion and propagates the result in the task
  returned;
- `Post` is for controlled fire-and-forget; the code that needs the result,
  canceling or handling the exception uses `InvokeAsync`;
- `PendingCount` includes unstarted work items, not already started async operations;
- public callbacks run with `ExecutionContext` captured at enqueue.

### 6.2 `UiRelayOptions`
```csharp
public sealed class UiRelayOptions
{
    public int MaxCallbacksPerUpdate { get; init; } = 1024;
}
```
- values ​​lower than or equal to zero are rejected;
- the options are copied/validated when building the root, not read mutable in
  the middle of the frame;
- the configuration enters as the last optional parameter in the constructor `UIRoot`, without
  to break existing calls.

### 6.3 Exposure through hosting

- `UIRoot.Relay` is the non-null source of truth.
- `UiHost.Relay` and `MonoGameUiHost.Relay` are nullable properties of
  convenience, because the host can exist temporarily without root.
- `SetRoot` verifies that it is called on the Relay thread of the new one
  roots; don't close the old root's Relay, because that root may be
  reused or processed directly.

### 6.4 Scoped async context

An internal `UiRelaySynchronizationContext` is only installed around:

- the drain of the Relay;
- retained processing;
- input routing and handlers called by it;
- hosting callbacks that run as part of the update.

The previous context is restored in `finally`. Two roots pumped successively on
the same thread does not mix its continuations. `SynchronizationContext.Post`
call `UiRelay.Post`; `Send` is allowed inline only when
`CheckAccess == true`, otherwise throw.

## 7. The proposed architecture

### 7.1 MPSC core and work items

`UiRelay` uses a `ConcurrentQueue<UiRelayWorkItem>` and counters
atomic for backlog. A single consumer, the UI thread, executes the drain.

Each work item contains only what the contract requires:

- callbacks and states;
- `ExecutionContext` captured;
- fire-and-forget or request/response type;
- optional source completion;
- optional token and registration;
- atomic state `Pending`, `Running`, `Completed` or `Canceled`.

We do not use `Channel<T>`, thread pool consumer or `Task.Run` for drain. The tail
it is only transport between productions and frame loop; otherwise we would build a bus
to cross the kitchen.

### 7.2 Deterministic drain

At the beginning of the update:

1. the owner thread is checked;
2. the existing pending number is captured;
3. the limit is `min(snapshotCount, MaxCallbacksPerUpdate)`;
4. at most that FIFO limit is processed;
5. canceled work items are completed as canceled without callback;
6. exceptions `InvokeAsync` complete the faulted task;
7. the exceptions `Post` are collected, without abandoning the rest of the snapshot;
8. the remaining backlog is reported and postponed for the next update;
9. after the snapshot, a single `AggregateException` is thrown for the `Post`s
   failed.

A concurrent enqueue after capturing the snapshot is visible in
`HasPendingWork`, but does not enter the current drain. Counters must avoid
lost wakeups when enqueue and dequeue are interspersed.

### 7.3 Integration with `UiHost` and `UIRoot`

`UiHost.UpdateCore` opens a single update session for root:
```text
VerifyAccess
Install scoped SynchronizationContext
Apply viewport, initial-frame and time-sensitive invalidations
Drain Relay once
Run pre-input scheduler gate
Dispatch input
Run post-input scheduler gate
Commit retained render data
Restore previous SynchronizationContext
```
`UIRoot.ProcessFrame` remains useful in tests and custom hosting: the public call
open the same gate, drain once and then process the scheduler. `UiHost`
it uses an internal core that does not re-drain between the pre-input gate and the
post-input.

Relay callbacks can invalidate the UI, and the scheduler sees those
invalidations in the same update. A `Post` made from the input or during the drain
waiting for the next update.

### 7.4 Wake-up for Windows and MonoGame

- `MonoGameUiHost` is pumped by the game with every update and does not require an OS signal
  separately.
- `WindowApplicationRuntime.PumpOnce` includes
  `context.Root.Relay.HasPendingWork` in the predicate that requires `Render`.
- `RenderRequested` and the Relay backlog remain separate concepts; one
  callback that doesn't visually change anything can produce an update without a new draw,
  according to the existing retained contract.
- `UiHost.SetRoot` and `WindowApplicationRuntime.GetOrCreateContext` check
  compatibility of the root thread with the window runtime.

### 7.5 Thread affinity for UI

An internal mutation hook is introduced in `UiObject`, no-op for objects
generic and verified by `UIElement` when attached:
```text
UiObject.SetValue/ClearValue/SetValueUntyped/ClearValueUntyped
    -> VerifyMutationAccess()
UIElement.VerifyMutationAccess()
    -> Root?.Relay.VerifyAccess()
```
The same check applies to canonical points that bypass the properties:

- mutations in `UIElementCollection`;
- attach/detach from subtree;
- mutable methods of `UIRoot`;
- `UiHost.Update` and `Draw`;
- root-owned integration of motion.

Detached objects can be built and configured before attach, but
the attach and any subsequent mutation of an attached element must be on
the root thread. Reads do not receive locks; The UI remains thread-affine.

### 7.5.1 Motion uses Relay, without its own guard

`MotionThreadGuard` is completely deleted. There is no `[Obsolete]` adapter left, alias,
compatibility shim or internal copy with another hat. For root-owned Motion,
`MotionSystem` delegates the checks to `UIRoot.Relay`; internal points can
use a thin `MotionSystem.VerifyAccess()`, but it does not have thread ID and
it does not expose a second source of truth.

Public change is intentionally breaking:

- the type and the file `MotionThreadGuard` are deleted;
- delete `MotionSystem.ThreadGuard`;
- the constructors `MotionGraph` that receive `MotionThreadGuard` are deleted;
- standalone public constructors of `MotionGraph` and `ManualMotionTimeline`
  internally captures the current thread through the Relay internal joint contract;
- the internal root-owned constructor of `MotionGraph` receives Relay access of
  root, without publishing a new abstraction only for compatibility;
- all `motion.ThreadGuard.VerifyAccess()` calls become delegated checks
  to Relay, and the references from the tests and examples are rewritten.

### 7.5.2 Aspect uses the same authority

`AspectThreadGuard` is not created. Aspect operations that modify registers,
environment, subscriptions, invalidation or apply styles to an attached root
check access through the same Relay. The audit includes at least
`AspectRegistry.Register/Unregister`, `AspectEnvironment.Set`,
`AspectInvalidation.Track/Recompute/Untrack`, `AspectEngine.Apply` and
`AspectProcessor.Process/Clear`. Standalone objects without root keep their
current local contract; from the moment I touch a root, Relay decides the thread.

### 7.6 Auto-marshaling for bindings

The common reactive controller has two paths:

- on UI thread: reevaluate immediately, keeping latent and existing semantics;
- off-thread: does not read the path and does not reach the target; increment a version
  atomica and asks for a single reevaluation pending on Relay.

Coalescing uses `requestedVersion`, `processedVersion`, an atomic flag
of enqueue and activation generation. If a notification arrives during the course
reevaluation, the callback does not lose the wakeup: it schedules exactly a continuation
for the next update. During execution, the current state is read, not the value
captured by the first event.

Lifecycle rules:

- the controller captures the root Relay upon attach/activation;
- detach unsubscribes the sources and invalidates the generation;
- a callback already in the queue checks the generation and becomes no-op;
- reattach starts with new generation and complete refresh;
- template swap/disposal does not let the queue permanently retain the controller;
- the inactive conditional binding and the short-circuited fragments do not program
  refresh unnecessary;
- interpolations and `@when` expressions coalesce at the controller level
  compound, not one UI write for each leaf.

For `TwoWay`, the target-to-source write starts from the UI thread. If the source
later responds with `PropertyChanged` off-thread, that echo follows the same path
coalesce and the reentrancy guard remains valid across the frame boundary.
### 7.7 Programmatic Binding

`UiPropertyBinding<T>` adopts the same dispatch controller:

- an attached `UIElement` target automatically uses `Root.Relay`;
- `BindingOperations` receives overloads with `UiRelay` explicitly for one
  `UiObject` generic or a target not yet attached;
- an off-thread event without a resolvable Relay produces an actionable error,
  not a direct writing;
- The explicit Relay must coincide with the Relay of the root after
  attach, otherwise the binding is rejected;
- Existing APIs remain compatible for strictly UI-thread uses.

### 7.8 Other first-party notices

External handlers that touch root are audited. Initial policy:

| Signal | Policy |
| --- | --- |
| CLR `INotifyPropertyChanged` | auto-marshal, coalesced per controller |
| `ObservableValue<T>.ValueChanged` | auto-marshal when the binding has Relay |
| `ICommand.CanExecuteChanged` | auto-marshal and coalesce per command source/control |
| `ThemeProvider.ThemeChanged` | auto-marshal and coalesce per root |
| `IObservableResourceProvider.ResourceChanged` | FIFO marshal; without coalescing until the delta semantics allow |
| `ObservableList<T>.Changed` | UI-thread-only; the mutation is posted explicitly |
| `UiObject.PropertyChanged` | UI-thread-only; the mutation is verified before the event |
| input, layout, render, motion graph | UI-thread-only; I explicitly use the Relay at the input |

We do not introduce a generic adapter that captures any event through reflection.
Each integration declares its policy of coalescing, lifecycle and consistency.

### 7.9 Statistics and diagnosis

`FrameStats` receives counters for:

- `RelayedCallbacks`;
- `CanceledRelayCallbacks`;
- `FaultedRelayCallbacks`;
- `DeferredRelayCallbacks`;
- `RelayBacklogAfterUpdate`.

`HasWork` includes the callbacks executed in the update. A frame that just drains
The relay is not reported as idle. `UiRelay` keeps internal cumulative counters
for benchmarks and tests, without a parallel logging system. Completion
the subsequent async delegate does not retroactively modify an already `FrameStats`
published; the result or exception remains on `Task`.

Fail-fast messages include:

- the name of the operation;
- the owner thread and the current thread;
- the root or the diagnosable property, when it exists;
- the concrete recommendation `Relay.Post` or `await Relay.InvokeAsync`.

## 8. Estimated files

Likely new files:

- `UI/Relay/UiRelay.cs`;
- `UI/Relay/UiRelayOptions.cs`;
- `UI/Relay/UiRelaySynchronizationContext.cs`;
- `UI/Relay/UiRelayWorkItem.cs`;
- a minimal internal thread access contract under `UI/Relay/`, only if it is
  required for standalone Motion objects;
- `tests/Cerneala.Tests/UI/Relay/UiRelayTests.cs`;
- `tests/Cerneala.Tests/UI/Relay/UiRelayConcurrencyTests.cs`;
- `tests/Cerneala.Tests/UI/Relay/UiRelaySynchronizationContextTests.cs`;
- `tests/Cerneala.Tests/UI/Hosting/UiHostRelayIntegrationTests.cs`;
- `tests/Cerneala.Tests/UI/Data/UiPropertyBindingThreadingTests.cs`;
- `benchmarks/Cerneala.Benchmarks/UiRelayBenchmarks.cs`;
- new API pages under `docs-site/documentation/classes/` for public types.

Existing files possibly modified:

- `UI/Elements/UIRoot.cs`;
- `UI/Hosting/UiHost.cs`;
- `UI/Hosting/MonoGame/MonoGameUiHost.cs`;
- `UI/Hosting/Windows/WindowApplicationRuntime.cs`;
- `UI/Invalidation/FrameStats.cs`;
- `UI/Core/UiObject.cs`;
- `UI/Elements/UIElement.cs`;
- `UI/Elements/UIElementCollection.cs`;
- `UI/Elements/ElementLifecycle.cs`;
- `UI/Motion/Core/MotionSystem.cs`;
- `UI/Motion/Core/MotionGraph.cs`;
- `UI/Motion/Core/ManualMotionTimeline.cs`;
- all Motion files calling today `ThreadGuard.VerifyAccess()`;
- Aspect implementation for registry, environment, invalidation, engine and
  processor;
- `UI/Data/BindingOperations.cs` and the common binding controller;
- command, theme and resource integrations;
- API tests and documentation of all affected public types.

Intentionally deleted files:

- `UI/Motion/Core/MotionThreadGuard.cs`;
- tests dedicated exclusively to the old guard;
- `docs-site/documentation/classes/Cerneala.UI.Motion.Core.MotionThreadGuard.md`;
- its entry from `docs-site/documentation/manifest.json`.

Dependencies between plans:

- the Relay core, hosting, thread affinity, Motion and Aspect can be
  implemented independently;
- the complete integration of the bindings is done according to the syntactic contract from
  `docs/plans/2026-07-14-markup-data-bindings.md`;
- until then, the bindings keep the strict/fail-fast contract described there.

## 9. Implementation checklist

### Stage 0 - Inventory, baseline and RED tests

- [x] Regenerate `FileTree.md`, index `Cerneala.slnx --json` and confirm
  `doctor` green before changes.
- [x] Use RoslynIndexer for definitions and references of `MotionThreadGuard`,
  `MotionSystem.ThreadGuard`, builders `MotionGraph` and calls
  `ThreadGuard.VerifyAccess`; save the inventory in the implementation notes.
- [x] Inventory the examples and API pages that build
  `MotionThreadGuard`, plus the exact entry from the manifest.
- [x] Characterizes thread ownership for `UIRoot`, `UiHost`,
  `MonoGameUiHost`, `WindowApplicationRuntime`, Motion standalone and Aspect.
- [x] Characterizes the current order from `UiHost.Update`: pre-input, input,
  post-input, scheduler and commit, so that Relay is drained exactly once.
- [x] Add RED tests for `Post` worker-to-UI, FIFO, exact-once, snapshot,
  budget, cancellation, exceptions and backlog on an idle root.
- [x] Add RED tests for continuations `await`, two roots on the same thread
  and the previous `SynchronizationContext` restoration.
- [x] Add RED tests that prove that direct off-thread mutations do not change
  property store, tree or tails retained before throwing.
- [x] Add RED tests for root-owned Motion and Aspect that must accept
  only the thread declared by Relay.
- [x] Add characterization tests for Motion standalone builders which
  they must remain usable after deleting the public guard.
- [x] Use barriers and deterministic primitives in concurrent tests; don't use
  `Thread.Sleep` as proof of synchronization.

**Gate Stage 0**

- [x] The RED tests fail for the expected threading reasons, not from the setup or
  fragile timing.
- [x] The order of the frame and the existing UI-thread behavior are characterized.
- [x] All public and documentary references of `MotionThreadGuard` are
  inventoried before deletion.
- [x] The full baseline is green except for the new RED tests.
- [x] No public signature is changed in stage 0.

### Stage 1 - Thread access and the core `UiRelay`

- [x] Enter `UiRelayOptions` with default 1.024 and strict validation for
  `MaxCallbacksPerUpdate > 0`.
- [x] Enter `UiRelay` root-owned, capturing owner thread ID in
  constructor `UIRoot`.
- [x] Implements `CheckAccess`, `VerifyAccess`, `HasPendingWork` and
  `PendingCount` without crossings or global lock on the read path.
- [x] Implements the MPSC queue with `ConcurrentQueue`, pending counter atomic and
  work item state machine that cannot be executed twice.
- [x] Implements `Post(Action)` with capture `ExecutionContext`.
- [x] Implements the `InvokeAsync` overloads with
  `TaskCompletionSource` configured `RunContinuationsAsynchronously`.
- [x] Implements race-safe cancellation before dequeue, between dequeue and run
  and after starting the async callback.
- [x] Implements the internal snapshot-based drain with numerical budget and FIFO.
- [x] Process the rest of the snapshot after exceptions `Post`, then throw a
  `AggregateException`; do not turn exceptions `InvokeAsync` into errors of
  frames.
- [x] Release `CancellationTokenRegistration`, `ExecutionContext`, the delegates
  and completion sources after completion in order not to retain graphs of objects.
- [x] Add tests for null arguments, invalid options, access checks,
  enqueue/dequeue, FIFO, cancellation and exceptions.
- [x] Adds multi-producer tests that check exactly-once and FIFO per producer,
  without imposing a false order between competing threads.
- [x] Add tests for enqueue competing with the end of the drain, checking that
  `HasPendingWork` doesn't miss the wakeup.
- [x] Add tests for callbacks that are reposted: the new work item remains
  for the next update.
- [x] Add tests for the 1,024 budget, backlog and continuation in updates
  successive.
- [x] Reindexes after each C# or project-file modification.

**Gate stage 1**

- [x] The core passes all repeated unit and concurrency tests.
- [x] No UI operation is partially migrated to Relay yet.
- [x] Enqueue and `HasPendingWork` do not depend on the retained tree.
- [x] There is no blocking invoke, nested pump or thread created by Relay.

### Stage 2 - `SynchronizationContext` async-first

- [x] Insert the `UiRelaySynchronizationContext` internal adapter.
- [x] Connects `Post` to Relay and implements `Send` only as fast-path on
  owner thread; off-thread throws a message recommending `InvokeAsync`.
- [x] Capture and restore the previous context with `try/finally`.
- [x] Add an idempotent internal scope for controlled nested updates, without
  to leave the context installed after exit or exception.
- [x] Checks that a `await Task.Yield()` started in a UI callback
  continue the execution on Relay in a later update.
- [x] Check `AsyncLocal`, culture and tracing context over `Post`,
  `InvokeAsync` and continue async.
- [x] Check two roots on the same thread: each continuation returns in
  the tail of the root that captured it.
- [x] Checks the fact that a pre-existing context is restored exactly after the update.
- [x] Checks exceptions and cancellation of async callbacks without blocking the drain.
- [x] Reindex after C# changes.

**Gate stage 2**

- [x] Async continuations return to the correct root.
- [x] No test depends on `.Wait()` or `.Result` on the owner thread.
- [x] The context does not remain globally installed between two updates.
- [x] The public API remains async-first and minimal.

### Stage 3 - Integration with frame loop and hosting

- [x] Add `UIRoot.Relay` and build it before services
  root-owned that need thread access.
- [x] Enter the internal update gate that verifies access, installs the context
  and drains the Relay exactly once.
- [x] Refactors `UIRoot.ProcessFrame` into a public entrance with drain and a
  internal core reused by `UiHost` without second drain.
- [x] Integrates the gate in `UiHost.UpdateCore` before scheduler and input.
- [x] Add `UiHost.Relay` and `MonoGameUiHost.Relay` as properties of
  convenience without duplication of ownership.
- [x] Check `UiHost.Update`, `Draw`, `SetRoot` and the new root via
  `VerifyAccess` before moves or backend calls.
- [x] Add `Relay.HasPendingWork` to the wake predicate
  `WindowApplicationRuntime.PumpOnce`.
- [x] Checks when creating the window context that the runtime and the root
they have the same owner thread.
- [x] Keep a single pump in `MonoGameUiHost.Update`, including when
  `GeneratedWindowApplication.PumpHosted` runs before the main host.
- [x] Ensures that the invalidations produced by Relay are processed in the gateway
  pre-input of the same update.
- [x] Ensures that a `Post` made in input or drain is postponed until the next
  update, even if the host is running the post-input scheduler.
- [x] Extends `FrameStats` with dispatch counters and includes execution in
  `HasWork`.
- [x] Keep tests idle: zero callbacks means zero new allocations on
  verification path and does not start the artificial scheduler.
- [x] Add root replacement tests: the old Relay is not closed,
  the new root is checked, and each queue is pumped only with its root.
- [x] Reindex after each C# change.

**Gate stage 3**

- [x] `UiHost.Update` drains exactly once on all paths.
- [x] Standalone Windows processes a callback on a previously idle root.
- [x] Relay invalidations arrive in the retained scheduler in the same update.
- [x] The existing input/scheduler/render and no-work order tests remain green.

### Stage 4 - Thread affinity for UI, Motion and Aspect

- [x] Add the internal hook `VerifyMutationAccess` to `UiObject` and call it
  in all paths typed/untyped by `SetValue` and `ClearValue` before access to
  `UiPropertyStore`.
- [x] Overwrite the hook in `UIElement` using `Root?.Relay.VerifyAccess`,
  keeping the configuration free for detached elements.
- [x] Protects the canonical mutations in `UIElementCollection` and
  `ElementLifecycle.AttachSubtree/DetachSubtree` before changing the shaft.
- [x] Protects mutable root-owned methods that can invalidate, change the theme,
  resources, viewport or platform services.
- [x] Enter only if the minimum internal thread access contract is required
  from the Relay subsystem; `UiRelay` implements it, and the standalone version
  capture the current thread without new public API.
- [x] Migrate `MotionSystem` root-owned to the same access thread owned by
  `UIRoot.Relay` and add an internal `MotionSystem.VerifyAccess()` only as a point
  of delegation, without own thread owner.
- [x] Deletes public property `MotionSystem.ThreadGuard` and replaces all
  calls `motion.ThreadGuard.VerifyAccess()` from coordinators, bindings,
  transactions and frame processing.
- [x] Rewrite constructors `MotionGraph`: standalone overloads capture
  internally the calling thread, and the internal root-owned constructor receives thread
  Relay access; `MotionThreadGuard` no longer receives any overload.
- [x] Migrate `ManualMotionTimeline` and the standalone tests to the new contract
  internally, keeping the thread affinity check without public guard.
- [x] Completely delete `UI/Motion/Core/MotionThreadGuard.cs`; do not add `[Obsolete]`,
  adapter, alias, forwarding type or compatibility shim.
- [x] Check through RoslynIndexer that there are no more C# references to the type,
  `MotionSystem.ThreadGuard` or removed builders.
- [x] Link mutable points Aspect to Relay without `AspectThreadGuard`: registry,
  environment, invalidation, engine and processor check the same owner thread
  before changing root-owned state.
- [x] Add Aspect tests on owner thread and off-thread for
  `Register/Unregister`, `Set`, `Track/Recompute/Untrack`, `Apply`, `Process` and
  `Clear`, adjusted to the real public/internal surface found during implementation.
- [x] Add tests that confirm that an off-thread mutation does not change the value,
  value source, dirty flags, invalidation queue or version tree.
- [x] Add tests for detached element configured on the worker and attached later
  on the owner thread.
- [x] Add tests for attach/detach, Motion, Aspect and off-thread root methods.
- [x] Run a Roslyn audit of public points that modify an attached root and
  explicitly note the remaining UI-thread-only exceptions by superior contract.
- [x] Reindex after each C# change.

**Gate Stage 4**

- [x] Properties and UI tree cannot be moved off-thread via paths
  canonical.
- [x] Motion and the rest of the root use the same owner thread.
- [x] Aspect and the rest of the root use the same owner thread, without a separate guard.
- [x] `MotionThreadGuard`, `MotionSystem.ThreadGuard` and the builders who
  receive no longer exists in the compiled API.
- [x] The detached elements remain easy to build without an artificial Relay.
- [x] No locks were entered in layout, render or property store.

### Stage 5 - Auto-marshaling for bindings

- [x] Implements in the common reactive controller fast-path UI and the path
  off-thread coalesced, without evaluating the source on the worker.
- [x] Uses atomic versions and generation guards so that a notification
  arrived during the refresh not to be lost.
- [x] Re-evaluates the entire typed path on the UI thread and reconnects the segments
  nested to the current state.
- [x] Coalesce per controller simple bindings, interpolations and
  the expressions `@when`, not per gross event.
- [x] Keeps the logic short-circuit at evaluation, but allows any leaf
  observed to ask for the re-evaluation of the composition.
- [x] Logically cancels stale callbacks to detach, reattach, template swap,
  the loss of a conditional branch and disposal.
- [x] Keep the synchronous fast-path for `PropertyChanged` already raised on
  the UI thread.
- [x] Check `TwoWay`: local write-back remains immediate, off-thread echo
  it does not create the loop and the last value wins deterministically.
- [x] Migrate `UiPropertyBinding<T>` and `ObservableValue<T>.ValueChanged` to
  the same dispatch mechanism.
- [x] Add `BindingOperations` overloads with explicit Relay for
  generic/unattached targets and validates the mismatch after attach.
- [x] Keeps fail-fast actionable when an off-thread programmatic binding does not
  can resolve no Relay.
- [x] Add tests with 1, 2 and 10,000 notifications, more productions, paths
  nested, change `DataContext`, interpolation and `(A and B) or C`.
- [x] Add tests for event in-flight simultaneously with detach/dispose and confirm
  zero stale writing and zero retention after the next drain.
- [x] Add tests that confirm that the source is not read at all on the worker thread.
- [x] Updates the strict contract from the plan and data binding documentation:
  CLR `INotifyPropertyChanged` attached becomes auto-marshal, while
  `UiObject.PropertyChanged` and direct UI mutations remain strictly UI-thread.
- [x] Reindex after each C# or source-generator change.

**Gate Stage 5**

- [x] All forms of binding in the dependent plane react off-thread without
  to touch the UI on the worker.
- [x] Bursts are coalesced and the last state is not lost.
- [x] Lifecycle does not allow stale callbacks or subscription leaks.
- [x] The existing UI-thread behavior remains immediate and compatible.

### Stage 6 - Other first-party signals

- [x] Inventory through Roslyn all subscriptions in which an external event
  it ends up in `Invalidate`, in a retained queue or in a UI mutation.
- [x] Migrate `ICommand.CanExecuteChanged` to a coalesced refresh on Relay
  per active control/source.
- [x] Migrate `ThemeProvider.ThemeChanged` to a coalesced invalidation per root.
- [x] Migrate `IObservableResourceProvider.ResourceChanged` to FIFO callbacks
  on Relay, keeping the order of the deltas and without unjustified coalescing.
- [x] Check unsubscribe and callback stale when changing the provider, detach,
  root replacement and disposal.
- [x] Keep `ObservableList<T>.Changed` UI-thread-only and add diagnostic
  before processing an off-thread mutation observed by an attached control.
- [x] Add examples and tests for the correct mutation of the collection by
  `InvokeAsync`, including cancellation.
- [x] Don't migrate an event just because it exists; document for each if
  is UI-owned, auto-marshaled or requires explicit dispatch.
- [x] Reindex after each C# change.

**Gate stage 6**

- [x] No inventoried first-party handlers accidentally touch retained queues
  from the worker.
- [x] The coalescent signals do not lose their final state, and the FIFO deltas do not change
  the order.
- [x] Mutable collections are not falsely presented as thread-safe.
- [x] All new subscriptions have cleanup checked.
### Stage 7 - Performance, stress and diagnosis

- [x] Add Release benchmarks for `Post`, `InvokeAsync`, empty drain,
  drain 1/100/1.024, backlog over budget and enqueue with 1/2/4/8 productions.
- [x] Add benchmark for coalescing 10,000 notifications into one
  binding and in an interpolation with several sources.
- [x] Measure time, allocated bytes, Gen0 and throughput; archive the hardware,
  the runtime and the configuration along with the results.
- [x] Structurally check that `HasPendingWork` and `PendingCount` do not allocate after
  warmup.
- [x] Checks the fact that an idle frame without backlog does not allocate due
  Relay and does not needlessly install new context objects.
- [x] Run stress test with concurrent productions, cancellation, exceptions, detach and
  root replacement for at least 100,000 work items, without fragile sleeps.
- [x] Check exactly-once: executed + canceled = accepted, no loss or
  duplicates.
- [x] Check backlog bounded per binding due to coalescing, even if
  the root remains temporarily unrooted.
- [x] Do not add pooling until the benchmark demonstrates a real problem and
  the tests can guarantee the complete reset of the work item.
- [x] Re-indexes after benchmark/project changes.

**Gate stage 7**

- [x] The results are reproducible and compared with the baseline.
- [x] The drain respects the budget and scales approximately linearly.
- [x] The idle frame does not receive allocation regressions or false work.
- [x] There are no lost wakeups, duplicates or callbacks after confirmed cancellation.

### Stage 8 - API documentation and final verification

- [x] Use the `writing-api-documentation` skill for all changes
  public from `docs-site/documentation/classes/`.
- [x] Add pages for `UiRelay` and `UiRelayOptions` and update
  `docs-site/documentation/manifest.json`.
- [x] Update the pages `UIRoot`, `UiHost`, `MonoGameUiHost`, `FrameStats`,
  `BindingOperations`, `UiPropertyBinding<T>`, `MotionSystem`, `MotionGraph`,
  `ManualMotionTimeline` and any other modified public type.
- [x] Delete API page `Cerneala.UI.Motion.Core.MotionThreadGuard.md`, remove
  entry from the manifest and rewrites all the examples that built the guard.
- [x] Confirm by text search that `MotionThreadGuard` no longer appears in C#,
  tests, API docs or manifest; the historical mentions in this plan are the only ones
  exception accepted.
- [x] Documents the Relay branding along with Aspect and Motion, without alias
  public `Dispatcher` and no names mixed between namespaces, types and hosts.
- [x] Document examples for `Post`, `InvokeAsync`, generic result,
  cancellation, exceptions and the mutation of a `ObservableList<T>` on UI thread.
- [x] Documents the exact order compared to the input/scheduler, the snapshot,
the budget, the latency of an update and the fact that the root must be pumped.
- [x] Documents the difference between auto-marshaling a CLR notification and
  prohibition of direct movement of a `UIElement` off-thread.
- [x] Documents the obligation of the source to allow coherent reading and the fact that
  auto-marshaling does not make the ViewModel or collection thread-safe.
- [x] Documents the lack of `Invoke` blocker, the lack of priorities and the risk of
  deadlock of the synchronous waiting on the owner thread.
- [x] Updates the conceptual documentation of hosting and data binding, without a
  put API docs under `docs/documentation/`.
- [x] Run a public API diff and confirm that all additions are intentional,
  correct and documented nullables.
- [x] Run the formatter, `dotnet build .\Cerneala.slnx` and
  ZZZ BLACK 10ZZZ.
- [x] Run the target tests with
  `dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~UiRelay|FullyQualifiedName~Relay"`.
- [x] Run the hosting, data binding, Motion, Aspect, invalidation tests,
  lifecycle and Windows runtime.
- [x] Run the final benchmarks in Release and save the report.
- [x] Regenerate `FileTree.md`, reindex `Cerneala.slnx --json` and confirm
  zero warnings of the indexer.
- [x] Run `git diff --check` and confirm that there are no temporary files left,
  flaky processes or tests.

**Gate Stage 8**

- [x] The build and the complete suite are green.
- [x] API diff contains only the approved surface.
- [x] The API documentation and the manifest are synchronized.
- [x] Benchmarks and stress tests are archived and reproducible.

## 10. Risks and mitigations

### Lost wakeup between coalescing and drain

- [x] Use atomic versions and barrier tests in all cold windows,
  not a simple naively reset flag.

### Async callback that blocks the frame

- [x] The drain starts the async delegate and follows the task without waiting
  its synchronous completion.

### Unlimited backlog on a non-pumped root

- [x] Automatically merges status signals, exposes `PendingCount`, applies
  budget per update and documents the cancellation for explicit operations.

### Fire-and-forget exception lost

- [x] Process the complete snapshot, count the fault and throw aggregate from
  update; don't let the errors disappear into nothingness like the salary after the invoices.

### UI move already done before the marshal

- [x] Checks access to canonical mutation entries; don't try to marshal
  `UiObject.PropertyChanged` after the property store was changed.

### Collection read while the worker modifies it

- [x] Keep `ObservableList<T>` UI-thread-only and ask to post the mutation
  complete on Relay.

### Double drain in `UiHost.Update`
- [x] Separates the public gateway `UIRoot.ProcessFrame` from the internal core used by
  host and explicitly tests the pre-input plus post-input update.

### Standalone Windows does not wake up

- [x] Include the Relay backlog in the wake predicate and test a root
  completely idle with a single callback worker.

### The breaking migration of Motion leaves an old path behind

- [x] Deletes the old guard and overloads in one coherent batch,
  migrate all call sites from the repo and explicitly validate the API diff;
  the intended compatibility is the standalone behavior, not the old signatures.

### ExecutionContext holds objects

- [x] Cleans references after execution/cancellation and adds tests with weak references
  for callbacks and detached controllers.

## 11. Stop conditions

The implementation stops for re-evaluation if:

Final audit: none of the stop conditions were activated. May ticks
below explicitly confirms the negative check of each condition.

- [x] Confirmed that the frame loop can guarantee a single drain without the public change of
  semantics `UiHost.Update`;
- [x] Confirmed that `WindowApplicationRuntime` and his roots do not have owner threads
  legitimately different in the current configuration;
- [x] Confirmed that the integration with the bindings does not evaluate CLR paths on the worker;
- [x] Confirmed that atomic versions avoid lost wakeups without an unbounded queue per event;
- [x] Confirmed that a blocking invoke or nested pump is not necessary for compatibility;
- [x] Confirmed that first-party collection mutations are not accepted off-thread without
  a thread-safe source;
- [x] Confirmed that the public API does not accumulate priorities, timers or generic scheduler
  without separate request;
- [x] Confirmed that the benchmarks do not show a persistent idle regression that cannot be isolated.

The problem is documented and discussed separately; we do not cover it with a global lock
and a prayer.

## 12. Recommended order

1. Characterize the thread, frame order and current defects.
2. Build and verify the MPSC core in isolation.
3. Add the async scoped context.
4. Integrate a single drain gate in root and hosts.
5. Unify thread affinity for property store, tree, Motion and Aspect and
   completely delete `MotionThreadGuard`.
6. After completing the data binding plan, adopt Relay and coalescing
   in the common reactive controller.
7. Migrate the other first-party signals according to the table, without magic adapter.
8. Close performance, documentation and complete suite.

## 13. The definition of ready

- [x] Any `Post`/`InvokeAsync` accepted by Relay runs exactly once or
  is canceled explicitly, never per worker and never twice.
- [x] The relay is drained only once at the beginning of the update, before
scheduler and input, with snapshot and deterministic budget.
- [x] An idle Windows root is woken up by the backlog, and a MonoGame host
  process to the following `Update`.
- [x] `await` from the UI code returns through the context of the correct root, and the context
  previously it is restored after the update.
- [x] Direct mutations of an attached element are rejected off-thread before
  to change the property store, tree or retained queues.
- [x] Motion and Aspect use the Relay authority of root; does not exist
  `MotionThreadGuard`, `AspectThreadGuard` or another parallel owner thread source.
- [x] `MotionSystem.ThreadGuard`, builders receiving the guard, API page
  and the manifest entry have disappeared, and Motion standalone remains functional.
- [x] The simple, nested, `TwoWay`, interpolated, conditional and
  expressions `@when` auto-marshal `INotifyPropertyChanged` off-thread,
  they coalesce the bursts and display the last state.
- [x] Detach, reattach, template swap, root replacement and disposal does not leave
  stale callbacks, forgotten tasks or retained references.
- [x] `CanExecuteChanged`, the theme and resources follow the stated policy, and
  mutable collections require the explicit use of Relay.
- [x] The idle frame remains cheap, the backlog is observable, and the benchmarks
  confirm the multi-producer scaling and the budgeted drain.
- [x] All new public APIs are documented in the single source of truth,
  the manifest is synchronized, the diff API is approved, the build and all
  tests are green.
- [x] The public surface consistently carries the `Relay` branding: namespace
  `Cerneala.UI.Relay`, `UiRelay`, `UiRelayOptions` and the properties `.Relay`, without
  public duplicates called `Dispatcher`.

The system is ready when a worker can announce the UI without touching it directly,
the frame loop decides exactly when the thing runs, and a flurry of notifications does not
turn the update into a dumpster of non-deterministic callbacks.
