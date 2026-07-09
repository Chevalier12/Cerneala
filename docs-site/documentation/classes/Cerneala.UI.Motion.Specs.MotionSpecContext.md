# MotionSpecContext Class

## Definition
Namespace: `Cerneala.UI.Motion.Specs`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Specs/MotionSpecContext.cs`

Carries per-sampler services and frame metadata used when a `MotionSpec` creates a `MotionSampler`.

```csharp
public sealed record MotionSpecContext(
    ReducedMotionPolicy ReducedMotion,
    ValueMixerRegistry Mixers,
    MotionDiagnostics? Diagnostics,
    TimeSpan Now,
    string? DebugName = null);
```

Inheritance:
`object` -> `MotionSpecContext`

## Examples

Create a context for a typed sampler:

```csharp
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Diagnostics;
using Cerneala.UI.Motion.Interpolation;
using Cerneala.UI.Motion.Specs;

ValueMixerRegistry mixers = new();
mixers.RegisterBuiltIns();

MotionSpecContext context = new(
    ReducedMotion: ReducedMotionPolicy.Default,
    Mixers: mixers,
    Diagnostics: new MotionDiagnostics(),
    Now: TimeSpan.Zero,
    DebugName: "opacity");

MotionSpec<float> spec = Motion.Tween<float>(TimeSpan.FromMilliseconds(120));
MotionSampler<float> sampler = spec.CreateSampler(0f, 1f, new FloatMixer(), context);
```

## Remarks

`MotionSpecContext` is passed to `MotionSpec.CreateSamplerUntyped` and `MotionSpec<T>.CreateSampler` so concrete specifications can adapt sampler creation to the current motion environment.

The motion graph creates this context with its configured reduced-motion policy, mixer registry, optional diagnostics sink, and optional debug name. Specifications use those values selectively. For example, `TweenSpec<T>` shortens reduced-motion tweens and records reduced-motion skips through `Diagnostics`, `RepeatSpec<T>` turns infinite animation into a static sampler when reduced motion is enabled, and spring/decay samplers retain the context for diagnostics and nested bounce sampler creation.

`MotionSpecContext` is an immutable record. Use record construction or `with` expressions to pass adjusted values for tests or specialized sampler creation.

## Constructors

| Name | Description |
| --- | --- |
| `MotionSpecContext(ReducedMotionPolicy ReducedMotion, ValueMixerRegistry Mixers, MotionDiagnostics? Diagnostics, TimeSpan Now, string? DebugName = null)` | Initializes a context with reduced-motion policy, mixer registry, diagnostics sink, current time metadata, and an optional debug name. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `ReducedMotion` | `ReducedMotionPolicy` | Policy consulted by specifications that need to reduce or disable animation. |
| `Mixers` | `ValueMixerRegistry` | Registry of value mixers available to motion code that needs to resolve interpolation support. |
| `Diagnostics` | `MotionDiagnostics?` | Optional diagnostics sink used to record warnings and reduced-motion skips. |
| `Now` | `TimeSpan` | Current motion time metadata supplied with the context. |
| `DebugName` | `string?` | Optional name included in diagnostics messages for the sampler or animated value. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Deconstruct(out ReducedMotionPolicy ReducedMotion, out ValueMixerRegistry Mixers, out MotionDiagnostics? Diagnostics, out TimeSpan Now, out string? DebugName)` | `void` | Deconstructs the positional record values. |
| `Equals(MotionSpecContext? other)` | `bool` | Compares this record with another `MotionSpecContext` by record equality. |
| `ToString()` | `string` | Returns the record-formatted string representation. |

## Applies to

Cerneala UI motion specifications and samplers that need reduced-motion behavior, mixer resolution, diagnostics, or debug metadata during sampler creation.

## See also

- `Cerneala.UI.Motion.Specs.MotionSpec`
- `Cerneala.UI.Motion.Specs.MotionSpec<T>`
- `Cerneala.UI.Motion.Specs.MotionSampler`
- `Cerneala.UI.Motion.Core.MotionGraph`
- `Cerneala.UI.Motion.Core.ReducedMotionPolicy`
- `Cerneala.UI.Motion.Interpolation.ValueMixerRegistry`
- `Cerneala.UI.Motion.Diagnostics.MotionDiagnostics`
