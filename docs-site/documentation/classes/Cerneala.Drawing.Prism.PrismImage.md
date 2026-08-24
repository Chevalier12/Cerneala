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

frame.DrawSprite(glowImage, destination);
```

## Remarks

`PrismImage` keeps the original source and pipeline description, synchronizes a native `PrismInstance` only when needed, and uses a stable cache-owner token for repeated draws. Dispose the image when it is no longer needed to request deterministic eviction of its retained Prism cache entries. Eviction occurs at the next safe rendering boundary; an entry that is leased by an active frame is removed as soon as that lease is released.

Disposal is idempotent. Drawing a disposed image or adding a new content-change observer throws `ObjectDisposedException`.

`PrismImage` does not own or dispose its `Source`, `Pipeline`, or pipeline operations. Their lifetime remains the caller's responsibility. A source texture managed by a MonoGame `ContentManager`, for example, continues to follow the content manager's lifetime.

Operation and pipeline mutations increment the recorded visual content version. After a `PrismImage` is drawn by an on-demand `RenderSurface2D`, those mutations also mark that surface dirty automatically. The surface stops observing the image when a later frame no longer draws it.

Nested `PrismImage` values are supported and compose as nested native Prism scopes. Changes from a nested source propagate through the outer image to an observing surface.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Source` | `IDrawImage` | Gets the unprocessed source image. |
| `Pipeline` | `PrismPipeline` | Gets the live pipeline used for subsequent draws. |
| `Width` | `int` | Gets the source image width. |
| `Height` | `int` | Gets the source image height. |

## Methods

| Name | Description |
| --- | --- |
| `Dispose()` | Stops source and pipeline observation and requests deterministic eviction of retained cache entries owned by this image. |

## Explicit Interface Implementations

| Name | Description |
| --- | --- |
| `IDrawImageInvalidationSource.ContentChanged` | Notifies observing surfaces when the source, pipeline, or image lifetime changes. |

## Applies To

`Cerneala` applications using Prism images through `DrawingContext` or `RenderSurface2DFrame`.

## See Also

- `IDrawImage`
- `Prism`
- `PrismPipeline`
- `RenderSurface2DFrame.DrawImage`
