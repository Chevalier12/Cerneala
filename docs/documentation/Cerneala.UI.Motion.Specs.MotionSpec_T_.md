# MotionSpec<T> Class

## Definition
Namespace: `Cerneala.UI.Motion.Specs`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Specs/MotionSpec{T}.cs`

Defines the typed base contract for motion specifications that create samplers for values of type `T`.

```csharp
public abstract class MotionSpec<T> : MotionSpec
```

Inheritance:
`object` -> `MotionSpec` -> `MotionSpec<T>`

Derived:
`TweenSpec<T>`, `SpringSpec<T>`, `KeyframesSpec<T>`, `DecaySpec<T>`, `RepeatSpec<T>`, `PingPongSpec<T>`

## Type Parameters

| Name | Description |
| --- | --- |
| `T` | The value type animated by the specification. |

## Examples

Create a typed tween specification, then create and advance its sampler:

```csharp
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Interpolation;
using Cerneala.UI.Motion.Specs;

MotionSpec<float> spec = Motion.Tween<float>(
    TimeSpan.FromMilliseconds(120),
    Easings.Standard);

MotionSpecContext context = new(
    ReducedMotionPolicy.Default,
    new ValueMixerRegistry(),
    Diagnostics: null,
    Now: TimeSpan.Zero);

MotionSampler<float> sampler = spec.CreateSampler(
    from: 0f,
    to: 1f,
    mixer: new FloatMixer(),
    context: context);

sampler.Advance(TimeSpan.FromMilliseconds(60));
float current = sampler.Current;
```

Use the same typed specification through the untyped base API:

```csharp
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Interpolation;
using Cerneala.UI.Motion.Specs;

MotionSpec spec = Motion.Tween<float>(TimeSpan.FromMilliseconds(120));

MotionSampler sampler = spec.CreateSamplerUntyped(
    from: 0f,
    to: 1f,
    mixer: new FloatMixer(),
    context: new MotionSpecContext(
        ReducedMotionPolicy.Default,
        new ValueMixerRegistry(),
        Diagnostics: null,
        Now: TimeSpan.Zero));
```

## Remarks

`MotionSpec<T>` is the typed layer between the untyped `MotionSpec` base class and concrete motion specifications. Concrete derived classes implement `CreateSampler(T, T, ValueMixer<T>, MotionSpecContext)` to build a `MotionSampler<T>` for one motion run.

`CreateSamplerUntyped` adapts untyped callers to the typed API. It verifies that the supplied `IValueMixer` is a `ValueMixer<T>`, casts `from` and `to` to `T`, and then delegates to `CreateSampler`. `null` values are accepted only when `T` can be null; otherwise the cast helper throws `ArgumentException`.

The specification object describes how a sampler should be created. The returned sampler owns per-run state such as current value, elapsed time, completion state, velocity, and retargeting behavior.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `CreateSampler(T, T, ValueMixer<T>, MotionSpecContext)` | `MotionSampler<T>` | Creates a typed sampler from a starting value, target value, matching mixer, and specification context. Concrete specifications define validation and sampling behavior. |
| `CreateSamplerUntyped(object?, object?, IValueMixer, MotionSpecContext)` | `MotionSampler` | Casts untyped values and the mixer to the `T`-specific API, then delegates to `CreateSampler`. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `CreateSamplerUntyped(object?, object?, IValueMixer, MotionSpecContext)` | `ArgumentException` | `mixer` is not a `ValueMixer<T>`, `from` is not assignable to `T`, `to` is not assignable to `T`, or a non-nullable `T` receives `null`. |

## Applies to

Cerneala UI motion specifications, motion values, property motion bindings, layout motion, presence motion, and other APIs that create typed motion samplers.

## See also

- `Cerneala.UI.Motion.Specs.MotionSpec`
- `Cerneala.UI.Motion.Specs.MotionSampler<T>`
- `Cerneala.UI.Motion.Specs.Motion`
- `Cerneala.UI.Motion.Interpolation.ValueMixer<T>`
- `Cerneala.UI.Motion.Core.MotionValue<T>`
