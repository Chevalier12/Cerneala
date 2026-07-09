# RepeatSpec<T> Class

## Definition
Namespace: `Cerneala.UI.Motion.Specs`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Specs/RepeatSpec.cs`

Represents a tween-based motion specification that restarts the same interpolation for a fixed number of repeats or indefinitely.

```csharp
public sealed class RepeatSpec<T> : MotionSpec<T>
```

Inheritance:
`object` -> `MotionSpec` -> `MotionSpec<T>` -> `RepeatSpec<T>`

## Type Parameters

| Name | Description |
| --- | --- |
| `T` | The animated value type. The supplied `ValueMixer<T>` is used to interpolate between the `from` and `to` values. |

## Examples

Create a repeat sampler that runs two 100 millisecond cycles. At an exact cycle boundary, the finite repeat has restarted at the beginning of the next cycle.

```csharp
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Interpolation;
using Cerneala.UI.Motion.Specs;

RepeatSpec<float> spec = new(
    Motion.Tween<float>(TimeSpan.FromMilliseconds(100), Easings.Linear),
    repeatCount: 2);

MotionSpecContext context = new(
    ReducedMotionPolicy.Default,
    new ValueMixerRegistry(),
    Diagnostics: null,
    Now: TimeSpan.Zero);

MotionSampler<float> sampler = spec.CreateSampler(0, 10, new FloatMixer(), context);

sampler.Advance(TimeSpan.FromMilliseconds(100));
float cycleBoundary = sampler.Current; // 0

sampler.Advance(TimeSpan.FromMilliseconds(50));
float halfwayThroughSecondCycle = sampler.Current; // 5
```

## Remarks

`RepeatSpec<T>` wraps a `TweenSpec<T>` and uses the wrapped tween's `Duration` and `Easing` for each repeated cycle. Each cycle samples from the original `from` value toward the original `to` value; when a cycle finishes, progress wraps back to the beginning instead of reversing.

When `repeatCount` is a positive integer, the sampler completes after `inner.Duration * repeatCount` and sets `Current` to `to`. When `repeatCount` is `null`, the sampler repeats indefinitely and continues requesting frames until the owning motion is canceled or replaced.

Infinite repeats are treated as non-essential motion when reduced motion is enabled. If the context's reduced-motion mode is not `NoPreference`, `CreateSampler` records a reduced-motion skip when diagnostics are available and returns a complete static sampler whose `Current` value is `to`.

The current sampler does not retarget repeat animations. Calling `Retarget` does not change the target, current value, elapsed time, or completion state.

## Constructors

| Name | Description |
| --- | --- |
| `RepeatSpec(TweenSpec<T>, int?)` | Initializes a repeat specification from an inner tween and an optional positive repeat count. `null` creates an infinite repeat. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `CreateSampler(T, T, ValueMixer<T>, MotionSpecContext)` | `MotionSampler<T>` | Creates a sampler that restarts the configured tween cycle until the optional repeat count is reached. |
| `CreateSamplerUntyped(object?, object?, IValueMixer, MotionSpecContext)` | `MotionSampler` | Inherited from `MotionSpec<T>`. Casts untyped values and mixer instances before delegating to `CreateSampler`. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `RepeatSpec(TweenSpec<T>, int?)` | `ArgumentNullException` | `inner` is `null`. |
| `RepeatSpec(TweenSpec<T>, int?)` | `ArgumentOutOfRangeException` | `repeatCount` is zero or negative. |
| `CreateSampler(T, T, ValueMixer<T>, MotionSpecContext)` | `ArgumentNullException` | `context` is `null`. |

## Applies to

Cerneala UI motion specifications and repeated value animation sampling.

## See also

- `Cerneala.UI.Motion.Specs.Motion`
- `Cerneala.UI.Motion.Specs.TweenSpec<T>`
- `Cerneala.UI.Motion.Specs.MotionSpec<T>`
- `Cerneala.UI.Motion.Specs.MotionSampler<T>`
- `Cerneala.UI.Motion.Specs.Easings`
- `Cerneala.UI.Motion.Interpolation.ValueMixer<T>`
