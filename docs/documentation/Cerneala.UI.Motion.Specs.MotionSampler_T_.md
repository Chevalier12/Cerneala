# MotionSampler<T> Class

## Definition
Namespace: `Cerneala.UI.Motion.Specs`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Specs/MotionSampler.cs`

Defines the typed sampling contract used by motion specifications to advance, inspect, and retarget an animated value.

```csharp
public abstract class MotionSampler<T> : MotionSampler
```

Inheritance:
`object` -> `MotionSampler` -> `MotionSampler<T>`

## Type Parameters

| Name | Description |
| --- | --- |
| `T` | The value type sampled by the motion. |

## Examples

Create a sampler from a tween specification and advance it manually:

```csharp
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Interpolation;
using Cerneala.UI.Motion.Specs;

MotionSpecContext context = new(
    ReducedMotionPolicy.Default,
    new ValueMixerRegistry(),
    Diagnostics: null,
    Now: TimeSpan.Zero);

MotionSampler<float> sampler = Motion.Tween<float>(
        TimeSpan.FromMilliseconds(100),
        Easings.Linear)
    .CreateSampler(0f, 1f, new FloatMixer(), context);

sampler.Advance(TimeSpan.FromMilliseconds(50));

float current = sampler.Current;
object? currentUntyped = sampler.CurrentUntyped;

sampler.Retarget(2f, RetargetMode.Restart);
```

## Remarks

`MotionSampler<T>` is the typed base class returned by `MotionSpec<T>.CreateSampler`. Concrete sampler implementations live inside motion specifications such as tween, keyframes, spring, decay, repeat, and ping-pong specs.

`Current` exposes the typed sample. `CurrentUntyped` is inherited from `MotionSampler` and returns `Current` boxed as `object?`, allowing untyped motion infrastructure to observe samplers without knowing `T`.

`Advance(TimeSpan)` is inherited from `MotionSampler` and moves the sampler forward by a time delta. Concrete implementations in this package reject negative deltas and stop changing their output after `IsComplete` becomes `true`.

`Velocity` is optional. The base implementation returns `null`; samplers that can report velocity override it. `MotionValue<T>` reads this property after graph ticks and treats `InvalidOperationException` from a sampler as no available velocity.

`Retarget(T, RetargetMode)` lets a typed caller change the sampler target. The inherited `RetargetUntyped(object?, RetargetMode)` helper casts the supplied value to `T`, accepts `null` only when `T` can be null, and throws `ArgumentException` for incompatible values.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Current` | `T` | Gets the current typed sample value. |
| `Velocity` | `MotionVelocity<T>?` | Gets the current sampled velocity when the sampler provides one; otherwise `null`. |
| `CurrentUntyped` | `object?` | Gets `Current` as an untyped value. |
| `IsComplete` | `bool` | Inherited from `MotionSampler`. Gets whether the sampler has completed. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Advance(TimeSpan)` | `void` | Inherited from `MotionSampler`. Advances the sampler by the supplied time delta. |
| `Retarget(T, RetargetMode)` | `void` | Retargets the sampler to a typed destination using the requested retarget behavior. |
| `RetargetUntyped(object?, RetargetMode)` | `void` | Casts an untyped destination to `T`, then delegates to `Retarget`. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `RetargetUntyped(object?, RetargetMode)` | `ArgumentException` | `to` is not assignable to `T`, or `to` is `null` and `T` cannot be null. |

## Applies to

Cerneala UI motion specifications and graph-driven value animation sampling.

## See also

- `Cerneala.UI.Motion.Specs.MotionSampler`
- `Cerneala.UI.Motion.Specs.MotionSpec<T>`
- `Cerneala.UI.Motion.Specs.RetargetMode`
- `Cerneala.UI.Motion.Specs.MotionVelocity<T>`
- `Cerneala.UI.Motion.Core.MotionValue<T>`
