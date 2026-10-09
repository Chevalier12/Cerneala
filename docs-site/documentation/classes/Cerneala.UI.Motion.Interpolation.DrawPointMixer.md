# DrawPointMixer Class

## Definition
Namespace: `Cerneala.UI.Motion.Interpolation`

Assembly/Project: `Cerneala`

Source: [`UI/Motion/Interpolation/DrawPointMixer.cs`](https://github.com/Chevalier12/Cerneala/blob/master/UI/Motion/Interpolation/DrawPointMixer.cs)

Interpolates `DrawPoint` values and provides vector-style arithmetic for motion calculations.

```csharp
public sealed class DrawPointMixer : ValueMixer<DrawPoint>
```

Inheritance:
`object` -> `ValueMixer<DrawPoint>` -> `DrawPointMixer`

Implements:
`IValueMixer` through `ValueMixer<DrawPoint>`

## Examples

Interpolate between two drawing points:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Motion.Interpolation;

DrawPointMixer mixer = new();

DrawPoint halfway = mixer.Mix(new DrawPoint(0, 10), new DrawPoint(100, 50), 0.5f);
bool closeEnough = mixer.EqualsWithinTolerance(halfway, new DrawPoint(50, 30), 0.001f);
```

Resolve the built-in mixer from a registry:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Motion.Interpolation;

ValueMixerRegistry registry = new();
registry.RegisterBuiltIns();

ValueMixer<DrawPoint> mixer = registry.Resolve<DrawPoint>();
DrawPoint value = mixer.Mix(new DrawPoint(0, 0), new DrawPoint(20, 40), 0.25f);
```

## Remarks

`DrawPointMixer` is the built-in `ValueMixer<DrawPoint>` implementation used by the motion interpolation layer. `ValueMixerRegistry.RegisterBuiltIns` registers it for `DrawPoint`, and the default motion system exposes it through the root mixer registry.

`Mix` linearly interpolates the `X` and `Y` coordinates independently. Progress exactly `0` returns the source coordinate values, and progress exactly `1` returns the target coordinate values, preserving exact endpoints for large values. Progress outside `[0, 1]` extrapolates linearly.

The mixer supports vector operations. `Add`, `Subtract`, `Scale`, and `Magnitude` operate on the point as a two-dimensional vector, which lets motion code compute deltas, velocities, and tolerances for animated drawing positions.

`EqualsWithinTolerance` compares each coordinate with a finite, non-negative absolute tolerance. Passing a negative, infinite, or `NaN` tolerance throws `ArgumentOutOfRangeException`.

## Constructors

| Name | Description |
| --- | --- |
| `DrawPointMixer()` | Initializes a new `DrawPointMixer` instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `SupportsVectorOperations` | `bool` | Gets `true`, indicating that arithmetic and magnitude operations are supported. |
| `ValueType` | `Type` | Gets `typeof(DrawPoint)`. Inherited from `ValueMixer<DrawPoint>`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Add(DrawPoint left, DrawPoint right)` | `DrawPoint` | Returns a point whose coordinates are `left.X + right.X` and `left.Y + right.Y`. |
| `EqualsWithinTolerance(DrawPoint left, DrawPoint right, float tolerance)` | `bool` | Returns whether both coordinate differences are less than or equal to the tolerance. |
| `Magnitude(DrawPoint value)` | `float` | Returns the Euclidean length of the point vector. |
| `Mix(DrawPoint from, DrawPoint to, float progress)` | `DrawPoint` | Returns the linearly interpolated point for `progress`, with linear extrapolation outside `[0, 1]` and exact endpoints at progress `0` and `1`. |
| `Scale(DrawPoint value, float scalar)` | `DrawPoint` | Returns a point whose coordinates are multiplied by `scalar`. |
| `Subtract(DrawPoint left, DrawPoint right)` | `DrawPoint` | Returns a point whose coordinates are `left.X - right.X` and `left.Y - right.Y`. |
| `AddUntyped(object? left, object? right)` | `object?` | Casts the inputs to `DrawPoint` and delegates to `Add`. Inherited from `ValueMixer<DrawPoint>`. |
| `EqualsWithinToleranceUntyped(object? left, object? right, float tolerance)` | `bool` | Casts the inputs to `DrawPoint` and delegates to `EqualsWithinTolerance`. Inherited from `ValueMixer<DrawPoint>`. |
| `MagnitudeUntyped(object? value)` | `float` | Casts the input to `DrawPoint` and delegates to `Magnitude`. Inherited from `ValueMixer<DrawPoint>`. |
| `MixUntyped(object? from, object? to, float progress)` | `object?` | Casts the inputs to `DrawPoint` and delegates to `Mix`. Inherited from `ValueMixer<DrawPoint>`. |
| `ScaleUntyped(object? value, float scalar)` | `object?` | Casts the input to `DrawPoint` and delegates to `Scale`. Inherited from `ValueMixer<DrawPoint>`. |
| `SubtractUntyped(object? left, object? right)` | `object?` | Casts the inputs to `DrawPoint` and delegates to `Subtract`. Inherited from `ValueMixer<DrawPoint>`. |

## Applies To

Cerneala motion interpolation APIs that animate `DrawPoint` values.

## See Also

- [`DrawPoint`](Cerneala.Drawing.DrawPoint.md)
- [`ValueMixer<T>`](https://github.com/Chevalier12/Cerneala/blob/master/UI/Motion/Interpolation/ValueMixer.cs)
- [`ValueMixerRegistry`](https://github.com/Chevalier12/Cerneala/blob/master/UI/Motion/Interpolation/ValueMixerRegistry.cs)
