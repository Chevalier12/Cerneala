# PrismImage Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/PrismImage.cs`

Represents an `IDrawImage` whose source is processed lazily by a Prism pipeline when drawn.

```csharp
public sealed class PrismImage : IDrawImage, IDrawImageInvalidationSource, IDisposable
```

## Examples

```csharp
using PrismImage glowImage = Prism.Apply(
    sourceImage,
    new OuterGlowStyle
    {
        Size = 8,
        Opacity = 0.8f
    });

// Keep the source alpha as the glow mask, but omit the source pixels from
// this processed image. Draw the visible source separately when needed.
glowImage.Fill = 0;

frame.DrawSprite(glowImage, destination, Color.White);
```

## Remarks

`PrismImage` keeps the original source and pipeline description, synchronizes a native `PrismInstance` only when needed, and uses a stable cache-owner token for repeated draws. Dispose the image when it is no longer needed to request deterministic eviction of its retained Prism cache entries. Eviction occurs at the next safe rendering boundary; an entry that is still used by an active draw lease or a submitted command buffer is released when both uses have ended.

`Fill` controls only the processed layer's source content. Prism filters and styles still read the unscaled source. Setting `Fill` to `0` therefore supports effect-only images such as an outer glow mask, while the caller can draw separate visible content over the result.

Disposal is idempotent. Drawing a disposed image or adding a new content-change observer throws `ObjectDisposedException`.

`PrismImage` does not own or dispose its `Source`, `Pipeline`, or pipeline operations. Their lifetime remains the caller's responsibility.

Operation and pipeline mutations notify observers immediately; the image increments its recorded visual content version on the next draw that sees the change. After a `PrismImage` is drawn by an on-demand `RenderSurface2D`, those mutations also mark that surface dirty automatically. The surface stops observing the image when a later frame no longer draws it.

Nested `PrismImage` values are supported and compose as nested native Prism scopes. Changes from a nested source propagate through the outer image to an observing surface.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Source` | `IDrawImage` | Gets the unprocessed source image. |
| `Pipeline` | `PrismPipeline` | Gets the live pipeline used for subsequent draws. |
| `Fill` | `float` | Gets or sets the processed layer's source fill from `0` through `1`. The default is `1`. |
| `Width` | `int` | Gets the source image width. |
| `Height` | `int` | Gets the source image height. |

## Methods

| Name | Description |
| --- | --- |
| `Dispose()` | Stops source and pipeline observation and requests deterministic eviction of retained cache entries owned by this image. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Fill` setter | `ObjectDisposedException` | The image is disposed. |
| `Fill` setter | `ArgumentOutOfRangeException` | The value is not finite or is outside `0` through `1`. |
| Drawing the image | `ObjectDisposedException` | The image is disposed. |
| Drawing the image | `InvalidOperationException` | The pipeline was emptied after `Prism.Apply`. |

## Applies To

`Cerneala` applications using Prism images through `DrawingContext` or `RenderSurface2DFrame`.

## See Also

- `IDrawImage`
- `Prism`
- `PrismPipeline`
- `RenderSurface2DFrame.DrawImage`
