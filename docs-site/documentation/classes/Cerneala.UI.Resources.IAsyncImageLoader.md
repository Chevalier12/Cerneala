# IAsyncImageLoader Interface

## Definition

Namespace: `Cerneala.UI.Resources`

Assembly/Project: `Cerneala`

Source: `UI/Resources/IAsyncImageLoader.cs`

Extends path-based image loading with an explicit asynchronous loading contract.

```csharp
public interface IAsyncImageLoader : IImageLoader
```

## Remarks

Implement this interface when a loader can perform file access and decoding without blocking the acquiring rendering thread. The result is an independently owned, non-null `IDrawImage`, suitable for the drawing backend that will consume it. The loader must not return an image already owned by a different live cache entry. It must support concurrent calls when used by a cache with multiple asynchronous load slots.

`ImageResourceCache.AcquireAsync` invokes this method only for a new path-backed image. Synchronous acquisition continues to use the inherited `IImageLoader.Load` contract. The cache does not move arbitrary synchronous loaders to a worker implicitly; thread-affine loaders must explicitly implement a safe asynchronous preparation path.

Ordinary UI [Image](Cerneala.UI.Controls.Image.md) controls also use this contract for cold path preparation through their root's shared cache. They omit the bitmap while pending, expose `LoadingState`/`LoadingError`, and apply natural dimensions after completion. A cold path with only `IImageLoader` is an observable `NotSupportedException`, not synchronous UI decoding. The asynchronous method must yield without performing blocking file access or decoding on the acquiring UI thread; declaring the interface alone does not make a blocking implementation responsive.

Retained [Sprite2D](Cerneala.UI.Controls.Sprite2D.md) images and live image resources in scene Prism scopes use the same asynchronous capability and shared cache. The owning [RenderSurface2D](Cerneala.UI.Controls.RenderSurface2D.md) exposes their preparation through `PresentationState`/`PresentationError` and withholds the whole scene while required inputs are unavailable; simulation and ordinary UI remain independent. Bounds queries and scene sprite/Prism recording do not invoke the synchronous loader. This does not change explicit `Acquire`/`Load` calls or imply that arbitrary caller-defined image sources are nonblocking.

Honour cancellation where the decoder permits it. If a loader finishes successfully after all requesting acquisitions were cancelled, the cache releases the returned image. Do not upload GPU resources from a worker unless the backend explicitly permits it. The SDL_GPU loader prepares CPU pixel data on a worker; its device owner performs texture upload later on the rendering thread.

## Methods

| Name | Return type | Description |
| --- | --- | --- |
| `LoadAsync(string path, CancellationToken cancellationToken = default)` | `ValueTask<IDrawImage>` | Loads a new independently owned image; failures and cancellation are reported through the returned operation. |

## See also

- [IImageLoader](Cerneala.UI.Resources.IImageLoader.md)
- [ImageResourceCache](Cerneala.UI.Resources.ImageResourceCache.md)
- [ImageResourceLease](Cerneala.UI.Resources.ImageResourceLease.md)
