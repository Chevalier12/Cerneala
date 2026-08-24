# OuterGlowStyle Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `OuterGlow` style.

```csharp
public sealed class OuterGlowStyle : PrismStyle
```

## Examples

Animate the glow operation itself. Every frame that uses the same instance observes its current values:

```csharp
using System;
using Cerneala.Drawing.Prism;
using Cerneala.UI.Motion;
using Cerneala.UI.Motion.Specs;

OuterGlowStyle glow = new() { Size = 3f, Opacity = 0.62f };
PingPongSpec<float> pulse = new(
    Motion.Tween<float>(TimeSpan.FromSeconds(1.35), Easings.EaseInOut),
    cycles: null);

glow.Motion()
    .Animate(OuterGlowStyle.SizeProperty)
    .From(3f)
    .To(18f)
    .Start(pulse);
```

## Constructors

| Signature | Description |
| --- | --- |
| `OuterGlowStyle()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `BlendMode` | `string` | `Screen` | Optional catalog parameter; unit: `none`. |
| `Color` | `Color` | `#FFFFFFBE` | Optional catalog parameter; unit: `none`. |
| `Gradient` | `PrismResourceId` | `—` | Optional catalog parameter; unit: `none`. |
| `Opacity` | `float` | `0.75` | Optional catalog parameter; unit: `unitless`. |
| `Noise` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |
| `Technique` | `string` | `Softer` | Optional catalog parameter; unit: `none`. |
| `Spread` | `float` | `0` | Optional catalog parameter; unit: `dip`. |
| `Size` | `float` | `5` | Optional catalog parameter; unit: `dip`. |
| `Contour` | `string` | `Linear` | Optional catalog parameter; unit: `none`. |
| `AntiAlias` | `bool` | `False` | Optional catalog parameter; unit: `none`. |
| `Range` | `float` | `0.5` | Optional catalog parameter; unit: `unitless`. |
| `Jitter` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |

## Remarks

Parameter assignments are validated against the `OuterGlow` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismStyle`
- `PrismPipeline`
- `Prism.Apply`
- `MotionProperty<TTarget, TValue>`
- `ObjectMotionFacade`
