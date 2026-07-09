# FloatMixer Class

## Definition
Namespace: `Cerneala.UI.Motion.Interpolation`

Assembly/Project: `Cerneala`

Source: [`UI/Motion/Interpolation/FloatMixer.cs`](../../UI/Motion/Interpolation/FloatMixer.cs)

Interpolates `float` values and provides vector-style arithmetic for motion calculations.

```csharp
public sealed class FloatMixer : ValueMixer<float>
```

Inheritance:
`object` -> `ValueMixer<float>` -> `FloatMixer`

Implements:
`IValueMixer`

## Examples

Interpolate between two `float` values:

```csharp
using Cerneala.UI.Motion.Interpolation;

FloatMixer mixer = new();

float halfway = mixer.Mix(10f, 20f, 0.5f);
bool closeEnough = mixer.EqualsWithinTolerance(halfway, 15f, 0.001f);
```

Resolve the built-in mixer from a registry:

```csharp
using Cerneala.UI.Motion.Interpolation;

ValueMixerRegistry registry = new();
registry.RegisterBuiltIns();

ValueMixer<float> mixer = registry.Resolve<float>();
float value = mixer.Mix(0f, 100f, 0.25f);
```

## Remarks

`FloatMixer` is the built-in `ValueMixer<float>` implementation used by the motion interpolation layer. `ValueMixerRegistry.RegisterBuiltIns` registers it for `float`, and the default motion system makes it available through the root mixer registry.

`Mix` linearly interpolates from `from` to `to`. Progress values less than or equal to `0` return `from`, and progress values greater than or equal to `1` return `to`, preserving exact endpoints for large values.

The mixer supports vector operations. `Add`, `Subtract`, `Scale`, and `Magnitude` operate directly on the numeric value, which allows motion code to compute deltas, velocities, and tolerances for `float` animations.

`EqualsWithinTolerance` treats the tolerance as a finite, non-negative absolute tolerance. Passing a negative, infinite, or `NaN` tolerance throws `ArgumentOutOfRangeException`.

## Constructors

| Name | Description |
| --- | --- |
| `FloatMixer()` | Initializes a new `FloatMixer` instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `SupportsVectorOperations` | `bool` | Gets `true`, indicating that arithmetic and magnitude operations are supported. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Add(float left, float right)` | `float` | Returns `left + right`. |
| `EqualsWithinTolerance(float left, float right, float tolerance)` | `bool` | Returns whether the absolute difference between two values is less than or equal to the tolerance. |
| `Magnitude(float value)` | `float` | Returns the absolute value. |
| `Mix(float from, float to, float progress)` | `float` | Returns the linearly interpolated value for `progress`, with exact endpoint clamping at `0` and `1`. |
| `Scale(float value, float scalar)` | `float` | Returns `value * scalar`. |
| `Subtract(float left, float right)` | `float` | Returns `left - right`. |

## Applies To

Cerneala motion interpolation APIs that animate `float` values, including built-in motion registrations for opacity and transform-related `UIElement` properties.

## See Also

- [`ValueMixer<T>`](../../UI/Motion/Interpolation/ValueMixer.cs)
- [`DoubleMixer`](Cerneala.UI.Motion.Interpolation.DoubleMixer.md)
- [`ValueMixerRegistry`](../../UI/Motion/Interpolation/ValueMixerRegistry.cs)
