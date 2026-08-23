# Prism Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/Prism.cs`

Creates lazy Prism images from drawable images and code-defined Prism operations.

```csharp
public static class Prism
```

## Examples

Create a reusable glow pipeline and draw the resulting image from `RenderSurface2D`.

```csharp
using Cerneala.Drawing.Prism;

PrismPipeline foodEffects = new()
{
    new BlurFilter
    {
        Radius = 0.75f
    },
    new OuterGlowStyle
    {
        Size = 4,
        Opacity = 0.38f,
        Color = Color.FromArgb(0x70, 0xFF, 0x48, 0x90)
    }
};

PrismImage glowingFood = Prism.Apply(foodImage, foodEffects);

surface.Draw += (_, frame) =>
    frame.DrawImage(glowingFood, foodBounds, Color.White);
```

## Remarks

`Apply` is lazy. It does not allocate or expose a GPU texture when called. `PrismImage` expands into the native Prism command scope when it is drawn, so graph planning, filter kernels, bounds expansion, composition, and cache invalidation use the same pipeline as Prism markup.

The returned image remains connected to its `PrismPipeline`. Changing an operation property or changing the pipeline collection is observed on the next draw. Filters execute in insertion order, followed by styles in insertion order, matching the Prism layer model.

## Methods

| Name | Return type | Description |
| --- | --- | --- |
| `Apply(IDrawImage, params PrismOperation[])` | `PrismImage` | Creates a Prism image from a source and an inline operation list. |
| `Apply(IDrawImage, PrismPipeline)` | `PrismImage` | Creates a Prism image backed by a reusable pipeline. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Apply` | `ArgumentNullException` | The source, operation array, or pipeline is `null`. |
| `Apply` | `ArgumentException` | The pipeline contains no operations. |

## See Also

- `PrismImage`
- `PrismPipeline`
- `PrismOperation`
- `RenderSurface2DFrame`
