# DoubleMixer Class

## Definition
Namespace: `Cerneala.UI.Motion.Interpolation`

Assembly/Project: `Cerneala`

Source: [`UI/Motion/Interpolation/DoubleMixer.cs`](../../UI/Motion/Interpolation/DoubleMixer.cs)

Interpolates `double` values and provides vector-style arithmetic for motion calculations.

```csharp
public sealed class DoubleMixer : ValueMixer<double>
```

Inheritance:
`object` -> `ValueMixer<double>` -> `DoubleMixer`

Implements:
`IValueMixer`

## Examples

Interpolate between two `double` values:

```csharp
using Cerneala.UI.Motion.Interpolation;

DoubleMixer mixer = new();

double halfway = mixer.Mix(10d, 20d, 0.5f);
bool closeEnough = mixer.EqualsWithinTolerance(halfway, 15d, 0.001f);
```

Resolve the built-in mixer from a registry:

```csharp
using Cerneala.UI.Motion.Interpolation;

ValueMixerRegistry registry = new();
registry.RegisterBuiltIns();

ValueMixer<double> mixer = registry.Resolve<double>();
double value = mixer.Mix(0d, 100d, 0.25f);
```

## Remarks

`DoubleMixer` is the built-in `ValueMixer<double>` implementation used by the motion interpolation layer. `ValueMixerRegistry.RegisterBuiltIns` registers it for `double`, and the default motion system makes it available through the root mixer registry.

`Mix` linearly interpolates from `from` to `to`. Progress values less than or equal to `0` return `from`, and progress values greater than or equal to `1` return `to`, preserving exact endpoints for large values.

The mixer supports vector operations. `Add`, `Subtract`, `Scale`, and `Magnitude` operate directly on the numeric value, which allows motion code to compute deltas, velocities, and tolerances for `double` animations.

`EqualsWithinTolerance` treats the `float` tolerance as a finite, non-negative absolute tolerance. Passing a negative, infinite, or `NaN` tolerance throws `ArgumentOutOfRangeException`.

## Constructors

| Name | Description |
| --- | --- |
| `DoubleMixer()` | Initializes a new `DoubleMixer` instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `SupportsVectorOperations` | `bool` | Gets `true`, indicating that arithmetic and magnitude operations are supported. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Add(double left, double right)` | `double` | Returns `left + right`. |
| `EqualsWithinTolerance(double left, double right, float tolerance)` | `bool` | Returns whether the absolute difference between two values is less than or equal to the tolerance. |
| `Magnitude(double value)` | `float` | Returns the absolute value as a `float`. |
| `Mix(double from, double to, float progress)` | `double` | Returns the linearly interpolated value for `progress`, with exact endpoint clamping at `0` and `1`. |
| `Scale(double value, float scalar)` | `double` | Returns `value * scalar`. |
| `Subtract(double left, double right)` | `double` | Returns `left - right`. |

## Applies To

Cerneala motion interpolation APIs that animate `double` values.

## See Also

- [`ValueMixer<T>`](../../UI/Motion/Interpolation/ValueMixer.cs)
- [`FloatMixer`](../../UI/Motion/Interpolation/FloatMixer.cs)
- [`ValueMixerRegistry`](../../UI/Motion/Interpolation/ValueMixerRegistry.cs)
