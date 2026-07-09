# DrawSizeMixer Class

## Definition
Namespace: `Cerneala.UI.Motion.Interpolation`

Assembly/Project: `Cerneala`

Source: [`UI/Motion/Interpolation/DrawSizeMixer.cs`](../../UI/Motion/Interpolation/DrawSizeMixer.cs)

Interpolates `DrawSize` values and provides vector-style arithmetic for motion calculations.

```csharp
public sealed class DrawSizeMixer : ValueMixer<DrawSize>
```

Inheritance:
`object` -> `ValueMixer<DrawSize>` -> `DrawSizeMixer`

Implements:
`IValueMixer` through `ValueMixer<DrawSize>`

## Examples

Interpolate between two drawing sizes:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Motion.Interpolation;

DrawSizeMixer mixer = new();

DrawSize halfway = mixer.Mix(new DrawSize(10, 20), new DrawSize(30, 60), 0.5f);
bool closeEnough = mixer.EqualsWithinTolerance(halfway, new DrawSize(20, 40), 0.001f);
```

Resolve the built-in mixer from a registry:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Motion.Interpolation;

ValueMixerRegistry registry = new();
registry.RegisterBuiltIns();

ValueMixer<DrawSize> mixer = registry.Resolve<DrawSize>();
DrawSize value = mixer.Mix(new DrawSize(0, 0), new DrawSize(100, 50), 0.25f);
```

## Remarks

`DrawSizeMixer` is the built-in `ValueMixer<DrawSize>` implementation used by the motion interpolation layer. `ValueMixerRegistry.RegisterBuiltIns` registers it for `DrawSize`, and the default motion system exposes it through the root mixer registry.

`Mix` linearly interpolates the `Width` and `Height` components independently. Progress values less than or equal to `0` return the source component values, and progress values greater than or equal to `1` return the target component values, preserving exact endpoints for large values.

The mixer supports vector operations. `Add`, `Subtract`, `Scale`, and `Magnitude` operate on the size as a two-dimensional vector, which lets motion code compute deltas, velocities, and tolerances for animated drawing sizes.

`EqualsWithinTolerance` compares both components with a finite, non-negative absolute tolerance. Passing a negative, infinite, or `NaN` tolerance throws `ArgumentOutOfRangeException`.

`DrawSize` validates that its `Width` and `Height` values are finite, so results that construct a `DrawSize` also follow that validation.

## Constructors

| Name | Description |
| --- | --- |
| `DrawSizeMixer()` | Initializes a new `DrawSizeMixer` instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `SupportsVectorOperations` | `bool` | Gets `true`, indicating that arithmetic and magnitude operations are supported. |
| `ValueType` | `Type` | Gets `typeof(DrawSize)`. Inherited from `ValueMixer<DrawSize>`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Add(DrawSize left, DrawSize right)` | `DrawSize` | Returns a size whose components are `left.Width + right.Width` and `left.Height + right.Height`. |
| `EqualsWithinTolerance(DrawSize left, DrawSize right, float tolerance)` | `bool` | Returns whether both component differences are less than or equal to the tolerance. |
| `Magnitude(DrawSize value)` | `float` | Returns the Euclidean length of the size vector. |
| `Mix(DrawSize from, DrawSize to, float progress)` | `DrawSize` | Returns the linearly interpolated size for `progress`, with exact endpoint clamping at `0` and `1`. |
| `Scale(DrawSize value, float scalar)` | `DrawSize` | Returns a size whose components are multiplied by `scalar`. |
| `Subtract(DrawSize left, DrawSize right)` | `DrawSize` | Returns a size whose components are `left.Width - right.Width` and `left.Height - right.Height`. |
| `AddUntyped(object? left, object? right)` | `object?` | Casts the inputs to `DrawSize` and delegates to `Add`. Inherited from `ValueMixer<DrawSize>`. |
| `EqualsWithinToleranceUntyped(object? left, object? right, float tolerance)` | `bool` | Casts the inputs to `DrawSize` and delegates to `EqualsWithinTolerance`. Inherited from `ValueMixer<DrawSize>`. |
| `MagnitudeUntyped(object? value)` | `float` | Casts the input to `DrawSize` and delegates to `Magnitude`. Inherited from `ValueMixer<DrawSize>`. |
| `MixUntyped(object? from, object? to, float progress)` | `object?` | Casts the inputs to `DrawSize` and delegates to `Mix`. Inherited from `ValueMixer<DrawSize>`. |
| `ScaleUntyped(object? value, float scalar)` | `object?` | Casts the input to `DrawSize` and delegates to `Scale`. Inherited from `ValueMixer<DrawSize>`. |
| `SubtractUntyped(object? left, object? right)` | `object?` | Casts the inputs to `DrawSize` and delegates to `Subtract`. Inherited from `ValueMixer<DrawSize>`. |

## Applies To

Cerneala motion interpolation APIs that animate `DrawSize` values.

## See Also

- [`DrawSize`](../../UI/Drawing/DrawSize.cs)
- [`DrawPointMixer`](Cerneala.UI.Motion.Interpolation.DrawPointMixer.md)
- [`ValueMixer<T>`](../../UI/Motion/Interpolation/ValueMixer.cs)
- [`ValueMixerRegistry`](../../UI/Motion/Interpolation/ValueMixerRegistry.cs)
