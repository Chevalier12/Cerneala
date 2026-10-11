# IValueMixer Interface

## Definition
Namespace: `Cerneala.UI.Motion.Interpolation`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Interpolation/IValueMixer.cs`

Untyped view of a value mixer, the object that interpolates and combines values of one type for motion.

```csharp
public interface IValueMixer
```

## Examples

Resolve the built-in mixer for `float` and use it without knowing the type at compile time:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Interpolation;

UIRoot root = new(100, 100);
IValueMixer mixer = root.Motion.Mixers.Resolve(typeof(float));

Console.WriteLine(mixer.ValueType.Name);           // Single
Console.WriteLine(mixer.MixUntyped(0f, 10f, 0.5f)); // 5
Console.WriteLine(mixer.AddUntyped(2f, 3f));        // 5
Console.WriteLine(mixer.MagnitudeUntyped(-4f));     // 4
```

## Remarks

`ValueMixerRegistry.Resolve(Type)` and `ValueMixerRegistry.TryResolve(Type, out IValueMixer)` return mixers as `IValueMixer`. `MotionSpec.CreateSamplerUntyped` takes one as a parameter.

Implement a mixer by deriving from `ValueMixer<T>`, not by implementing this interface directly. `ValueMixer<T>` implements every `IValueMixer` member by casting the arguments to `T` and calling its typed methods. `ValueMixerRegistry.Register<T>` accepts only `ValueMixer<T>`, and creating a sampler or a property animation with a mixer that does not derive from `ValueMixer<T>` throws an exception.

`SupportsVectorOperations` tells whether `Add`, `Subtract`, `Scale`, and `Magnitude` are meaningful for the type. Springs and decay require it and throw `InvalidOperationException` for a mixer that returns `false`.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `ValueType` | `Type` | Gets the value type this mixer handles. |
| `SupportsVectorOperations` | `bool` | Gets whether the mixer supports add, subtract, scale, and magnitude. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `MixUntyped(object? from, object? to, float progress)` | `object?` | Interpolates from `from` to `to` at `progress`. |
| `EqualsWithinToleranceUntyped(object? left, object? right, float tolerance)` | `bool` | Returns whether two values differ by at most `tolerance`. |
| `AddUntyped(object? left, object? right)` | `object?` | Adds two values. |
| `SubtractUntyped(object? left, object? right)` | `object?` | Subtracts `right` from `left`. |
| `ScaleUntyped(object? value, float scalar)` | `object?` | Multiplies a value by `scalar`. |
| `MagnitudeUntyped(object? value)` | `float` | Returns the size of a value, for example the absolute value of a `float`. |

## Implementations

`ValueMixer<T>` is the public base class. The built-in mixers derive from it: `FloatMixer`, `DoubleMixer`, `ColorMixer`, `BrushMixer`, `ThicknessMixer`, `DrawPointMixer`, `DrawSizeMixer`, `DrawRectMixer`, `TransformMixer`, and `Vector4Mixer`.

## Applies to

Cerneala motion interpolation.

## See also

- `Cerneala.UI.Motion.Interpolation.ValueMixer_T_`
- `Cerneala.UI.Motion.Interpolation.ValueMixerRegistry`
- `Cerneala.UI.Motion.Specs.MotionSpec`
