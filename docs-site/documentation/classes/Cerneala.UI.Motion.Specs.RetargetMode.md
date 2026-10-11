# RetargetMode Enum

## Definition
Namespace: `Cerneala.UI.Motion.Specs`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Specs/RetargetMode.cs`

Specifies how a new animation replaces an animation that is still running on the same value.

```csharp
public enum RetargetMode
```

## Examples

Retarget a running 100 ms linear tween halfway through, with each mode:

```csharp
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Specs;

MotionGraph graph = new();
MotionValue<float> value = graph.CreateValue(0f);
value.AnimateTo(100f, Motion.Tween<float>(TimeSpan.FromMilliseconds(100), Easings.Linear));
graph.Tick(new MotionFrame(TimeSpan.Zero, TimeSpan.Zero, 0, MotionFrameReason.Scheduled, MotionFramePhase.BeforeRender));
graph.Tick(new MotionFrame(TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(50), 1, MotionFrameReason.Scheduled, MotionFramePhase.BeforeRender));
// value.Current == 50

value.AnimateTo(
    200f,
    Motion.Tween<float>(TimeSpan.FromMilliseconds(100), Easings.Linear),
    new MotionStartOptions(RetargetMode: RetargetMode.PreserveProgress));
graph.Tick(new MotionFrame(TimeSpan.FromMilliseconds(60), TimeSpan.FromMilliseconds(10), 2, MotionFrameReason.Scheduled, MotionFramePhase.BeforeRender));

Console.WriteLine(value.Current); // 140
```

With `RetargetMode.Restart` instead, the same code prints `65`.

## Remarks

Pass the mode in `MotionStartOptions.RetargetMode` or `MotionPropertyStartOptions.RetargetMode`. Both default to `Restart`. In markup, the `@animate` option `retarget = PreserveProgress;` selects it; the accepted values are `Restart` and `PreserveProgress`.

The mode matters only when the value is already animating. In both modes the new animation starts from the current value, and the old handle finishes as canceled with `MotionCancelBehavior.KeepCurrent`. The difference is the starting time of the new animation:

- `Restart`: the new animation starts at time zero. In the example, at 10 ms it is 10% of the way from 50 to 200, so the value is `65`.
- `PreserveProgress`: the new animation is advanced by the time the old animation already ran. In the example, it starts as if 50 ms had passed, so at 10 ms later it is 60% of the way from 50 to 200, so the value is `140`.

When the running animation and the new animation are both springs, the spring continues from its current position and velocity in either mode. `SpringSpec<T>.VelocityMode` decides whether the velocity is kept.

`MotionSampler<T>.Retarget(T to, RetargetMode mode)` also takes a mode. The framework's `AnimateTo` path does not call it.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `Restart` | `0` | The new animation starts at time zero from the current value. The default. |
| `PreserveProgress` | `1` | The new animation starts from the current value, advanced by the time the replaced animation already ran. |

## Applies to

Cerneala motion retargeting.

## See also

- `Cerneala.UI.Motion.Core.MotionStartOptions`
- `Cerneala.UI.Motion.Properties.MotionPropertyStartOptions`
- `Cerneala.UI.Motion.Specs.SpringVelocityMode`
- `Cerneala.UI.Motion.Core.MotionValue_T_`
