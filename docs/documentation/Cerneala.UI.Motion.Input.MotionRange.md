# MotionRange Struct

## Definition

Namespace: `Cerneala.UI.Motion.Input`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Input/MotionRange.cs`

Maps a float input interval to a float output interval with clamped linear interpolation.

```csharp
public readonly record struct MotionRange(float InputStart, float InputEnd, float OutputStart, float OutputEnd)
```

Inheritance:
`ValueType` -> `MotionRange`

Implements:
`IEquatable<MotionRange>`

## Examples

Map normalized scroll progress to an opacity range:

```csharp
using Cerneala.UI.Motion.Input;

MotionRange opacityRange = new(0f, 1f, 1f, 0.6f);

float startOpacity = opacityRange.Map(0f);
float middleOpacity = opacityRange.Map(0.5f);
float endOpacity = opacityRange.Map(1f);
```

Values outside the input interval are clamped before interpolation:

```csharp
using Cerneala.UI.Motion.Input;

MotionRange range = new(10f, 20f, 100f, 200f);

float below = range.Map(5f);   // 100
float inside = range.Map(15f); // 150
float above = range.Map(25f);  // 200
```

## Remarks

`MotionRange` stores four float endpoints: the input interval and the output interval. `Map(float value)` calculates progress through the input interval, clamps that progress to `0` through `1`, and returns the corresponding interpolated output value.

When `InputStart` and `InputEnd` are equal, the input interval has no length. In that case, `Map` returns `OutputEnd`.

`ScrollTimelineProgress.Map(float from, float to)` creates a `MotionRange` with an input range of `0` to `1` and uses it inside `ScrollMotionBinding<T>` to translate normalized scroll progress into bound float property values.

## Constructors

| Name | Description |
| --- | --- |
| `MotionRange(float InputStart, float InputEnd, float OutputStart, float OutputEnd)` | Initializes a range with input and output interval endpoints. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `InputStart` | `float` | Gets the input value that maps to `OutputStart`. |
| `InputEnd` | `float` | Gets the input value that maps to `OutputEnd`. |
| `OutputStart` | `float` | Gets the output value returned when clamped progress is `0`. |
| `OutputEnd` | `float` | Gets the output value returned when clamped progress is `1`, and the fallback returned when the input interval has zero length. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Map(float value)` | `float` | Maps `value` from the input interval to the output interval, clamping input progress before interpolation. |

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `Cerneala.UI.Motion.Input.ScrollMotionBinding<T>`
- `Cerneala.UI.Motion.Input.ScrollTimelineProgress`
- `UI/Motion/Input/MotionRange.cs`
