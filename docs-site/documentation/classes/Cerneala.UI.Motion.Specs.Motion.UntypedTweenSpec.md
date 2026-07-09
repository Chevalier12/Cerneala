# Motion.UntypedTweenSpec Class

## Definition
Namespace: `Cerneala.UI.Motion.Specs`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Specs/Motion.cs`

Provides the private untyped tween specification returned by `Motion.Tween(TimeSpan, IEasing?)`.

```csharp
private sealed class UntypedTweenSpec : MotionSpec
```

Inheritance:
`object` -> `MotionSpec` -> `Motion.UntypedTweenSpec`

Containing type:
`Motion`

Access:
`private`; callers create this implementation through `Motion.Tween(TimeSpan, IEasing?)`.

## Examples

Create an untyped tween specification through the public factory and build a sampler with a mixer that identifies the animated value type:

```csharp
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Interpolation;
using Cerneala.UI.Motion.Specs;

MotionSpec spec = Motion.Tween(TimeSpan.FromMilliseconds(100), Easings.Linear);

MotionSpecContext context = new(
    ReducedMotionPolicy.Default,
    new ValueMixerRegistry(),
    Diagnostics: null,
    Now: TimeSpan.Zero);

MotionSampler sampler = spec.CreateSamplerUntyped(
    from: 0f,
    to: 1f,
    mixer: new FloatMixer(),
    context: context);

sampler.Advance(TimeSpan.FromMilliseconds(50));
object? current = sampler.CurrentUntyped;
```

## Remarks

`UntypedTweenSpec` adapts the public untyped `Motion.Tween(TimeSpan, IEasing?)` factory to the generic `TweenSpec<T>` implementation. Its constructor validates that `duration` is greater than `TimeSpan.Zero`, stores the optional easing value, and leaves the concrete value type unresolved until a sampler is created.

`CreateSamplerUntyped` uses `mixer.ValueType` to close the internal generic `CreateTweenSampler<T>` helper, then passes the stored duration, easing, untyped endpoints, mixer, and `MotionSpecContext` to that helper. The helper validates that the supplied mixer is a `ValueMixer<T>` and that `from` and `to` are assignable to `T`, allowing `null` only when `T` can accept it.

The produced sampler has the behavior of `TweenSpec<T>`: it interpolates from the starting value to the target value over the effective duration, applies the supplied easing or `Easings.Standard` when the easing argument is `null`, and observes reduced-motion policy through the `MotionSpecContext`.

Reflection invocation unwraps inner exceptions so validation failures keep the original helper stack trace.

## Constructors

| Name | Description |
| --- | --- |
| `UntypedTweenSpec(TimeSpan, IEasing?)` | Initializes an untyped tween specification with a positive duration and an optional easing function. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `CreateSamplerUntyped(object?, object?, IValueMixer, MotionSpecContext)` | `MotionSampler` | Creates a typed tween sampler by using `mixer.ValueType` to select the underlying `TweenSpec<T>` implementation. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `UntypedTweenSpec(TimeSpan, IEasing?)` | `ArgumentOutOfRangeException` | `duration` is less than or equal to `TimeSpan.Zero`. |
| `CreateSamplerUntyped(object?, object?, IValueMixer, MotionSpecContext)` | `ArgumentException` | `mixer` is not a `ValueMixer<T>` for `mixer.ValueType`, or `from` or `to` cannot be cast to the mixer value type. |

## Applies to

Cerneala UI motion specifications and untyped animation sampling internals.

## See also

- `Cerneala.UI.Motion.Specs.Motion`
- `Cerneala.UI.Motion.Specs.MotionSpec`
- `Cerneala.UI.Motion.Specs.TweenSpec<T>`
- `Cerneala.UI.Motion.Specs.MotionSampler`
- `Cerneala.UI.Motion.Interpolation.IValueMixer`
