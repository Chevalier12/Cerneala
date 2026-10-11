# FillMode Enum

## Definition
Namespace: `Cerneala.UI.Motion.Specs`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Specs/FillMode.cs`

Specifies which value a delayed tween reports while it waits for its delay.

```csharp
public enum FillMode
```

## Examples

Create a delayed tween with an explicit fill mode:

```csharp
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Specs;

MotionGraph graph = new();
MotionValue<float> value = graph.CreateValue(5f);
TweenSpec<float> spec = new(
    TimeSpan.FromMilliseconds(100),
    Easings.Linear,
    delay: TimeSpan.FromMilliseconds(100),
    fillMode: FillMode.None);

value.AnimateTo(10f, spec);
graph.Tick(new MotionFrame(TimeSpan.Zero, TimeSpan.Zero, 0, MotionFrameReason.Scheduled, MotionFramePhase.BeforeRender));
graph.Tick(new MotionFrame(TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(50), 1, MotionFrameReason.Scheduled, MotionFramePhase.BeforeRender));

Console.WriteLine(value.Current); // 5, the start value, for every FillMode
```

## Remarks

Set the fill mode with the `TweenSpec<T>` constructor or with `TweenSpec<T>.WithFillMode(FillMode)`. The default is `Both`. `Motion.Tween<T>` always creates a tween with `Both`.

In markup, a named `<Tween>` resource accepts a `FillMode` attribute with the values `None`, `Backwards`, `Forwards`, and `Both`. Inline `Tween(...)` values in `@animate` and the inner tween of `Repeat` and `PingPong` do not accept a fill mode.

The fill mode is read only while a delayed tween is still inside its delay. With `Backwards` or `Both`, the sampler sets its value to the start value. With `None` or `Forwards`, it keeps its current value. A tween sampler starts at its start value and does not change it before the delay ends, so in the current implementation all four values give the same result: the start value during the delay, and the target after completion. In the example above, `value.Current` is `5` during the delay for every fill mode.

`FillMode` does not control whether the final value stays on a property after the animation ends. That is controlled by `MotionPropertyStartOptions.HoldOnComplete` (in markup, `holdOnComplete`).

The fill mode has no effect on a tween without a delay. `RepeatSpec<T>` and `PingPongSpec<T>` use only the duration and easing of their inner tween, so they ignore its delay and fill mode.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `None` | `0` | During the delay, the sampler keeps its current value. |
| `Backwards` | `1` | During the delay, the sampler reports the start value. |
| `Forwards` | `2` | During the delay, the sampler keeps its current value. There is no separate after-completion behavior; a completed tween always reports its target. |
| `Both` | `3` | During the delay, the sampler reports the start value. This is the default. |

## Applies to

Cerneala tween motion specifications.

## See also

- `Cerneala.UI.Motion.Specs.TweenSpec_T_`
- `Cerneala.UI.Motion.Specs.Motion`
- `Cerneala.UI.Motion.Properties.MotionPropertyStartOptions`
