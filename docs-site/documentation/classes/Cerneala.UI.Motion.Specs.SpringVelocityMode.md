# SpringVelocityMode Enum

## Definition
Namespace: `Cerneala.UI.Motion.Specs`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Specs/SpringVelocityMode.cs`

Specifies whether a running spring keeps its velocity when it gets a new target.

```csharp
public enum SpringVelocityMode
```

## Examples

Send a moving spring back to `0` and compare the velocity right after the retarget:

```csharp
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Specs;

MotionGraph graph = new();
MotionValue<float> value = graph.CreateValue(0f);
SpringSpec<float> spring = new(velocityMode: SpringVelocityMode.Reset);

value.AnimateTo(100f, spring);
graph.Tick(new MotionFrame(TimeSpan.Zero, TimeSpan.Zero, 0, MotionFrameReason.Scheduled, MotionFramePhase.BeforeRender));
graph.Tick(new MotionFrame(TimeSpan.FromMilliseconds(16), TimeSpan.FromMilliseconds(16), 1, MotionFrameReason.Scheduled, MotionFramePhase.BeforeRender));
graph.Tick(new MotionFrame(TimeSpan.FromMilliseconds(32), TimeSpan.FromMilliseconds(16), 2, MotionFrameReason.Scheduled, MotionFramePhase.BeforeRender));
// value.Velocity is about 948.8 units per second

value.AnimateTo(0f, spring);
Console.WriteLine(value.Velocity); // MotionVelocity { Value = 0 }
```

With `SpringVelocityMode.Preserve`, the velocity after the retarget stays about `948.8`, so the value keeps moving up for a moment before it turns back.

## Remarks

Pass the mode to the `SpringSpec<T>` constructor (`velocityMode` parameter). The default is `Preserve`. In markup, the `<Spring>` resource takes it as the `VelocityMode` attribute with `Preserve` or `Reset`; a missing attribute means `Preserve`, and any other value is reported as an invalid property value.

The mode is used when a value that a spring is animating gets a new spring animation. The running spring continues from its current position. The mode of the new `SpringSpec<T>` decides the velocity: `Preserve` keeps it and `Reset` sets it to zero. This happens with both `RetargetMode.Restart` and `RetargetMode.PreserveProgress`.

When the running animation is not a spring, or the new animation is not a spring, the new animation starts fresh and this mode is not used. `SpringSpec<Transform>` applies the mode in the same way.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `Preserve` | `0` | Keep the current velocity when the spring is retargeted. The default. |
| `Reset` | `1` | Set the velocity to zero when the spring is retargeted. |

## Applies to

Cerneala spring motion.

## See also

- `Cerneala.UI.Motion.Specs.SpringSpec_T_`
- `Cerneala.UI.Motion.Specs.RetargetMode`
- `Cerneala.UI.Motion.Core.MotionValue_T_`
