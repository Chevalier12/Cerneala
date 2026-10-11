# MotionCompletionState Enum

## Definition
Namespace: `Cerneala.UI.Motion.Core`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Core/MotionCancellation.cs`

Reports how a motion handle finished.

```csharp
public enum MotionCompletionState
```

## Examples

Finish a motion immediately and read the state from the `Completed` event:

```csharp
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Specs;

MotionGraph graph = new();
MotionValue<float> value = graph.CreateValue(0f);
MotionHandle handle = value.AnimateTo(1f, Motion.Tween<float>(TimeSpan.FromMilliseconds(300)));
handle.Completed += (_, args) =>
    Console.WriteLine($"{args.State}, CancelBehavior is null: {args.CancelBehavior is null}");

handle.Complete(); // prints: Completed, CancelBehavior is null: True
Console.WriteLine(value.Current); // 1
```

## Remarks

`MotionCompletedEventArgs.State` carries this value. `MotionCompletedEventArgs.IsCanceled` is `true` when `State` is `Canceled`.

A handle finishes as `Completed` when its motion reaches the end, or when `MotionHandle.Complete()` is called. In both cases `CancelBehavior` is `null`.

A handle finishes as `Canceled` when `MotionHandle.Cancel` is called, and also in these cases:

- `AnimateTo` starts a new motion on a value that is already animating. The previous handle is canceled with `MotionCancelBehavior.KeepCurrent`.
- `MotionValue<T>.JumpTo` is called while a motion is active.

Two cases finish a handle as canceled without raising `Completed`: an `AnimateTo` request rejected by priority returns a handle that is already canceled, and disposing the motion value cancels its active handle silently.

The `Completed` event accepts subscribers only while the handle is active. A handler added after the handle completed or was canceled is never called.

Framework consumers use the state to decide what happens next. For example, `MotionSequence` starts the next step after `Completed` and cancels the rest of the sequence after `Canceled`, and motion property bindings restore the base value only after `Completed`.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `Completed` | `0` | The motion reached its end, or `MotionHandle.Complete()` was called. |
| `Canceled` | `1` | The motion was canceled or replaced. `MotionCompletedEventArgs.CancelBehavior` names the behavior. |

## Applies to

Cerneala motion handles.

## See also

- `Cerneala.UI.Motion.Core.MotionHandle`
- `Cerneala.UI.Motion.Core.MotionCompletedEventArgs`
- `Cerneala.UI.Motion.Core.MotionCancelBehavior`
- `Cerneala.UI.Motion.Core.MotionSequence`
