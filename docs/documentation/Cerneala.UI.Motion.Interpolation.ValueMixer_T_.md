# ValueMixer<T> Class

## Definition
Namespace: `Cerneala.UI.Motion.Interpolation`

Assembly/Project: `Cerneala`

Source: [`UI/Motion/Interpolation/ValueMixer.cs`](../../UI/Motion/Interpolation/ValueMixer.cs)

Provides the generic base class for value interpolation and optional vector-style motion arithmetic.

```csharp
public abstract class ValueMixer<T> : IValueMixer
```

Inheritance:
`object` -> `ValueMixer<T>`

Derived:
`ColorMixer`, `DoubleMixer`, `DrawPointMixer`, `DrawRectMixer`, `DrawSizeMixer`, `FloatMixer`, `ThicknessMixer`, `TransformMixer`

Implements:
`IValueMixer`

## Examples

Create a mixer for a custom value type:

```csharp
using Cerneala.UI.Motion.Interpolation;

public readonly record struct GaugeValue(float Amount);

public sealed class GaugeValueMixer : ValueMixer<GaugeValue>
{
    public override bool SupportsVectorOperations => true;

    public override GaugeValue Mix(GaugeValue from, GaugeValue to, float progress)
    {
        return new GaugeValue(from.Amount + ((to.Amount - from.Amount) * progress));
    }

    public override GaugeValue Add(GaugeValue left, GaugeValue right)
    {
        return new GaugeValue(left.Amount + right.Amount);
    }

    public override GaugeValue Subtract(GaugeValue left, GaugeValue right)
    {
        return new GaugeValue(left.Amount - right.Amount);
    }

    public override GaugeValue Scale(GaugeValue value, float scalar)
    {
        return new GaugeValue(value.Amount * scalar);
    }

    public override float Magnitude(GaugeValue value)
    {
        return MathF.Abs(value.Amount);
    }
}
```

Register and resolve the custom mixer:

```csharp
using Cerneala.UI.Motion.Interpolation;

ValueMixerRegistry registry = new();
registry.Register(new GaugeValueMixer());

ValueMixer<GaugeValue> mixer = registry.Resolve<GaugeValue>();
GaugeValue halfway = mixer.Mix(new GaugeValue(0), new GaugeValue(10), 0.5f);
```

## Remarks

`ValueMixer<T>` is the typed base for interpolation code used by motion specs, motion values, and `ValueMixerRegistry`. Implementations must provide `Mix(T, T, float)`, which returns the value at a supplied progress amount.

The base class also implements `IValueMixer` by casting untyped `object?` arguments to `T` and forwarding to the typed members. If an untyped value is not assignable to `T`, the forwarding methods throw `ArgumentException`. A `null` value is accepted only when `default(T)` is `null`.

Vector operations are opt-in. The base `SupportsVectorOperations` value is `false`, and `Add`, `Subtract`, `Scale`, and `Magnitude` throw `InvalidOperationException` until a derived mixer overrides them. Motion specs that need arithmetic, such as spring or decay motion, rely on mixers that set `SupportsVectorOperations` to `true` and implement those operations.

`EqualsWithinTolerance` validates that `tolerance` is finite and non-negative, then uses `EqualityComparer<T>.Default`. Derived mixers can override it to provide component-wise or numeric tolerance checks.

Derived mixers can use the protected `Lerp` helpers for endpoint-preserving `float` or `double` interpolation. The helpers return `from` when progress is less than or equal to `0`, return `to` when progress is greater than or equal to `1`, and linearly interpolate between those endpoints otherwise.

## Constructors

| Name | Description |
| --- | --- |
| `ValueMixer<T>()` | Initializes the base mixer state for a derived mixer. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `ValueType` | `Type` | Gets `typeof(T)` for registry and untyped motion APIs. |
| `SupportsVectorOperations` | `bool` | Gets whether the mixer supports `Add`, `Subtract`, `Scale`, and `Magnitude`; the base value is `false`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Add(T left, T right)` | `T` | Adds two values when vector operations are supported; the base implementation throws `InvalidOperationException`. |
| `AddUntyped(object? left, object? right)` | `object?` | Casts both arguments to `T` and delegates to `Add`. |
| `EqualsWithinTolerance(T left, T right, float tolerance)` | `bool` | Returns whether two typed values are equal within a finite, non-negative tolerance. |
| `EqualsWithinToleranceUntyped(object? left, object? right, float tolerance)` | `bool` | Casts both arguments to `T` and delegates to `EqualsWithinTolerance`. |
| `Magnitude(T value)` | `float` | Returns the magnitude of a value when vector operations are supported; the base implementation throws `InvalidOperationException`. |
| `MagnitudeUntyped(object? value)` | `float` | Casts the argument to `T` and delegates to `Magnitude`. |
| `Mix(T from, T to, float progress)` | `T` | When implemented in a derived class, returns the interpolated value for `progress`. |
| `MixUntyped(object? from, object? to, float progress)` | `object?` | Casts both arguments to `T` and delegates to `Mix`. |
| `Scale(T value, float scalar)` | `T` | Scales a value when vector operations are supported; the base implementation throws `InvalidOperationException`. |
| `ScaleUntyped(object? value, float scalar)` | `object?` | Casts the argument to `T` and delegates to `Scale`. |
| `Subtract(T left, T right)` | `T` | Subtracts two values when vector operations are supported; the base implementation throws `InvalidOperationException`. |
| `SubtractUntyped(object? left, object? right)` | `object?` | Casts both arguments to `T` and delegates to `Subtract`. |

## Applies To

Cerneala motion interpolation, motion specs, and value mixer registry APIs.

## See Also

- [`IValueMixer`](../../UI/Motion/Interpolation/IValueMixer.cs)
- [`ValueMixerRegistry`](../../UI/Motion/Interpolation/ValueMixerRegistry.cs)
- [`FloatMixer`](../../UI/Motion/Interpolation/FloatMixer.cs)
- [`DoubleMixer`](../../UI/Motion/Interpolation/DoubleMixer.cs)
