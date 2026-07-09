# MotionDefaults Class

## Definition
Namespace: `Cerneala.UI.Motion`

Assembly/Project: `Cerneala`

Source: `UI/Motion/MotionDefaults.cs`

Provides shared default motion specifications for common UI animation timings.

```csharp
public static class MotionDefaults
```

Inheritance:
`object` -> `MotionDefaults`

## Examples

Use a default motion specification when configuring motion-aware options:

```csharp
using Cerneala.UI.Motion;
using Cerneala.UI.Motion.Interpolation;
using Cerneala.UI.Motion.Properties;

MotionPropertyOptions options = new(
    mixerType: typeof(FloatMixer),
    defaultSpec: MotionDefaults.Standard,
    invalidationCategory: MotionPropertyInvalidationCategory.Render,
    isSafeForImplicitAnimation: true);
```

Create a faster interaction by selecting the shorter default specification:

```csharp
using Cerneala.UI.Motion;
using Cerneala.UI.Motion.Specs;

MotionSpec fastSpec = MotionDefaults.FastOut;
MotionSpec standardSpec = MotionDefaults.Standard;
```

## Remarks

`MotionDefaults` is a small static catalog of reusable untyped `MotionSpec` instances. It does not store mutable state and does not start animations by itself.

Each property creates a tween specification through `Cerneala.UI.Motion.Specs.Motion.Tween(TimeSpan, IEasing?)`. Both built-in defaults use `Easings.Standard`; `FastOut` uses a 120 ms duration, while `Standard` uses a 180 ms duration.

Use these defaults when an API needs a semantic motion baseline rather than a hand-built `TweenSpec<T>` or custom `MotionSpec`. Because the properties are expression-bodied, each access returns a freshly created specification.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `FastOut` | `MotionSpec` | Gets an untyped tween specification with a 120 ms duration and `Easings.Standard`. |
| `Standard` | `MotionSpec` | Gets an untyped tween specification with a 180 ms duration and `Easings.Standard`. |

## Applies to

Cerneala UI motion APIs that accept an untyped `Cerneala.UI.Motion.Specs.MotionSpec`.

## See also

- `Cerneala.UI.Motion.Specs.Motion`
- `Cerneala.UI.Motion.Specs.MotionSpec`
- `Cerneala.UI.Motion.Specs.TweenSpec<T>`
- `Cerneala.UI.Motion.Specs.Easings`
