# ImageResourceCache Class

## Definition

Namespace: `Cerneala.UI.Resources`

Assembly/Project: `Cerneala`

Source: `UI/Resources/ImageResourceCache.cs`

Shares acquired path-backed images and releases each owned image when its last acquisition ends.

```csharp
public sealed class ImageResourceCache : IDisposable, IAsyncDisposable
```

## Examples

Keep an acquisition alive for as long as its image is needed:

```csharp
using Cerneala.UI.Resources;

static void ShareAtlas(IImageLoader loader)
{
    using ImageResourceCache cache = new(loader);
    ImageResource atlas = new("Assets/atlas.png");
    using ImageResourceLease scene = cache.Acquire(atlas);
    using ImageResourceLease secondConsumer = scene.Retain();

    Console.WriteLine(ReferenceEquals(scene.Image, secondConsumer.Image)); // True
    scene.Dispose(); // The second acquisition still keeps the image alive.
    Console.WriteLine(secondConsumer.Image.Width);
}
```

Pass a loader appropriate to the drawing backend. Prepare an image asynchronously with a loader that explicitly supports asynchronous work:

```csharp
using Cerneala.UI.Resources;

static async Task PrepareAtlasAsync(IAsyncImageLoader loader, CancellationToken cancellationToken)
{
    await using ImageResourceCache cache = new(loader, maximumConcurrentLoads: 4);
    using ImageResourceLease lease = await cache.AcquireAsync(
        new ImageResource("Assets/atlas.png"), cancellationToken);
    Console.WriteLine(lease.Image.Width);
}
```

## Remarks

`Acquire` replaces the former raw-image `Resolve` cache API. It returns an [ImageResourceLease](Cerneala.UI.Resources.ImageResourceLease.md), not ownership of a raw `IDrawImage`. Overlapping acquisitions of the same ordinal path identity share one loaded image. The cache does not keep an additional permanent acquisition: releasing the last lease removes the entry and disposes its image if it implements `IDisposable`. A later acquisition loads it again.

`Acquire` is synchronous: a missing image is loaded on the acquiring thread. `AcquireAsync` uses [IAsyncImageLoader](Cerneala.UI.Resources.IAsyncImageLoader.md) for a new path-backed load and never silently wraps an arbitrary synchronous loader in background work. It can acquire an embedded/resident image or await an existing load without that interface. The concurrency limit bounds asynchronous queued/running loads, not explicit synchronous loads.

Concurrent acquisitions share a pending load. Concurrent loads for different identities may call the configured loader concurrently, so a loader used that way must support it. A loader failure is propagated to waiting acquisitions and the failed entry is removed when those requests finish. Recursive acquisition of the same pending identity in the loader's flowing execution context throws instead of deadlocking. A loader must not return an image already owned by a different live entry in this cache.

Ordinary UI [Image](Cerneala.UI.Controls.Image.md) controls hold preparation interest in this same cache without blocking the UI frame. Their interest may be pending, ready or failed; the control retains a failed preparation until its source/acquisition is replaced or released, rather than retrying on every frame. These consumers expose loading/error state and do not change the synchronous contract of explicit `Acquire` calls. `Remove`/`Clear` do not notify UI consumers or schedule a frame; an application retry of a failed path must also invalidate the affected control or change its source.

Cancellation releases only that request's interest. The shared load is cancelled when its last requester leaves; a queued cancelled load does not call the loader. A loader that ignores cancellation may finish later: the cache disposes that abandoned result instead of publishing it into a replacement entry. Await `DisposeAsync` to drain outstanding load/cleanup work and observe abandoned-result disposal failures. Normal load failures are delivered to requesting acquisitions. `DisposeAsync` does not wait for consumers to dispose successfully acquired leases and does not revoke those leases.

`Remove` and `Clear` forget identity mappings without revoking acquisitions. A subsequent acquisition can load a replacement while the previous image is still in use. `Dispose` additionally rejects new acquisitions. Existing leases remain valid and can be retained after cache disposal; their final release still disposes the owned image. None of these operations forcibly invalidates another consumer's lease.

Embedded images remain caller-owned. Acquiring an embedded resource creates a borrowed-image lease; disposing any of its leases, or the cache, never disposes that image. The external owner must keep it alive independently.

Do not dispose `lease.Image` directly. Drawing commands and sprite batches that borrow an image are not substitutes for an acquisition; an application retaining such values must keep its lease alive until they are no longer used. Image disposal runs on the thread releasing the last lease; the cache does not dispatch callbacks to a UI thread.

## Constructors

| Name | Description |
| --- | --- |
| `ImageResourceCache(IImageLoader? loader, int maximumConcurrentLoads = 4)` | Sets the loader and positive asynchronous load limit. A null loader permits embedded resources only. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `LoadCount` | `int` | Cumulative successful path-backed loads registered by this cache; hits and embedded images are excluded. |
| `ResidentCount` | `int` | Owned loaded images with outstanding acquisitions, including images whose identity was removed or whose cache was disposed. Pending loads and embedded images are excluded. |
| `PendingLoadCount` | `int` | Queued/running loads, including abandoned loads that have not finished cleanup. |

## Methods

| Name | Return type | Description |
| --- | --- | --- |
| `Acquire(ImageResource resource)` | `ImageResourceLease` | Acquires an embedded, resident or synchronously loaded image. |
| `AcquireAsync(ImageResource resource, CancellationToken cancellationToken = default)` | `ValueTask<ImageResourceLease>` | Acquires an image asynchronously; cancellation affects only this request's interest. |
| `Remove(ImageResource resource)` | `void` | Forgets the current identity mapping without invalidating existing leases. |
| `Clear()` | `void` | Forgets all current identity mappings without invalidating existing leases. |
| `Dispose()` | `void` | Closes new acquisitions and forgets mappings. Repeated disposal is harmless. |
| `DisposeAsync()` | `ValueTask` | Closes acquisition, awaits outstanding loading/cleanup operations and reports late release failures. Existing leases remain valid. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| Constructor | `ArgumentOutOfRangeException` | The asynchronous load limit is not positive. |
| `Acquire`, `AcquireAsync`, `Remove` | `ArgumentNullException` | Resource is null. |
| `Acquire`, `AcquireAsync` | `ObjectDisposedException` | The cache has been disposed. |
| `Acquire`, `AcquireAsync` | `InvalidOperationException` | A path-backed image has no loader, the loader returns null or another live entry's image, or the loader recursively acquires its own pending identity. |
| `Acquire`, `AcquireAsync` | Loader exception | Loading or decoding fails. |
| `AcquireAsync` | `NotSupportedException` | A new path-backed load requires a loader implementing `IAsyncImageLoader`. |
| `AcquireAsync` | `OperationCanceledException` | The request is cancelled. Cleanup failures may be combined with cancellation in an `AggregateException`. |
| `DisposeAsync` | `AggregateException` | Cleanup of an abandoned late result failed. |

## See also

- [ImageResourceLease](Cerneala.UI.Resources.ImageResourceLease.md)
- [ImageResource](Cerneala.UI.Resources.ImageResource.md)
- [IImageLoader](Cerneala.UI.Resources.IImageLoader.md)
