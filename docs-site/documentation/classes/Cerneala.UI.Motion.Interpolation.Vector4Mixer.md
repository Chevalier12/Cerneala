# Vector4Mixer Class

## Definition

Namespace: `Cerneala.UI.Motion.Interpolation`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Interpolation/Vector4Mixer.cs`

Interpolates and performs vector operations on `System.Numerics.Vector4` values.

```csharp
public sealed class Vector4Mixer : ValueMixer<Vector4>
```

## Remarks

`Mix` interpolates each component independently and extrapolates linearly for progress outside `[0, 1]`. Progress exactly `0` returns the source components and progress exactly `1` returns the target components, preserving exact endpoints even for large values.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `SupportsVectorOperations` | `bool` | Always `true`; springs and other vector-based specs can use this mixer. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Mix(Vector4 from, Vector4 to, float progress)` | `Vector4` | Linearly interpolates or extrapolates the four components, preserving exact endpoints at progress `0` and `1`. |
| `EqualsWithinTolerance(Vector4 left, Vector4 right, float tolerance)` | `bool` | Tests every component against the supplied non-negative tolerance. |
| `Add(Vector4 left, Vector4 right)` | `Vector4` | Adds two vectors. |
| `Subtract(Vector4 left, Vector4 right)` | `Vector4` | Subtracts one vector from another. |
| `Scale(Vector4 value, float scalar)` | `Vector4` | Multiplies a vector by a scalar. |
| `Magnitude(Vector4 value)` | `float` | Returns the vector length. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `EqualsWithinTolerance` | `ArgumentOutOfRangeException` | `tolerance` is negative. |

## See also

- `System.Numerics.Vector4`
- `Cerneala.UI.Motion.Interpolation.ValueMixer<T>`
- `Cerneala.UI.Motion.Interpolation.ValueMixerRegistry`
