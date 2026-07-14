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
    frameIndex: 1,
    MotionFrameReason.Manual,
    MotionFramePhase.BeforeRender);

graph.Tick(frame);

double current = opacity.Current;
bool completed = handle.IsCompleted;
```

## Remarks

`MotionValue<T>` instances are created by `MotionGraph.CreateValue<T>`. The constructor is internal, so callers receive values from the graph rather than constructing them directly. The value stores the current value, target value, animation start value, active sampler, optional velocity, and the active `MotionHandle`.

`AnimateTo` verifies access through the owning graph, creates a sampler from the supplied `MotionSpec<T>`, records the new target, and registers an internal motion node with the graph while the animation is active. When the sampler completes naturally, the value applies the final target, records completion diagnostics when diagnostics are configured, finishes the handle as completed, and unregisters its node.

Starting a new animation cancels the previous active handle with `MotionCancelBehavior.KeepCurrent`. When `MotionStartOptions.RetargetMode` is `RetargetMode.PreserveProgress`, the previous elapsed animation time is reused with the new sampler when the active motion can be detached safely; otherwise the operation falls back to a restart.

`JumpTo` cancels active motion, sets the target and animation start to the supplied value, and notifies subscribers only when the mixed value differs from `Current`. Change notifications are delivered to a snapshot of the subscription list, so listeners may cancel, complete, or start motion while a notification is being processed.

`Velocity` is read from the active sampler after graph ticks. If the sampler does not expose velocity and throws `InvalidOperationException`, `Velocity` is reported as `null`.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Current` | `T` | Gets the currently applied value. |
| `Target` | `T` | Gets the value the current motion is targeting, or the last value supplied to `JumpTo`. |
| `IsAnimating` | `bool` | Gets whether the value has an active sampler and active handle. |
| `Velocity` | `MotionVelocity<T>?` | Gets the most recently sampled velocity when the active sampler provides one; otherwise `null`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `AnimateTo(T target, MotionSpec<T> spec, MotionStartOptions? options = null)` | `MotionHandle` | Starts animating from `Current` toward `target` with `spec`, optionally using start options for retargeting, priority, and debug naming. |
| `JumpTo(T value)` | `void` | Cancels active motion, sets the target to `value`, and applies the value immediately. |
| `Subscribe(Action<MotionValueChanged<T>> listener)` | `IDisposable` | Adds a listener for value changes and returns a subscription that removes the listener when disposed. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `AnimateTo` | `ArgumentNullException` | `spec` is `null`. |
| `AnimateTo`, `JumpTo` | `InvalidOperationException` | The current thread is not the thread that created the owning standalone graph, or the UI thread that owns its root. |
| `Subscribe` | `ArgumentNullException` | `listener` is `null`. |

## Applies to

Cerneala motion core graph values.

## See also

- `Cerneala.UI.Motion.Core.MotionGraph`
- `Cerneala.UI.Motion.Core.MotionValue`
- `Cerneala.UI.Motion.Core.MotionValueChanged<T>`
- `Cerneala.UI.Motion.Core.MotionHandle`
- `Cerneala.UI.Motion.Specs.MotionSpec<T>`
- `Cerneala.UI.Motion.Interpolation.ValueMixer<T>`
