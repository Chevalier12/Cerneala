# MotionHandle Class

## Definition
Namespace: `Cerneala.UI.Motion.Core`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Core/MotionHandle.cs`

Represents a running motion operation that can be canceled, completed immediately, disposed, and observed for terminal completion.

```csharp
public sealed class MotionHandle : IDisposable
```

Inheritance:
`object` -> `MotionHandle`

Implements:
`IDisposable`

## Examples

Start a graph-owned motion value animation, subscribe to completion, then cancel it while keeping the sampled value reached so far:

```csharp
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Specs;

MotionGraph graph = new();
MotionValue<double> opacity = graph.CreateValue(0d);

MotionHandle handle = opacity.AnimateTo(
    1d,
    Motion.Tween<double>(TimeSpan.FromMilliseconds(150)));

handle.Completed += (_, args) =>
{
    bool wasCanceled = args.IsCanceled;
};

handle.Cancel(MotionCancelBehavior.KeepCurrent);

try
{
    await handle.Completion;
}
catch (OperationCanceledException)
{
    // The completion task is canceled when the handle is canceled.
}
```

## Remarks

`MotionHandle` is created by motion APIs such as `MotionValue<T>.AnimateTo`, motion property bindings, and facade animation builders. The constructor is internal, so application code receives handles from those APIs rather than creating them directly.

The handle has one active state and two terminal states. `IsActive` is `true` only while the handle is not completed, not canceled, and not disposed. Natural completion or `Complete()` marks the handle as completed, resolves `Completion` successfully, raises `Completed`, and clears the stored callbacks. `Cancel()` marks the handle as canceled through the owning motion object, cancels `Completion`, raises `Completed`, and applies the selected `MotionCancelBehavior`.

`Complete()` asks the owner to jump the motion to its target and finish as completed. `Cancel(MotionCancelBehavior.Complete)` may also jump to the target, but it still reports the terminal state as canceled and cancels the `Completion` task.

`Dispose()` releases completion event subscribers and owner callbacks. When the handle is still active, disposal cancels the owning motion with `MotionCancelBehavior.KeepCurrent` without firing `Completed`; this is useful when caller code wants to stop observing the handle without keeping callback targets alive.

An active handle returned by `MotionValue<T>.AnimateTo` uses its owning graph's thread affinity for `Cancel()`, `Complete()`, and `Dispose()`. A standalone graph captures its creating thread; a root-owned graph uses the UI thread that owns `UIRoot.Relay`. A call from another thread throws `InvalidOperationException` before changing the value, graph registration, handle state, completion task, or callbacks. Marshal lifecycle requests to the owning thread through the root's Relay when applicable. Terminal handles retain their no-op cancellation and completion behavior, and disposal remains idempotent.

Handlers added to `Completed` after the handle is completed, canceled, or disposed are ignored. If a `Completed` handler throws, the handle still invokes the remaining handlers, clears all subscribers and callbacks, then rethrows the first captured exception.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `IsActive` | `bool` | Gets whether the handle is neither completed, canceled, nor disposed. |
| `IsCompleted` | `bool` | Gets whether the handle reached successful completion. |
| `IsCanceled` | `bool` | Gets whether the handle reached canceled completion. |
| `Completion` | `ValueTask` | Gets a value task that completes successfully when the motion completes and is canceled when the motion is canceled. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Cancel(MotionCancelBehavior behavior = MotionCancelBehavior.KeepCurrent)` | `void` | Requests cancellation of the active motion using the supplied cancellation behavior. Does nothing when the handle is no longer active. |
| `Complete()` | `void` | Requests immediate successful completion of the active motion. Does nothing when the handle is no longer active. |
| `Dispose()` | `void` | Releases event subscribers and owner callbacks, canceling active owner state without firing `Completed`. |

## Events

| Name | Event Type | Description |
| --- | --- | --- |
| `Completed` | `EventHandler<MotionCompletedEventArgs>?` | Raised when an active handle completes or is canceled through normal handle operations. Subscribers are cleared after the terminal transition. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Cancel`, `Complete`, `Dispose` | `InvalidOperationException` | An active handle returned by `MotionValue<T>.AnimateTo` is called from a thread other than its owning graph's thread. The call is rejected without changing motion or handle state. |

## Applies to

Cerneala motion core animation handles.

## See also

- `Cerneala.UI.Motion.Core.MotionValue<T>`
- `Cerneala.UI.Motion.Core.MotionCompletedEventArgs`
- `Cerneala.UI.Motion.Core.MotionCancelBehavior`
- `Cerneala.UI.Motion.Core.MotionGroup`
- `Cerneala.UI.Motion.Core.MotionCompletionSource`
