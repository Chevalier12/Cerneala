# MotionGroupHandle Class

## Definition
Namespace: `Cerneala.UI.Motion.Core`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Core/MotionGroupHandle.cs`

Represents the observable and cancelable handle returned by grouped motion operations.

```csharp
public sealed class MotionGroupHandle
```

Inheritance:
`object` -> `MotionGroupHandle`

## Examples

Create a parallel group and wait until all child handles complete:

```csharp
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Specs;

ManualMotionTimeline timeline = new();
MotionValue<float> opacity = timeline.CreateValue(1f);
MotionValue<float> scale = timeline.CreateValue(1f);

MotionHandle fade = opacity.AnimateTo(0f, Motion.Tween<float>(TimeSpan.FromMilliseconds(150)));
MotionHandle grow = scale.AnimateTo(1.1f, Motion.Tween<float>(TimeSpan.FromMilliseconds(150)));

MotionGroupHandle group = MotionGroup.Parallel(fade, grow);

await group.Completion;
```

Cancel a sequence through its group handle:

```csharp
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Specs;

ManualMotionTimeline timeline = new();
MotionValue<float> opacity = timeline.CreateValue(1f);
MotionValue<float> scale = timeline.CreateValue(1f);

MotionGroupHandle group = MotionSequence.Start(
    () => opacity.AnimateTo(0f, Motion.Tween<float>(TimeSpan.FromMilliseconds(150))),
    () => scale.AnimateTo(1.1f, Motion.Tween<float>(TimeSpan.FromMilliseconds(150))));

group.Cancel();
```

## Remarks

`MotionGroupHandle` is created by motion grouping APIs such as `MotionGroup.Parallel` and `MotionSequence.Start`. Callers use it to observe whether the grouped operation completed or was canceled, await the grouped completion task through `Completion`, or cancel the grouped operation through `Cancel`.

`Cancel` is idempotent after the handle reaches a terminal state. If the group is already completed or canceled, calling `Cancel` returns without changing state. Otherwise it sets `IsCanceled`, invokes the cancellation action supplied by the grouping API, and cancels `Completion`.

Grouped APIs decide what cancellation means for their children. `MotionGroup.Parallel` cancels every child handle. `MotionSequence.Start` cancels the active child handle and prevents later sequence steps from starting.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `IsCompleted` | `bool` | Gets whether the grouped operation completed successfully. |
| `IsCanceled` | `bool` | Gets whether the grouped operation was canceled. |
| `Completion` | `ValueTask` | Gets a value task that completes when the group completes and is canceled when the group is canceled. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Cancel()` | `void` | Cancels the grouped operation unless the handle has already completed or been canceled. |

## Applies to

Cerneala motion core grouped motion handles.

## See also

- `Cerneala.UI.Motion.Core.MotionGroup`
- `Cerneala.UI.Motion.Core.MotionSequence`
- `Cerneala.UI.Motion.Core.MotionHandle`
