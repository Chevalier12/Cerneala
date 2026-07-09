# MotionGroup Class

## Definition
Namespace: `Cerneala.UI.Motion.Core`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Core/MotionGroup.cs`

Provides factory methods for combining motion handles into a single group handle.

```csharp
public static class MotionGroup
```

Inheritance:
`object` -> `MotionGroup`

## Examples

Group two motion values so caller code can observe or cancel both handles through one `MotionGroupHandle`:

```csharp
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Specs;

ManualMotionTimeline timeline = new();
MotionValue<float> opacity = timeline.CreateValue(1f);
MotionValue<float> scale = timeline.CreateValue(1f);

MotionHandle fade = opacity.AnimateTo(0f, Motion.Tween<float>(TimeSpan.FromMilliseconds(150)));
MotionHandle grow = scale.AnimateTo(1.1f, Motion.Tween<float>(TimeSpan.FromMilliseconds(150)));

MotionGroupHandle group = MotionGroup.Parallel(fade, grow);

group.Cancel();
```

## Remarks

`MotionGroup` currently exposes parallel grouping. `Parallel` creates a `MotionGroupHandle` that completes after every supplied `MotionHandle` raises `Completed`. A group with no children completes immediately.

Canceling the returned group handle calls `Cancel()` on each child handle. Child cancellation does not complete the group, because `MotionHandle.Completed` subscribers are invoked only for handlers attached before a handle reaches a terminal state, and the group listens for the child completion event.

`Parallel` validates that the `children` array itself is not `null`. Individual child entries are not prevalidated; a `null` entry will fail when the method tries to attach or cancel child handles.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Parallel(params MotionHandle[] children)` | `MotionGroupHandle` | Creates a group handle that completes after all supplied child handles complete and cancels all children when the group is canceled. |

## Applies to

Cerneala motion core handle grouping.

## See also

- `Cerneala.UI.Motion.Core.MotionGroupHandle`
- `Cerneala.UI.Motion.Core.MotionHandle`
- `Cerneala.UI.Motion.Core.MotionSequence`
