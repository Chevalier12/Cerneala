# Relay

> Code: `UI/Relay`, `UI/Elements/UIRoot.cs` (`BeginUpdate`), `UI/Hosting/UiHost.cs` · Verified at commit `0cb32300` (2026-10-10)

## Responsibility

Relay moves work from any thread onto the UI thread that owns a `UIRoot`, and runs it at a defined point of the next update. It also enforces thread affinity: UI state is changed only on the owning thread.

Relay does not make application state thread-safe. A worker thread posts the whole UI change as one callback; the callback runs later on the UI thread.

Example: a worker finishes loading and calls `root.Relay.Post(() => label.Text = "Done")`. Nothing changes at once. On the next `root.ProcessFrame()` (or `UiHost.Update`), the callback runs on the UI thread before any frame phase, so the `Text` change is measured and rendered in that same frame.

## Components

| Type | File | Role | Owner |
|---|---|---|---|
| `UiRelay` | `UI/Relay/UiRelay.cs` | Queue (`ConcurrentQueue`), `Post`, `InvokeAsync` overloads, `CheckAccess`, `VerifyAccess`, internal `Drain` | `UIRoot` (`UIRoot.Relay`, created in the `UIRoot` constructor) |
| `UiRelayOptions` | `UI/Relay/UiRelayOptions.cs` | `MaxCallbacksPerUpdate`, default `1024`, must be `> 0` | passed as `relayOptions` to the `UIRoot` constructor |
| `UiRelayDrainResult` | `UI/Relay/UiRelayDrainResult.cs` | Internal counts of one drain: snapshot, dequeued, executed, canceled, faulted, deferred, backlog | one drain |
| `UiRelaySynchronizationContext` | `UI/Relay/UiRelaySynchronizationContext.cs` | Internal; `Post` marshals `await` continuations to the relay; `Send` runs inline on the owner and throws elsewhere | `UiRelay` |
| `UiRelayRefreshDispatcher` | `UI/Relay/UiRelayRefreshDispatcher.cs` | Internal; coalesces reactive refreshes (bindings, command state, theme) into one post per generation | binding / subscription |
| `IUiThreadAccess`, `CapturedUiThreadAccess` | `UI/Relay/` | Internal; thread check for components that are not given a relay (Aspect engine, Motion graph, timelines) | the component |

`UiRelay`'s constructor is internal. Application code gets a relay from `UIRoot.Relay` or `UiHost.Relay` (the current root's relay).

## Data Flow

1. **Post.** `Post(Action)` and every `InvokeAsync` overload capture the `ExecutionContext`, increment the pending count and enqueue. They never run the callback inline, even on the UI thread. `InvokeAsync` returns a `Task` that completes when the callback has run.
2. **Drain.** `UIRoot.BeginUpdate` calls `UiRelay.Drain` on the owning thread, inside the relay's `SynchronizationContext`:
   - It reads `PendingCount` once (the snapshot) and runs at most `min(snapshot, MaxCallbacksPerUpdate)` items, in FIFO order.
   - Callbacks posted during the drain go to the end of the queue. They are outside the snapshot and run in the next drain.
   - `PendingCount` after the loop is reported as the backlog.
3. **Count.** `FrameStats.CountRelay` adds the drain result to the frame's counters.
4. **Faults.**
   - A `Post` callback that throws does not stop the drain. All `Post` faults of one drain are collected and thrown together as one `AggregateException("One or more fire-and-forget Relay callbacks failed.")`, after the snapshot has run. `BeginUpdate` rethrows it, so that frame's phases do not run.
   - An `InvokeAsync` callback that throws faults only its own `Task`; the drain continues and does not throw.

Concrete example (from `SnapshotAndBudgetDeferNewOrExcessWork`), with `MaxCallbacksPerUpdate = 2`: callbacks 1, 2, 3 are queued, and callback 1 posts callback 4. The first drain runs `[1, 2]` and reports `Backlog = 2`. The second drain runs `3` and `4`. The final order is `[1, 2, 3, 4]`.

## Where The Drain Runs

The drain is not a `FramePhase`. It runs before the scheduler, in `UIRoot.BeginUpdate`:

| Caller | When |
|---|---|
| `UIRoot.ProcessFrame` | Every call, before the no-work check, so idle frames also drain |
| `UiHost.UpdateCore` | After the viewport is applied and render time advances, before scheduled layout and input dispatch |
| `SceneSimulationContext2D.Update` | Headless scene contexts only; calls `Relay.Drain()` directly, not counted in `FrameStats` |

`WindowApplicationRuntime` treats `Relay.HasPendingWork` as a reason to render a frame, so a post from a worker wakes an idle window.

The full frame order is in [invalidation-and-frame.md](invalidation-and-frame.md).

## Lifecycle And Ownership

