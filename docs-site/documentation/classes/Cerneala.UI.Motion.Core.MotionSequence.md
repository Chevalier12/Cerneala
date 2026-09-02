# MotionSequence Class

## Definition
Namespace: `Cerneala.UI.Motion.Core`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Core/MotionSequence.cs`

Provides a factory method for running motion handles one after another.

```csharp
public static class MotionSequence
```

Inheritance:
`object` -> `MotionSequence`

## Examples

Start a sequence where each motion begins only after the previous handle completes:

```csharp
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Specs;

ManualMotionTimeline timeline = new();
MotionValue<float> opacity = timeline.CreateValue(1f);
MotionValue<float> scale = timeline.CreateValue(1f);

MotionGroupHandle sequence = MotionSequence.Start(
    () => opacity.AnimateTo(0f, Motion.Tween<float>(TimeSpan.FromMilliseconds(150))),
    () => scale.AnimateTo(1.1f, Motion.Tween<float>(TimeSpan.FromMilliseconds(150))));

await sequence.Completion;
```

Cancel the active sequence step and prevent later steps from starting:

```csharp
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Specs;

ManualMotionTimeline timeline = new();
MotionValue<float> opacity = timeline.CreateValue(1f);
MotionValue<float> scale = timeline.CreateValue(1f);

MotionGroupHandle sequence = MotionSequence.Start(
    () => opacity.AnimateTo(0f, Motion.Tween<float>(TimeSpan.FromMilliseconds(150))),
    () => scale.AnimateTo(1.1f, Motion.Tween<float>(TimeSpan.FromMilliseconds(150))));

sequence.Cancel();
```

## Remarks

`MotionSequence.Start` accepts factories instead of pre-created handles. The first factory is invoked immediately. Each later factory is invoked only after the active `MotionHandle` reaches a completed, non-canceled state. A handle that is already terminal when returned by its factory is observed immediately.

The returned `MotionGroupHandle` completes after every step has completed successfully. If `Start` is called with no steps, the group completes immediately.

Canceling the returned group handle cancels only the currently active handle. The sequence checks the group cancellation state before starting the next step, so canceling the group also prevents future steps from being created. If a child handle is canceled independently, the group becomes canceled and later steps are not started.

`Start` validates that the `steps` array itself is not `null`. Individual delegates are invoked lazily and are not prevalidated. A factory that returns `null` causes `InvalidOperationException`.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Start(params Func<MotionHandle>[] steps)` | `MotionGroupHandle` | Starts the first step immediately, starts each following step after the active handle completes, and returns a handle for observing or canceling the sequence. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Start` | `ArgumentNullException` | `steps` is `null`. |
| `Start` | `InvalidOperationException` | A step factory returns `null`. |

## Applies to

Cerneala motion core sequential motion orchestration.

## See also

- `Cerneala.UI.Motion.Core.MotionGroupHandle`
- `Cerneala.UI.Motion.Core.MotionHandle`
- `Cerneala.UI.Motion.Core.MotionGroup`
