# UiRelay Class

## Definition
Namespace: `Cerneala.UI.Relay`

Assembly/Project: `Cerneala`

Source: `UI/Relay/UiRelay.cs`

Queues callbacks from any producer thread for deterministic execution on one owning UI thread.

```csharp
public sealed class UiRelay
```

Inheritance:
`object` -> `UiRelay`

## Examples

Post fire-and-forget work when the caller does not need a result:

```csharp
using Cerneala.UI.Relay;

static void RequestRefresh(UiRelay relay, Action refresh)
{
    relay.Post(refresh);
}
```

Use `InvokeAsync` when the caller needs completion, cancellation, a result, or exception propagation:

```csharp
using Cerneala.UI.Relay;

static Task<int> ReadOnUiThreadAsync(
    UiRelay relay,
    Func<int> read,
    CancellationToken cancellationToken)
{
    return relay.InvokeAsync(read, cancellationToken);
}
```

Observe cancellation and callback exceptions through the returned task:

```csharp
try
{
    await relay.InvokeAsync(
        () => SaveCurrentDocument(),
        cancellationToken);
}
catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
{
    // The callback was canceled before or during cooperative execution.
}
catch (DocumentSaveException error)
{
    LogSaveFailure(error);
}
```

Marshal an incremental collection mutation as one cancelable UI operation:

```csharp
static Task AddItemAsync<T>(
    UIRoot root,
    ObservableList<T> items,
    T item,
    CancellationToken cancellationToken)
{
    return root.Relay.InvokeAsync(() => items.Add(item), cancellationToken);
}
```

## Remarks

`UiRelay` captures its owner thread when `UIRoot` constructs it and is exposed through `UIRoot.Relay`, `UiHost.Relay`, and `MonoGameUiHost.Relay`. The class does not create a thread, block the caller, or run a nested message loop. It does not provide a blocking `Invoke`, priorities, delayed dispatch, or a general-purpose task scheduler.

`UIRoot.ProcessFrame` and each host update drain one queue snapshot on the owner thread before retained scheduler and input work. The root must keep being pumped for queued callbacks and captured continuations to run. Relay invalidations can therefore participate in the same update, while callbacks posted during the drain or input wait for a later update.

`Post` and every `InvokeAsync` overload always enqueue work, including calls made from the owner thread. Work is dequeued in FIFO order after enqueue linearization. A drain processes a stable snapshot up to the configured callback budget, so callbacks posted during a drain remain pending for a later update.

Scheduling captures the caller's `ExecutionContext`. `AsyncLocal` state, culture, and tracing context therefore flow into the callback. `PendingCount` includes callbacks that have not started; it does not include asynchronous callbacks after their delegate has started.

Relay drains install a root-specific `SynchronizationContext` only for the duration of the update and restore the previous context even when a callback fails. An `await` that captures this context posts its continuation back to the same root's Relay queue for a later update. Two roots on one thread therefore keep their continuation queues independent.

Cancellation that wins before execution prevents the callback from running. Cancellation cannot interrupt a synchronous callback after it starts. The asynchronous overload receives the token for cooperative cancellation and begins the delegate on the owner thread without synchronously blocking the drain until its returned task completes.

Exceptions from `InvokeAsync` are stored on the returned task. Exceptions from `Post` are collected while the rest of the drain snapshot continues, then surfaced by the update as an `AggregateException`. Prefer `InvokeAsync` whenever the caller must observe failure.

First-party notifications use explicit threading policies rather than treating every event as magic dispatch:

| Signal | Policy |
| --- | --- |
| Attached CLR `INotifyPropertyChanged` and `ObservableValue<T>` binding notifications | UI-thread fast path; off-thread state refresh coalesced per active binding. |
| `IObservableCommand.CanExecuteChanged` | UI-thread fast path; off-thread re-query coalesced per attached command control. |
| `ThemeProvider.ThemeChanged` | UI-thread fast path; off-thread aspect invalidation coalesced per root. |
| `IObservableResourceProvider.ResourceChanged` | UI-thread fast path; each off-thread delta is posted FIFO and is not coalesced. |
| `ObservableList<T>.Changed` observed by an attached control | UI-thread-only; marshal the complete mutation with `InvokeAsync`. |
| `ItemCollection`, `StrokeCollection`, UI properties, input, template, Aspect, Motion, and retained-control events | UI-owned; attached consumers reject off-thread processing. |

Relay moves callback execution, not arbitrary mutable data. Sources still need coherent publication, and mutable collections remain non-concurrent.

Do not call `.Wait()`, `.Result`, or `GetAwaiter().GetResult()` for Relay work on the owner thread. The synchronous wait prevents the next update from draining the callback and can deadlock. Direct mutation of an attached `UIElement` also remains invalid off-thread; marshal the complete mutation rather than changing UI state first and attempting to relay its notification afterward.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `HasPendingWork` | `bool` | Gets whether at least one callback has not started. |
| `PendingCount` | `int` | Gets the number of callbacks that have not started. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `CheckAccess()` | `bool` | Returns `true` when called on the owning UI thread. |
| `VerifyAccess()` | `void` | Throws when the current thread is not the owning UI thread. |
| `Post(Action callback)` | `void` | Enqueues a fire-and-forget callback and captures the caller's execution context. |
| `InvokeAsync(Action callback, CancellationToken cancellationToken = default)` | `Task` | Enqueues an action and returns a task for completion, cancellation, or failure. |
| `InvokeAsync<T>(Func<T> callback, CancellationToken cancellationToken = default)` | `Task<T>` | Enqueues a value-producing callback and returns its result asynchronously. |
| `InvokeAsync(Func<CancellationToken, Task> callback, CancellationToken cancellationToken = default)` | `Task` | Starts an asynchronous callback on the UI thread and propagates its eventual completion without blocking the drain. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `VerifyAccess()` | `InvalidOperationException` | The current managed thread is not the owner captured by the Relay. |
| `Post`, `InvokeAsync` | `ArgumentNullException` | `callback` is `null`. |

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `Cerneala.UI.Relay.UiRelayOptions`
- `Cerneala.UI.Elements.UIRoot`
- `UI/Relay/UiRelay.cs`
