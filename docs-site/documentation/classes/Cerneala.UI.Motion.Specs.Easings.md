# Easings Class

## Definition
Namespace: `Cerneala.UI.Motion.Specs`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Specs/Easings.cs`

Provides shared easing instances for common Cerneala UI motion curves.

```csharp
public static class Easings
```

Inheritance:
`object` -> `Easings`

## Examples

Use the linear preset when a tween should advance proportionally with elapsed time:

```csharp
using Cerneala.UI.Motion.Interpolation;
using Cerneala.UI.Motion.Specs;

TweenSpec<float> spec = Motion.Tween<float>(TimeSpan.FromMilliseconds(100), Easings.Linear);
MotionSampler<float> sampler = spec.CreateSampler(0, 10, new FloatMixer(), new MotionSpecContext());

sampler.Advance(TimeSpan.FromMilliseconds(50));
float value = sampler.Current; // 5
```

Use a named curve with an untyped motion specification:

```csharp
using Cerneala.UI.Motion.Specs;

MotionSpec enter = Motion.Tween(TimeSpan.FromMilliseconds(180), Easings.EaseOut);
MotionSpec exit = Motion.Tween(TimeSpan.FromMilliseconds(140), Easings.EaseIn);
```

## Remarks

`Easings` is a static catalog of reusable `IEasing` instances. Each property returns the same easing object for the lifetime of the application.

`Linear` clamps normalized progress to `[0, 1]` and returns `0` for `NaN` input. The other presets are `CubicBezierEasing` instances; they clamp input and output to `[0, 1]`, return exact endpoint values for `0` and `1`, and return `0` for `NaN` input.

`TweenSpec<T>` uses `Easings.Standard` when no easing is supplied. The default theme motion tokens also use these presets for instant, fast, standard, emphasized, enter, and exit motion curves.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Linear` | `IEasing` | Gets a linear easing that maps normalized progress directly to clamped progress. |
| `Standard` | `IEasing` | Gets a cubic Bezier easing with control points `(0.2, 0)` and `(0, 1)`. Used as the default easing for `TweenSpec<T>`. |
| `Emphasized` | `IEasing` | Gets a cubic Bezier easing with control points `(0.2, 0)` and `(0, 1)`. |
| `EaseIn` | `IEasing` | Gets a cubic Bezier easing with control points `(0.4, 0)` and `(1, 1)`. |
| `EaseOut` | `IEasing` | Gets a cubic Bezier easing with control points `(0, 0)` and `(0.2, 1)`. |
| `EaseInOut` | `IEasing` | Gets a cubic Bezier easing with control points `(0.4, 0)` and `(0.2, 1)`. |
| `Sharp` | `IEasing` | Gets a cubic Bezier easing with control points `(0.4, 0)` and `(0.6, 1)`. |

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `Cerneala.UI.Motion.Specs.IEasing`
- `Cerneala.UI.Motion.Specs.CubicBezierEasing`
- `Cerneala.UI.Motion.Specs.Motion`
- `Cerneala.UI.Motion.Specs.TweenSpec<T>`
- `Cerneala.UI.Motion.States.ThemeMotionTokens`
