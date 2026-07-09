# RepeatSpec<T>.StaticSampler Class

## Definition
Namespace: `Cerneala.UI.Motion.Specs`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Specs/RepeatSpec.cs`

Provides the completed sampler returned by `RepeatSpec<T>` when an infinite repeat is skipped because reduced motion is enabled.

```csharp
private sealed class StaticSampler(T current) : MotionSampler<T>
```

Inheritance:
`object` -> `MotionSampler` -> `MotionSampler<T>` -> `RepeatSpec<T>.StaticSampler`

Containing type:
`RepeatSpec<T>`

Access:
`private`; callers receive it through the `MotionSampler<T>` returned by `RepeatSpec<T>.CreateSampler`.

## Examples

`CreateSampler` returns the static sampler for an infinite repeat when the motion context requests reduced motion:

```csharp
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Interpolation;
using Cerneala.UI.Motion.Specs;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

RepeatSpec<float> spec = new(
    MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100), Easings.Linear));

MotionSpecContext context = new(
    new ReducedMotionPolicy(ReducedMotionMode.DisableNonEssential),
    new ValueMixerRegistry(),
    Diagnostics: null,
    Now: TimeSpan.Zero);

MotionSampler<float> sampler = spec.CreateSampler(0, 1, new FloatMixer(), context);

bool complete = sampler.IsComplete;
float current = sampler.Current;
```

## Remarks

`StaticSampler` is a private implementation detail of `RepeatSpec<T>`. `RepeatSpec<T>.CreateSampler` creates it only when `repeatCount` is `null` and `MotionSpecContext.ReducedMotion.Mode` is not `ReducedMotionMode.NoPreference`. In that path, the sampler immediately exposes the target value passed to the constructor, reports completion, and records a reduced-motion skip through diagnostics when diagnostics are present.

Calling `Advance` does not change the sampled value. Calling `Retarget` also does not change the sampled value. This keeps an otherwise infinite repeated tween from requesting more frames under reduced-motion settings.

## Constructors

| Name | Description |
| --- | --- |
| `StaticSampler(T current)` | Initializes the sampler with the value exposed by `Current`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Current` | `T` | Gets the fixed sampled value supplied when the sampler was created. |
| `CurrentUntyped` | `object?` | Gets `Current` through the base `MotionSampler<T>` implementation. |
| `IsComplete` | `bool` | Always returns `true`. |
| `Velocity` | `MotionVelocity<T>?` | Inherits the base `MotionSampler<T>` behavior, which returns `null`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Advance(TimeSpan delta)` | `void` | Does nothing; the sampler remains complete and fixed at `Current`. |
| `Retarget(T to, RetargetMode mode)` | `void` | Does nothing; the fixed sampled value is not retargeted. |
| `RetargetUntyped(object? to, RetargetMode mode)` | `void` | Inherited from `MotionSampler<T>`; validates the target value type before calling `Retarget`. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `RetargetUntyped(object? to, RetargetMode mode)` | `ArgumentException` | `to` is not assignable to `T`, except for `null` when `T` allows it. |

## Applies to

Cerneala UI motion specifications when reduced motion disables an infinite `RepeatSpec<T>` animation.

## See also

- `RepeatSpec<T>`
- `MotionSampler<T>`
- `MotionSpecContext`
- `ReducedMotionPolicy`
