# Easings.LinearEasing Class

## Definition
Namespace: `Cerneala.UI.Motion.Specs`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Specs/Easings.cs`

Provides the private linear easing implementation exposed through `Easings.Linear`.

```csharp
private sealed class Easings.LinearEasing : IEasing
```

Inheritance:
`object` -> `Easings.LinearEasing`

Implements:
`IEasing`

## Examples

Use the public `Easings.Linear` instance when a tween should advance with unclipped linear progress:

```csharp
using Cerneala.UI.Motion.Specs;

TweenSpec<float> spec = Motion.Tween<float>(
    TimeSpan.FromMilliseconds(100),
    Easings.Linear);

float midpoint = Easings.Linear.Transform(0.5f);
```

## Remarks

`Easings.LinearEasing` is a private nested implementation detail of `Easings`. Callers do not construct it directly; they use the singleton `IEasing` exposed by `Easings.Linear`.

`Transform(float progress)` returns `0` when `progress` is `NaN`. Otherwise, it clamps the input progress to the inclusive `0` to `1` range and returns that clamped value. This makes the easing linear for normal in-range progress while preventing out-of-range values from flowing into motion mixers.

Keyframe sampling uses `Easings.Linear` as the fallback easing when a keyframe segment does not provide its own easing.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Transform(float progress)` | `float` | Returns `0` for `NaN`; otherwise returns `progress` clamped to the inclusive `0` to `1` range. |

## Applies to

Cerneala motion easing specifications.

## See also

- `Easings`
- `IEasing`
- `TweenSpec<T>`
- `MotionKeyframe<T>`
