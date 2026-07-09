# ThicknessMixer Class

## Definition
Namespace: `Cerneala.UI.Motion.Interpolation`

Assembly/Project: `Cerneala`

Source: [`UI/Motion/Interpolation/ThicknessMixer.cs`](../../UI/Motion/Interpolation/ThicknessMixer.cs)

Interpolates `Thickness` values and provides vector-style arithmetic for motion calculations.

```csharp
public sealed class ThicknessMixer : ValueMixer<Thickness>
```

Inheritance:
`object` -> `ValueMixer<Thickness>` -> `ThicknessMixer`

Implements:
`IValueMixer` through `ValueMixer<Thickness>`

## Examples

Interpolate between two thickness values:

```csharp
using Cerneala.UI.Layout;
using Cerneala.UI.Motion.Interpolation;

ThicknessMixer mixer = new();

Thickness mixed = mixer.Mix(
    new Thickness(0, 10, 20, 30),
    new Thickness(100, 110, 120, 130),
    0.25f);
```

Resolve the built-in mixer from a registry:

```csharp
using Cerneala.UI.Layout;
using Cerneala.UI.Motion.Interpolation;

ValueMixerRegistry registry = new();
registry.RegisterBuiltIns();

ValueMixer<Thickness> mixer = registry.Resolve<Thickness>();
Thickness value = mixer.Mix(new Thickness(0), new Thickness(8, 12, 8, 12), 0.5f);
```

## Remarks

`ThicknessMixer` is the built-in `ValueMixer<Thickness>` implementation used by the motion interpolation layer. `ValueMixerRegistry.RegisterBuiltIns` registers it for `Thickness`, and the default motion system exposes it through the root mixer registry.

`Mix` linearly interpolates `Left`, `Top`, `Right`, and `Bottom` independently. Progress values less than or equal to `0` return the source component values, and progress values greater than or equal to `1` return the target component values, preserving exact endpoints for large values.

The mixer supports vector operations. `Add`, `Subtract`, `Scale`, and `Magnitude` operate on the four thickness edges as a vector, which lets motion code compute deltas, velocities, and tolerances for animated layout spacing.

`EqualsWithinTolerance` compares each edge with a finite, non-negative absolute tolerance. Passing a negative, infinite, or `NaN` tolerance throws `ArgumentOutOfRangeException`.

Built-in animatable properties use this mixer for `Control.BorderThicknessProperty`, `Control.PaddingProperty`, and `UIElement.MarginProperty`. Those built-in property registrations use a 180 ms tween and are not marked safe for implicit animation.

## Constructors

| Name | Description |
| --- | --- |
| `ThicknessMixer()` | Initializes a new `ThicknessMixer` instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `SupportsVectorOperations` | `bool` | Gets `true`, indicating that arithmetic and magnitude operations are supported. |
| `ValueType` | `Type` | Gets `typeof(Thickness)`. Inherited from `ValueMixer<Thickness>`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Add(Thickness left, Thickness right)` | `Thickness` | Returns a thickness whose edges are the sums of the matching edges from `left` and `right`. |
| `EqualsWithinTolerance(Thickness left, Thickness right, float tolerance)` | `bool` | Returns whether the absolute differences for `Left`, `Top`, `Right`, and `Bottom` are all less than or equal to the tolerance. |
| `Magnitude(Thickness value)` | `float` | Returns the Euclidean length of the four-edge thickness vector. |
| `Mix(Thickness from, Thickness to, float progress)` | `Thickness` | Returns the linearly interpolated thickness for `progress`, with exact endpoint clamping at `0` and `1`. |
| `Scale(Thickness value, float scalar)` | `Thickness` | Returns a thickness whose edges are multiplied by `scalar`. |
| `Subtract(Thickness left, Thickness right)` | `Thickness` | Returns a thickness whose edges are the differences of the matching edges from `left` and `right`. |
| `AddUntyped(object? left, object? right)` | `object?` | Casts the inputs to `Thickness` and delegates to `Add`. Inherited from `ValueMixer<Thickness>`. |
| `EqualsWithinToleranceUntyped(object? left, object? right, float tolerance)` | `bool` | Casts the inputs to `Thickness` and delegates to `EqualsWithinTolerance`. Inherited from `ValueMixer<Thickness>`. |
| `MagnitudeUntyped(object? value)` | `float` | Casts the input to `Thickness` and delegates to `Magnitude`. Inherited from `ValueMixer<Thickness>`. |
| `MixUntyped(object? from, object? to, float progress)` | `object?` | Casts the inputs to `Thickness` and delegates to `Mix`. Inherited from `ValueMixer<Thickness>`. |
| `ScaleUntyped(object? value, float scalar)` | `object?` | Casts the input to `Thickness` and delegates to `Scale`. Inherited from `ValueMixer<Thickness>`. |
| `SubtractUntyped(object? left, object? right)` | `object?` | Casts the inputs to `Thickness` and delegates to `Subtract`. Inherited from `ValueMixer<Thickness>`. |

## Applies To

Cerneala motion interpolation APIs that animate `Thickness` values.

## See Also

- [`Thickness`](../../UI/Layout/Thickness.cs)
- [`ValueMixer<T>`](../../UI/Motion/Interpolation/ValueMixer.cs)
- [`ValueMixerRegistry`](../../UI/Motion/Interpolation/ValueMixerRegistry.cs)
- [`AnimatablePropertyRegistry`](../../UI/Motion/Properties/AnimatablePropertyRegistry.cs)
