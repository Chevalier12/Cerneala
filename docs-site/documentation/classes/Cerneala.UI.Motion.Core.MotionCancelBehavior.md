# MotionCancelBehavior Enum

## Definition
Namespace: `Cerneala.UI.Motion.Core`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Core/MotionCancellation.cs`

Specifies which value a motion value keeps when its active motion is canceled.

```csharp
public enum MotionCancelBehavior
```

## Examples

Cancel a running tween and jump back to its start value:

```csharp
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Specs;

MotionGraph graph = new();
MotionValue<float> value = graph.CreateValue(0f);
MotionHandle handle = value.AnimateTo(100f, Motion.Tween<float>(TimeSpan.FromMilliseconds(100)));
handle.Completed += (_, args) => Console.WriteLine($"{args.State} {args.CancelBehavior}");

graph.Tick(new MotionFrame(TimeSpan.Zero, TimeSpan.Zero, 0, MotionFrameReason.Scheduled, MotionFramePhase.BeforeRender));
graph.Tick(new MotionFrame(TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(50), 1, MotionFrameReason.Scheduled, MotionFramePhase.BeforeRender));

handle.Cancel(MotionCancelBehavior.Revert); // prints: Canceled Revert
Console.WriteLine(value.Current);           // 0
```

## Remarks

Pass a value to `MotionHandle.Cancel(MotionCancelBehavior)`. The parameter defaults to `KeepCurrent`. In the example above the value is about `87.8` when `Cancel` runs. The three behaviors then give:

| Behavior | `Current` after cancel | `Target` after cancel |
| --- | --- | --- |
| `KeepCurrent` | `87.8` (unchanged) | `87.8` |
| `Revert` | `0` (the start value) | `0` |
| `Complete` | `100` (the target) | `100` |

Every behavior stops the motion and finishes the handle as canceled. The `Completed` event is raised with `MotionCompletedEventArgs.State` equal to `MotionCompletionState.Canceled` and `CancelBehavior` equal to the behavior passed to `Cancel`. `Complete` jumps to the target but still reports `Canceled`; call `MotionHandle.Complete()` to finish a motion as `Completed`.

`Cancel` does nothing when the handle is no longer active, for example after it completed or was canceled.

The framework itself cancels with `KeepCurrent`. For example, starting a new `AnimateTo` on a value that is already animating, or calling `JumpTo`, cancels the previous handle with `KeepCurrent`.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `KeepCurrent` | `0` | Keeps the current sampled value and sets the target to it. This is the default. |
| `Revert` | `1` | Sets the value and the target back to the value the motion started from. |
| `Complete` | `2` | Sets the value to the motion's target. The handle still finishes as canceled. |

## Applies to

Cerneala motion cancellation.

## See also

- `Cerneala.UI.Motion.Core.MotionHandle`
- `Cerneala.UI.Motion.Core.MotionCompletedEventArgs`
- `Cerneala.UI.Motion.Core.MotionCompletionState`
- `Cerneala.UI.Motion.Core.MotionValue_T_`
