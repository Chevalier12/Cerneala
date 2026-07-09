# MotionKeyframe<T> Struct

## Definition
Namespace: `Cerneala.UI.Motion.Specs`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Specs/KeyframesSpec.cs`

Represents one normalized keyframe in a `KeyframesSpec<T>` sequence.

```csharp
public readonly record struct MotionKeyframe<T>(
    float Offset,
    T Value,
    IEasing? Easing = null,
    bool Hold = false)
```

Inheritance:
`ValueType` -> `MotionKeyframe<T>`

Implements:
`IEquatable<MotionKeyframe<T>>`

## Type Parameters

| Name | Description |
| --- | --- |
| `T` | The animated value type stored by the keyframe. |

## Examples

Create keyframes for a float animation:

```csharp
using Cerneala.UI.Motion.Specs;

KeyframesSpec<float> spec = Motion.Keyframes(
    new MotionKeyframe<float>(0, 0),
    new MotionKeyframe<float>(0.5f, 12, Easings.EaseOut),
    new MotionKeyframe<float>(1, 20));
```

Use a held keyframe to keep a value across the following segment:

```csharp
using Cerneala.UI.Motion.Specs;

KeyframesSpec<float> spec = new(
[
    new MotionKeyframe<float>(0, 0, Hold: true),
    new MotionKeyframe<float>(0.4f, 10),
    new MotionKeyframe<float>(1, 20)
]);
```

## Remarks

`MotionKeyframe<T>` is consumed by `KeyframesSpec<T>`. `Offset` is normalized animation progress, where `0` is the start of the keyframe sequence and `1` is the end.

`KeyframesSpec<T>` validates the list that contains these values. The list must contain at least two keyframes, start with offset `0`, end with offset `1`, keep every offset in `[0, 1]`, avoid `NaN`, and be sorted in ascending order.

During sampling, the segment that starts at this keyframe uses this keyframe's `Easing` value. If `Easing` is `null`, the sampler uses `Easings.Linear`. If `Hold` is `true`, the sampler returns this keyframe's `Value` for the segment instead of interpolating toward the next keyframe. A segment whose start and end offsets are equal is also sampled as a held segment.

Because this type is a readonly record struct, it has value equality, generated copy support through `with` expressions, and generated deconstruction for the primary constructor values.

## Constructors

| Name | Description |
| --- | --- |
| `MotionKeyframe(float Offset, T Value, IEasing? Easing = null, bool Hold = false)` | Initializes a keyframe with normalized offset, value, optional easing, and optional hold behavior. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Offset` | `float` | Gets normalized progress for this keyframe. Valid keyframe specs require offsets in `[0, 1]`. |
| `Value` | `T` | Gets the value sampled at this keyframe. |
| `Easing` | `IEasing?` | Gets the easing applied to the segment that starts at this keyframe. `null` means linear easing when sampled by `KeyframesSpec<T>`. |
| `Hold` | `bool` | Gets whether the segment that starts at this keyframe should keep `Value` instead of interpolating. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Deconstruct(out float Offset, out T Value, out IEasing? Easing, out bool Hold)` | `void` | Deconstructs the keyframe into the values from its primary constructor. |
| `Equals(MotionKeyframe<T>)` | `bool` | Determines whether another keyframe has equal record values. |
| `GetHashCode()` | `int` | Returns the generated record hash code for the keyframe values. |
| `ToString()` | `string` | Returns the generated record string representation. |

## Applies to

Cerneala UI keyframe motion specifications and value animation sampling.

## See also

- `Cerneala.UI.Motion.Specs.KeyframesSpec<T>`
- `Cerneala.UI.Motion.Specs.Motion`
- `Cerneala.UI.Motion.Specs.IEasing`
- `Cerneala.UI.Motion.Specs.Easings`
- `Cerneala.UI.Motion.Interpolation.ValueMixer<T>`
