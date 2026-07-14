# MotionCompletedEventArgs Class

## Definition
Namespace: `Cerneala.UI.Motion.Core`

Assembly/Project: `Cerneala`

Source: [UI/Motion/Core/MotionHandle.cs](../../UI/Motion/Core/MotionHandle.cs)

Provides data for the `MotionHandle.Completed` event.

```csharp
public sealed class MotionCompletedEventArgs : EventArgs
```

Inheritance:
`object` -> `EventArgs` -> `MotionCompletedEventArgs`

## Examples

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
    if (args.IsCanceled)
    {
        MotionCancelBehavior? behavior = args.CancelBehavior;
        return;
    }

    MotionCompletionState state = args.State;
};

handle.Complete();
```

## Remarks

`MotionCompletedEventArgs` is raised by `MotionHandle` when an active motion reaches a terminal state through normal completion or cancellation.

When a motion completes successfully, `State` is `MotionCompletionState.Completed`, `IsCanceled` is `false`, and `CancelBehavior` is `null`. When a motion is canceled, `State` is `MotionCompletionState.Canceled`, `IsCanceled` is `true`, and `CancelBehavior` contains the `MotionCancelBehavior` used for the cancellation.

The constructor stores the supplied state and cancellation behavior without additional validation. `IsCanceled` is derived from `State`.

## Constructors

| Name | Description |
| --- | --- |
| `MotionCompletedEventArgs(MotionCompletionState state, MotionCancelBehavior? cancelBehavior)` | Initializes a new instance with the terminal motion state and optional cancellation behavior. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `State` | `MotionCompletionState` | Gets whether the motion completed successfully or was canceled. |
| `CancelBehavior` | `MotionCancelBehavior?` | Gets the cancellation behavior used for a canceled motion, or `null` for successful completion. |
| `IsCanceled` | `bool` | Gets whether `State` is `MotionCompletionState.Canceled`. |

## Applies to

Cerneala motion core animation handles.

## See also

- `Cerneala.UI.Motion.Core.MotionHandle`
- `Cerneala.UI.Motion.Core.MotionCancelBehavior`
- `Cerneala.UI.Motion.Core.MotionCompletionState`
