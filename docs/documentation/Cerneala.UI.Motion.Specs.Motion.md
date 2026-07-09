# Motion Class

## Definition
Namespace: `Cerneala.UI.Motion.Specs`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Specs/Motion.cs`

Creates common motion specifications for typed and untyped animation workflows.

```csharp
public static class Motion
```

Inheritance:
`object` -> `Motion`

## Examples

Create typed motion specifications for values that will be sampled with a matching `ValueMixer<T>`:

```csharp
using Cerneala.UI.Motion.Specs;

TweenSpec<float> fade = Motion.Tween<float>(
    TimeSpan.FromMilliseconds(180),
    Easings.Standard);

SpringSpec<float> settle = Motion.Spring<float>(
    stiffness: 520,
    damping: 38,
    mass: 1);

KeyframesSpec<float> pulse = Motion.Keyframes(
    new MotionKeyframe<float>(0, 0),
    new MotionKeyframe<float>(0.5f, 1),
    new MotionKeyframe<float>(1, 0));
```

Create an untyped specification when the value type is supplied later through an `IValueMixer`:

```csharp
using Cerneala.UI.Motion.Specs;

MotionSpec tokenMotion = Motion.Tween(TimeSpan.FromMilliseconds(120));
```

## Remarks

`Motion` is a static factory for the built-in motion specification types. The generic methods return typed `MotionSpec<T>` implementations directly: `TweenSpec<T>`, `SpringSpec<T>`, `KeyframesSpec<T>`, and `DecaySpec<T>`.

The non-generic `Tween` and `Spring` overloads return `MotionSpec` instances. Those specifications defer the concrete value type until `CreateSamplerUntyped` receives an `IValueMixer`; the mixer value type is then used to create the matching typed sampler.

Generic tween, spring, keyframe, and decay specifications keep the validation and sampling behavior of their concrete specification classes. For example, tween durations must be positive, spring stiffness and mass must be positive, spring damping cannot be negative, and decay deceleration must be greater than `0` and less than `1`.

Untyped tween and spring specifications also validate their constructor arguments immediately. When they later create a sampler, `from`, `to`, and the mixer must match the mixer value type or sampler creation throws an `ArgumentException`.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Tween<T>(TimeSpan duration, IEasing? easing = null)` | `TweenSpec<T>` | Creates a typed tween specification with the specified duration and optional easing. |
| `Spring<T>(float stiffness = 520, float damping = 38, float mass = 1)` | `SpringSpec<T>` | Creates a typed spring specification with stiffness, damping, and mass parameters. |
| `Keyframes<T>(params MotionKeyframe<T>[] frames)` | `KeyframesSpec<T>` | Creates a typed keyframes specification from the supplied keyframes. |
| `Decay<T>(MotionVelocity<T> initialVelocity, float deceleration = 0.998f)` | `DecaySpec<T>` | Creates a typed decay specification using the supplied initial velocity and deceleration. |
| `Tween(TimeSpan duration, IEasing? easing = null)` | `MotionSpec` | Creates an untyped tween specification whose concrete value type is resolved from the mixer at sampler creation time. |
| `Spring(float stiffness = 520, float damping = 38, float mass = 1)` | `MotionSpec` | Creates an untyped spring specification whose concrete value type is resolved from the mixer at sampler creation time. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Tween<T>(TimeSpan, IEasing?)` | `ArgumentOutOfRangeException` | `duration` is less than or equal to `TimeSpan.Zero`. |
| `Spring<T>(float, float, float)` | `ArgumentOutOfRangeException` | `stiffness` or `mass` is not finite or is less than or equal to `0`; `damping` is not finite or is less than `0`. |
| `Keyframes<T>(params MotionKeyframe<T>[])` | `ArgumentNullException` | `frames` is `null`. |
| `Keyframes<T>(params MotionKeyframe<T>[])` | `ArgumentException` | Fewer than two keyframes are supplied, offsets do not start at `0` and end at `1`, an offset is outside `[0, 1]`, or offsets are not sorted. |
| `Decay<T>(MotionVelocity<T>, float)` | `ArgumentOutOfRangeException` | `deceleration` is not finite, is less than or equal to `0`, or is greater than or equal to `1`. |
| `Tween(TimeSpan, IEasing?)` | `ArgumentOutOfRangeException` | `duration` is less than or equal to `TimeSpan.Zero`. |
| `Spring(float, float, float)` | `ArgumentOutOfRangeException` | `stiffness` or `mass` is not finite or is less than or equal to `0`; `damping` is not finite or is less than `0`. |

## Applies to

Cerneala UI motion specifications, motion tokens, and APIs that need a concise factory for built-in motion behavior.

## See also

- `Cerneala.UI.Motion.Specs.MotionSpec`
- `Cerneala.UI.Motion.Specs.MotionSpec<T>`
- `Cerneala.UI.Motion.Specs.TweenSpec<T>`
- `Cerneala.UI.Motion.Specs.SpringSpec<T>`
- `Cerneala.UI.Motion.Specs.KeyframesSpec<T>`
- `Cerneala.UI.Motion.Specs.DecaySpec<T>`
- `Cerneala.UI.Motion.Interpolation.IValueMixer`
