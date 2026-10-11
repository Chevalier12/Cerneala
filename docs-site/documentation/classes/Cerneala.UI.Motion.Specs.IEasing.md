# IEasing Interface

## Definition
Namespace: `Cerneala.UI.Motion.Specs`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Specs/IEasing.cs`

Maps linear animation progress to eased progress.

```csharp
public interface IEasing
```

## Examples

Use built-in easings, and pass a custom easing to a tween:

```csharp
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Specs;

Console.WriteLine(Easings.Linear.Transform(0.25f));                   // 0.25
Console.WriteLine(new StepEasing(4).Transform(0.6f));                // 0.5
Console.WriteLine(new CubicBezierEasing(0.3f, 0f, 0.2f, 1f).Transform(0.5f)); // about 0.7972

MotionGraph graph = new();
MotionValue<float> value = graph.CreateValue(0f);
value.AnimateTo(100f, new TweenSpec<float>(TimeSpan.FromMilliseconds(100), new HoldThenJumpEasing()));
graph.Tick(new MotionFrame(TimeSpan.Zero, TimeSpan.Zero, 0, MotionFrameReason.Scheduled, MotionFramePhase.BeforeRender));
graph.Tick(new MotionFrame(TimeSpan.FromMilliseconds(25), TimeSpan.FromMilliseconds(25), 1, MotionFrameReason.Scheduled, MotionFramePhase.BeforeRender));

Console.WriteLine(value.Current); // 0

sealed class HoldThenJumpEasing : IEasing
{
    public float Transform(float progress) => progress < 0.5f ? 0f : 1f;
}
```

## Remarks

`Transform` receives the linear progress of an animation segment and returns the progress used to mix between the start and end values. For example, at 25% of a 0 to 100 tween, `Easings.Linear` returns `0.25` and the value is `25`; the custom easing in the example returns `0` and the value stays `0`.

Easings are used by:

- `TweenSpec<T>` and `Motion.Tween<T>`. A `null` easing becomes `Easings.Standard`.
- `RepeatSpec<T>` and `PingPongSpec<T>`, through the easing of their inner tween.
- `MotionKeyframe<T>.Easing`, for the segment that starts at that keyframe. A `null` keyframe easing means linear.

A tween calls `Transform` only while its raw progress is below `1`. When the tween completes, the value is set exactly to the target, whatever the easing returns. A keyframe segment marked `Hold` does not call `Transform`.

The mixer does not clamp the result of `Transform`. A result above `1` or below `0` extrapolates beyond the start and end values. For example, `ColorMixer` clamps color channels to `0`..`255` and `DrawRectMixer` clamps width and height at `0`, but a float value moves past its target.

Built-in implementations:

| Easing | Type | Behavior |
| --- | --- | --- |
| `Easings.Linear` | Private class | Clamps progress to `0`..`1` and returns it. `NaN` returns `0`. |
| `Easings.Standard` | `CubicBezierEasing(0.2, 0, 0, 1)` | Default tween easing. |
| `Easings.Emphasized` | `CubicBezierEasing(0.2, 0, 0, 1)` | Same curve as `Standard`, separate instance. |
| `Easings.EaseIn` | `CubicBezierEasing(0.4, 0, 1, 1)` | |
| `Easings.EaseOut` | `CubicBezierEasing(0, 0, 0.2, 1)` | |
| `Easings.EaseInOut` | `CubicBezierEasing(0.4, 0, 0.2, 1)` | |
| `Easings.Sharp` | `CubicBezierEasing(0.4, 0, 0.6, 1)` | |
| `StepEasing` | Public class | Jumps between a fixed number of steps; see `StepPosition`. |
| `CubicBezierEasing` | Public class | Custom cubic Bézier curve. |

In markup, easings are named by their `Easings` property name, for example `Tween(200ms, EaseOut)`. `Step(count, position)` is accepted only as a keyframe segment easing. `CubicBezierEasing` and custom `IEasing` implementations have no markup syntax.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Transform(float progress)` | `float` | Returns the eased progress for the supplied linear progress. |

## Applies to

Cerneala tween, repeat, ping-pong, and keyframe motion specifications.

## See also

- `Cerneala.UI.Motion.Specs.Easings`
- `Cerneala.UI.Motion.Specs.CubicBezierEasing`
- `Cerneala.UI.Motion.Specs.StepEasing`
- `Cerneala.UI.Motion.Specs.StepPosition`
- `Cerneala.UI.Motion.Specs.TweenSpec_T_`
- `Cerneala.UI.Motion.Specs.MotionKeyframe_T_`