- A relay lives as long as its `UIRoot`. `UiRelay` has no `Dispose`, `Clear` or shutdown, and `UIRoot` has no dispose path.
- Root replacement: `UiHost.SetRoot` does not drain or clear the old relay. Work queued on the old root stays there and runs only if that root is processed again.
- Cancel: canceling an `InvokeAsync` token marks the item canceled and releases its callback. The item still counts in `PendingCount` until a drain dequeues it, and it takes one slot of the cap.
- Detached elements have no owner relay, so mutating them off-thread is not checked. The check applies again once they are attached.
- Replaced theme and resource providers: the subscription is deactivated, and work it already queued becomes a no-op when it runs (generation check in `UiRelayRefreshDispatcher`, disposed check in the resource subscription).

## Thread Affinity

- `UiRelay.CheckAccess` compares the current managed thread id with the thread that constructed the relay.
- `UiRelay.VerifyAccess` throws `InvalidOperationException("Relay work must be drained and UI state must be accessed on the owning UI thread.")`.
- `UiHost.Update`, `UiHost.Draw`, `UiHost.SetRoot` and attached property writes call it before changing state, so an off-thread call throws and changes nothing.
- `UiRelaySynchronizationContext.Send` from another thread throws and tells the caller to use `InvokeAsync`.

## Invariants

- Default cap is `1024`; `0` and negative values are rejected. `tests/Cerneala.Tests/UI/Relay/UiRelayCoreTests.cs:OptionsDefaultAndInvalidValuesAreDeterministic`, `:DefaultBudgetProcessesExactly1024Callbacks` (1025 posts: 1024 run, `Backlog = 1`).
- A drain runs one FIFO snapshot; work posted during the drain and work beyond the cap wait for the next drain. `UiRelayCoreTests.cs:SnapshotAndBudgetDeferNewOrExcessWork`; `tests/Cerneala.Tests/UI/Relay/UiRelayHostingIntegrationTests.cs:StandaloneFrameDrainsOneSnapshotAndCountsDispatch`.
- Access checks use the constructing thread. `UiRelayCoreTests.cs:AccessChecksUseTheConstructingThread`.
- Off-thread `UiHost.Update`, `Draw` and `SetRoot` throw before any work (root unchanged, zero backend renders), and an off-thread write to an attached element's property throws before changing it. `UiRelayHostingIntegrationTests.cs:HostOperationsRejectOffThreadBeforeUpdateDrawOrRootReplacementWork`; `RelayStageZeroTests.cs:AttachedPropertyMutationOffThreadThrowsBeforeChangingState`.
- A `Post` fault does not abandon the snapshot; faults are aggregated. `UiRelayCoreTests.cs:PostFaultDoesNotAbandonTheSnapshot`; `tests/Cerneala.Tests/UI/Relay/RelayStageZeroTests.cs:PostFaultIsAggregatedAfterSnapshotAndInvokeFaultStaysOnTask`.
- An `InvokeAsync` callback that throws faults its task, not the drain. `tests/Cerneala.Tests/UI/Relay/UiRelayInvocationCompletionTests.cs:SynchronousCallbackThrowFaultsTheReturnedTaskNotTheDrain`.
- Faulted drains are counted before the rethrow. `UiRelayHostingIntegrationTests.cs:StandaloneFrameCountsFireAndForgetFaultBeforeRethrowing`.
- The host drains only the current root; the old root's relay keeps its work. `UiRelayHostingIntegrationTests.cs:RootReplacementPumpsOnlyTheCurrentRootAndKeepsOldRelayUsable`.
- Off-thread theme changes coalesce into one pending callback (10,000 changes → `PendingCount = 1`); resource changes keep FIFO order and ignore replaced providers. `tests/Cerneala.Tests/UI/Relay/FirstPartyRelayIntegrationTests.cs:ThemeChangesCoalesceAndProviderReplacementInvalidatesQueuedWork`, `:ResourceChangesKeepFifoAndIgnoreReplacedProviders`.
- Netestat: the snapshot is exact only when no producer is between incrementing the count and enqueueing at the moment of the snapshot (`UiRelay.Enqueue` increments before `queue.Enqueue`). A callback reposted during that window can run in the same drain.
- Netestat: abandoning a root with pending callbacks (no test disposes or abandons a root with queued work).

## Diagnostic

`FrameStats` has `RelaySnapshotCallbacks`, `RelayDequeuedCallbacks`, `RelayExecutedCallbacks`, `RelayCanceledCallbacks`, `RelayFaultedCallbacks`, `RelayDeferredCallbacks` and `RelayBacklog`. `UIRoot.Detective.CaptureFrame` copies them into `FrameDiagnosticsSnapshot`. See [detective.md](detective.md).

## Known Limitations

- `UiRelayDrainResult.Deferred` and `Backlog` are always the same value (`UiRelay.Drain` passes `backlog` twice).
- If two drains run in one frame, `FrameStats.CountRelay` sums `RelayDeferredCallbacks` but overwrites `RelayBacklog`.
- `SceneSimulationContext2D.Update` drains without counting in `FrameStats`.
- The 2026-09-02 audit ([2026-09-02-relay-audit.md](../audits/2026-09-02-relay-audit.md)) is history. One of its findings, the drain running before the new viewport is applied, no longer holds: `UiHost.UpdateCore` applies the viewport before `BeginUpdate`. Its other findings were not re-verified here.
