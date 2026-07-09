# CubicBezierEasing Class

## Definition
Namespace: `Cerneala.UI.Motion.Specs`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Specs/CubicBezierEasing.cs`

Applies a CSS-style cubic Bezier easing curve to normalized motion progress.

```csharp
public sealed class CubicBezierEasing : IEasing
```

Inheritance:
`object` -> `CubicBezierEasing`

Implements:
`IEasing`

## Examples

Create a custom easing curve and transform normalized progress values:

```csharp
using Cerneala.UI.Motion.Specs;

CubicBezierEasing easing = new(0.4f, 0, 0.2f, 1);

float start = easing.Transform(0);      // 0
float middle = easing.Transform(0.5f);
float end = easing.Transform(1);        // 1
```

Use a cubic Bezier easing with a tween spec:

```csharp
using Cerneala.UI.Motion.Specs;

IEasing easing = new CubicBezierEasing(0, 0, 0.2f, 1);
TweenSpec<float> spec = new(TimeSpan.FromMilliseconds(200), easing);
```

## Remarks

`CubicBezierEasing` maps an input progress value to an eased output value. The input passed to `Transform` is clamped to `[0, 1]`; `NaN` input returns `0`. Exact endpoint inputs return exact endpoint outputs: `0` maps to `0`, and `1` maps to `1`.

The constructor requires `x1` and `x2` to be finite values in `[0, 1]` so the curve can be solved as a function of progress. `y1` and `y2` may be outside `[0, 1]`, but they must be finite. The transformed result is clamped to `[0, 1]` after sampling the curve.

Internally, `Transform` solves the Bezier parameter for the requested x progress with Newton iteration and falls back to bisection when the derivative is too small or the Newton step leaves the valid range.

The static `Easings` presets use `CubicBezierEasing` for common motion curves such as `Standard`, `Emphasized`, `EaseIn`, `EaseOut`, `EaseInOut`, and `Sharp`.

## Constructors

| Name | Description |
| --- | --- |
| `CubicBezierEasing(float x1, float y1, float x2, float y2)` | Initializes a cubic Bezier easing with two control points. `x1` and `x2` must be in `[0, 1]`; `y1` and `y2` must be finite. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Transform(float progress)` | `float` | Converts normalized progress to eased progress. Clamps the input and returned value to `[0, 1]`, and returns `0` for `NaN` input. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `CubicBezierEasing(float x1, float y1, float x2, float y2)` | `ArgumentOutOfRangeException` | `x1` or `x2` is `NaN` or outside `[0, 1]`; `y1` or `y2` is not finite. |

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `UI/Motion/Specs/CubicBezierEasing.cs`
- `UI/Motion/Specs/IEasing.cs`
- `UI/Motion/Specs/Easings.cs`
- `UI/Motion/Specs/TweenSpec.cs`
