# DrawRectMixer Class

## Definition
Namespace: `Cerneala.UI.Motion.Interpolation`

Assembly/Project: `Cerneala`

Source: [`UI/Motion/Interpolation/DrawRectMixer.cs`](../../UI/Motion/Interpolation/DrawRectMixer.cs)

Interpolates `DrawRect` values for motion animations.

```csharp
public sealed class DrawRectMixer : ValueMixer<DrawRect>
```

Inheritance:
`object` -> `ValueMixer<DrawRect>` -> `DrawRectMixer`

Implements:
`IValueMixer` through `ValueMixer<DrawRect>`

## Examples

Interpolate between two drawing rectangles:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Motion.Interpolation;

DrawRectMixer mixer = new();

DrawRect mixed = mixer.Mix(
    new DrawRect(0, 10, 20, 30),
    new DrawRect(100, 110, 120, 130),
    0.25f);
```

Resolve the built-in mixer from a registry:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Motion.Interpolation;

ValueMixerRegistry registry = new();
registry.RegisterBuiltIns();

ValueMixer<DrawRect> mixer = registry.Resolve<DrawRect>();
DrawRect value = mixer.Mix(new DrawRect(0, 0, 10, 10), new DrawRect(20, 30, 40, 50), 0.5f);
```

## Remarks

`DrawRectMixer` is the built-in `ValueMixer<DrawRect>` implementation used by the motion interpolation layer. `ValueMixerRegistry.RegisterBuiltIns` registers it for `DrawRect`, and the default motion system exposes it through the root mixer registry.

`Mix` linearly interpolates `X`, `Y`, `Width`, and `Height` independently. Progress values less than or equal to `0` return the source rectangle, and progress values greater than or equal to `1` return the target rectangle, preserving exact endpoints.

`DrawRectMixer` keeps the default `ValueMixer<DrawRect>` vector behavior. `SupportsVectorOperations` is `false`, and inherited vector methods such as `Add`, `Subtract`, `Scale`, and `Magnitude` throw `InvalidOperationException`.

`EqualsWithinTolerance` compares each rectangle component with a finite, non-negative absolute tolerance. Passing a negative, infinite, or `NaN` tolerance throws `ArgumentOutOfRangeException`.

The interpolated values are passed to the `DrawRect` constructor, so invalid rectangle coordinates or sizes are validated by `DrawRect`.

## Constructors

| Name | Description |
| --- | --- |
| `DrawRectMixer()` | Initializes a new `DrawRectMixer` instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `SupportsVectorOperations` | `bool` | Gets `false`; rectangle mixing does not expose vector operations. Inherited from `ValueMixer<DrawRect>`. |
| `ValueType` | `Type` | Gets `typeof(DrawRect)`. Inherited from `ValueMixer<DrawRect>`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `EqualsWithinTolerance(DrawRect left, DrawRect right, float tolerance)` | `bool` | Returns whether the absolute differences for `X`, `Y`, `Width`, and `Height` are all less than or equal to the tolerance. |
| `Mix(DrawRect from, DrawRect to, float progress)` | `DrawRect` | Returns the linearly interpolated rectangle for `progress`, with exact endpoint clamping at `0` and `1`. |
| `Add(DrawRect left, DrawRect right)` | `DrawRect` | Throws `InvalidOperationException` because vector operations are not supported. Inherited from `ValueMixer<DrawRect>`. |
| `AddUntyped(object? left, object? right)` | `object?` | Casts the inputs to `DrawRect` and delegates to `Add`. Inherited from `ValueMixer<DrawRect>`. |
| `EqualsWithinToleranceUntyped(object? left, object? right, float tolerance)` | `bool` | Casts the inputs to `DrawRect` and delegates to `EqualsWithinTolerance`. Inherited from `ValueMixer<DrawRect>`. |
| `Magnitude(DrawRect value)` | `float` | Throws `InvalidOperationException` because vector operations are not supported. Inherited from `ValueMixer<DrawRect>`. |
| `MagnitudeUntyped(object? value)` | `float` | Casts the input to `DrawRect` and delegates to `Magnitude`. Inherited from `ValueMixer<DrawRect>`. |
| `MixUntyped(object? from, object? to, float progress)` | `object?` | Casts the inputs to `DrawRect` and delegates to `Mix`. Inherited from `ValueMixer<DrawRect>`. |
| `Scale(DrawRect value, float scalar)` | `DrawRect` | Throws `InvalidOperationException` because vector operations are not supported. Inherited from `ValueMixer<DrawRect>`. |
| `ScaleUntyped(object? value, float scalar)` | `object?` | Casts the input to `DrawRect` and delegates to `Scale`. Inherited from `ValueMixer<DrawRect>`. |
| `Subtract(DrawRect left, DrawRect right)` | `DrawRect` | Throws `InvalidOperationException` because vector operations are not supported. Inherited from `ValueMixer<DrawRect>`. |
| `SubtractUntyped(object? left, object? right)` | `object?` | Casts the inputs to `DrawRect` and delegates to `Subtract`. Inherited from `ValueMixer<DrawRect>`. |

## Applies To

Cerneala motion interpolation APIs that animate `DrawRect` values.

## See Also

- [`DrawRect`](Cerneala.Drawing.DrawRect.md)
- [`ValueMixer<T>`](../../UI/Motion/Interpolation/ValueMixer.cs)
- [`ValueMixerRegistry`](../../UI/Motion/Interpolation/ValueMixerRegistry.cs)
