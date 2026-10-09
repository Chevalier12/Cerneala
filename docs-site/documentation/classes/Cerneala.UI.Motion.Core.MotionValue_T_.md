# MotionValue<T> Class

## Definition
Namespace: `Cerneala.UI.Motion.Core`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Core/MotionValue{T}.cs`

Represents a graph-bound mutable value that can jump immediately or animate toward a target through a `MotionSpec<T>`.

```csharp
public sealed class MotionValue<T> : MotionValue
```

Inheritance:
`object` -> `MotionValue` -> `MotionValue<T>`

## Examples

Create a value, observe changes, animate it, then tick the owning graph:

```csharp
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Specs;

MotionGraph graph = new();
MotionValue<double> opacity = graph.CreateValue(0d);

using IDisposable subscription = opacity.Subscribe(change =>
{
    double previous = change.OldValue;
    double next = change.NewValue;
});

MotionHandle handle = opacity.AnimateTo(
    1d,
    Motion.Tween<double>(TimeSpan.FromMilliseconds(150)));

MotionFrame frame = new(
    TimeSpan.FromMilliseconds(150),
    TimeSpan.FromMilliseconds(150),
    FrameIndex: 1,
    MotionFrameReason.Manual,
    MotionFramePhase.BeforeRender);

graph.Tick(frame);

double current = opacity.Current;
bool completed = handle.IsCompleted;
```

## Remarks

`MotionValue<T>` instances are created by `MotionGraph.CreateValue<T>`. The constructor is internal, so callers receive values from the graph rather than constructing them directly. The value stores the current value, target value, animation start value, active sampler, optional velocity, and the active `MotionHandle`.

`AnimateTo` verifies access through the owning graph, creates or continues a sampler using the supplied `MotionSpec<T>`, records the requested target, and registers an internal motion node with the graph while the animation is active. When the sampler completes naturally, the value applies the sampler's final `Current` value, updates `Target` to that completed value, records completion diagnostics when diagnostics are configured, finishes the handle as completed, and unregisters its node. This preserves specifications whose natural endpoint can differ from the originally requested target, such as an even-cycle `PingPongSpec<T>`.

Starting an animation with equal or higher priority cancels the previous active handle with `MotionCancelBehavior.KeepCurrent`. A lower-priority request is rejected without changing the active animation and returns an already canceled handle. When `MotionStartOptions.RetargetMode` is `RetargetMode.PreserveProgress`, the previous elapsed animation time is reused with the new sampler when the active motion can be detached safely; otherwise the operation falls back to a restart.

An active spring instead retains its current position when retargeted to another spring. The incoming spec supplies the new spring parameters and determines whether velocity is preserved or reset. Both retarget modes continue without elapsed-time replay: for a spring, `PreserveProgress` preserves motion state rather than elapsed time. If cancellation callbacks change the current value, the normal new-sampler path starts from that value instead. A built-in transform spring retains its decomposed component positions and velocities internally; `Velocity` is `null` because component velocity cannot be represented losslessly as a transform matrix.

`JumpTo` cancels active motion, sets the target and animation start to the supplied value, and notifies subscribers only when the mixed value differs from `Current`. Change notifications are delivered to a snapshot of the subscription list, so listeners may cancel, complete, or start motion while a notification is being processed.

The active handle returned by `AnimateTo` also verifies graph access before `Cancel`, `Complete`, or `Dispose` changes any motion or handle state. Rejected cross-thread calls leave the animation intact: the current value, target, sampled velocity, graph registration, completion task, and callbacks are preserved. Subsequent owner-thread ticks and lifecycle calls continue normally.

`Velocity` is read from the active sampler after graph ticks and after a successful spring-state handoff, so retargeting exposes the preserved or reset velocity immediately. If the sampler does not expose velocity and throws `InvalidOperationException`, `Velocity` is reported as `null`.

When a live `MotionPropertyBinding<T>` connects this value to a UI property, `AnimateTo` (including retargets) and `JumpTo` validate the destination through that binding before changing active motion. Invalid destinations throw synchronously, using the property's coercion, metadata validation, and owner-specific checks. An unbound motion value has no UI-property validator. Disposing a binding removes its validation; if multiple bindings share a value, each live binding validates the destination.

Sampler values remain unclamped in `Current`. The property's write path skips intermediate samples whose coerced value fails its metadata validator or is rejected with `ArgumentException` by the owner validation hook, retaining the last valid animated property value without stopping motion. Thus `Current` can differ from a bound property during overshoot. Ordinary subscriber exceptions still propagate; invalid-sample skipping is not a general exception handler.

If a subscriber throws while a terminal value is applied, the exception propagates to the caller. The motion still becomes terminal and unregisters its graph node before the exception escapes, so the handle is not left active.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Current` | `T` | Gets the currently applied value. |
| `Target` | `T` | Gets the value the active motion is targeting. After natural completion, gets the sampler's completed value; after `JumpTo`, gets the supplied value. |
| `IsAnimating` | `bool` | Gets whether the value has an active sampler and active handle. |
| `Velocity` | `MotionVelocity<T>?` | Gets the velocity last read from the sampler after sampling or a successful spring-state handoff; otherwise `null`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `AnimateTo(T target, MotionSpec<T> spec, MotionStartOptions? options = null)` | `MotionHandle` | Starts animating from `Current` toward `target`, or returns a canceled handle when an active motion has higher priority. |
| `JumpTo(T value)` | `void` | Cancels active motion, sets the target to `value`, and applies the value immediately. |
| `Subscribe(Action<MotionValueChanged<T>> listener)` | `IDisposable` | Adds a listener for value changes and returns a subscription that removes the listener when disposed. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `AnimateTo` | `ArgumentNullException` | `spec` is `null`. |
| `AnimateTo`, `JumpTo` | `InvalidOperationException` | The current thread is not the thread that created the owning standalone graph, or the UI thread that owns its root. |
| `Subscribe` | `ArgumentNullException` | `listener` is `null`. |
| `AnimateTo`, `JumpTo` | `ArgumentException` | A live UI-property binding rejects the destination after coercion. Owner-specific validation can throw its existing exceptions. |

## Applies to

Cerneala motion core graph values.

## See also

- `Cerneala.UI.Motion.Core.MotionGraph`
- `Cerneala.UI.Motion.Core.MotionValue`
- `Cerneala.UI.Motion.Core.MotionValueChanged<T>`
- `Cerneala.UI.Motion.Core.MotionHandle`
- `Cerneala.UI.Motion.Specs.MotionSpec<T>`
- `Cerneala.UI.Motion.Interpolation.ValueMixer<T>`
