# RepeatSpec<T>.Sampler Class

## Definition
Namespace: `Cerneala.UI.Motion.Specs`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Specs/RepeatSpec.cs`

Samples repeated tween motion for a `RepeatSpec<T>` by wrapping elapsed time around the inner tween duration.

```csharp
private sealed class Sampler : MotionSampler<T>
```

Inheritance:
`object` -> `MotionSampler` -> `MotionSampler<T>` -> `RepeatSpec<T>.Sampler`

Containing type:
`RepeatSpec<T>`

Access:
`private`; this nested sampler is an implementation detail created by `RepeatSpec<T>.CreateSampler` when repeat motion is not reduced to a static value.

## Examples

Callers do not create `RepeatSpec<T>.Sampler` directly. Create it through `RepeatSpec<T>.CreateSampler`, usually by constructing a `RepeatSpec<T>` around a tween.

```csharp
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Interpolation;
using Cerneala.UI.Motion.Specs;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

ValueMixerRegistry mixers = new();
mixers.RegisterBuiltIns();

MotionSpecContext context = new(
    ReducedMotionPolicy.Default,
    mixers,
    Diagnostics: null,
    Now: TimeSpan.Zero,
    DebugName: "pulse");

RepeatSpec<float> spec = new(
    MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100), Easings.Linear),
    repeatCount: 2);

MotionSampler<float> sampler = spec.CreateSampler(0, 10, new FloatMixer(), context);

sampler.Advance(TimeSpan.FromMilliseconds(150));
float halfwayThroughSecondLoop = sampler.Current;
```

## Remarks

`RepeatSpec<T>.Sampler` starts with `Current` equal to the supplied `from` value. Each `Advance` call adds the delta to internal elapsed time, divides the elapsed time by the inner `TweenSpec<T>.Duration`, and samples the current cycle from `from` to `to` with the supplied `ValueMixer<T>` and the tween's `Easing`.

When `repeatCount` has a value, the sampler completes once elapsed time reaches `Duration * repeatCount`. At completion, `Current` is set to the supplied `to` value and `IsComplete` becomes `true`. Before that final boundary, exact cycle boundaries wrap to progress `0`, so the sampled value returns to `from` between loops.

When `repeatCount` is `null`, the sampler repeats indefinitely and does not complete on its own. `RepeatSpec<T>.CreateSampler` chooses a separate static sampler instead of this sampler for infinite repeats when reduced motion mode is not `NoPreference`.

`Retarget` is currently a no-op. The sampler keeps its original `to` value, elapsed time, and completion state when retargeting is requested. The sampler also does not provide velocity, so the inherited `Velocity` property returns `null`.

## Constructors

| Name | Description |
| --- | --- |
| `Sampler(TweenSpec<T> spec, int? repeatCount, T from, T to, ValueMixer<T> mixer)` | Initializes the repeat sampler with the inner tween, optional finite repeat count, start value, target value, and value mixer. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Current` | `T` | Gets the current sampled value. |
| `IsComplete` | `bool` | Gets whether a finite repeat count has reached its final boundary. Infinite repeats remain incomplete. |
| `Velocity` | `MotionVelocity<T>?` | Inherited from `MotionSampler<T>`. Repeat sampling does not provide velocity, so the value is `null`. |
| `CurrentUntyped` | `object?` | Gets `Current` through the inherited `MotionSampler<T>` implementation. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Advance(TimeSpan delta)` | `void` | Advances elapsed time, samples the active repeat cycle, and completes finite repeats when elapsed time reaches `Duration * repeatCount`. |
| `Retarget(T to, RetargetMode mode)` | `void` | Does nothing; the existing target and elapsed state are retained. |
| `RetargetUntyped(object? to, RetargetMode mode)` | `void` | Retargets through the inherited typed-value adapter from `MotionSampler<T>`. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `RetargetUntyped(object? to, RetargetMode mode)` | `ArgumentException` | `to` is not assignable to `T`, except for `null` when `T` allows it. |

## Applies to

Cerneala motion spec internals.

## See also

- `RepeatSpec<T>`
- `TweenSpec<T>`
- `MotionSampler<T>`
- `Easings`
- `ValueMixer<T>`
