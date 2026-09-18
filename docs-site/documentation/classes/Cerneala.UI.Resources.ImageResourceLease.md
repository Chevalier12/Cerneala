# ImageResourceLease Class

## Definition

Namespace: `Cerneala.UI.Resources`

Assembly/Project: `Cerneala`

Source: `UI/Resources/ImageResourceLease.cs`

Keeps one acquired image available until this lease is disposed.

```csharp
public sealed class ImageResourceLease : IDisposable
```

## Remarks

Obtain a lease from [ImageResourceCache.Acquire](Cerneala.UI.Resources.ImageResourceCache.md). There is no public constructor. `Retain` creates an independent lease for the same image; it does not reload or copy pixels. Disposing one lease does not invalidate the others. Cache-owned disposable images are disposed on final release; embedded caller-owned images are never disposed by these leases.

`Image` and `Retain` throw `ObjectDisposedException` after this lease is disposed, even if another lease still owns the image. Disposal is idempotent and safe against concurrent duplicate calls. Retaining and disposing the same lease are serialized: retention either obtains its own acquisition or throws. Do not use the raw image concurrently with releasing the lease that protects that use.

Disposal removes this lease's image and callback references before invoking its release action. A failing release is propagated, but cannot be repeated by disposing the same lease again. Final image disposal runs on the releasing thread without implicit UI dispatch. Do not call `Dispose` on the raw `Image`; release the lease instead.

Keep a lease for as long as application-owned drawing commands, batches or controls borrow its raw image. A raw reference alone does not extend the acquired lifetime. Cache disposal does not revoke existing leases, and a live lease can still be retained after its cache is disposed.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Image` | `IDrawImage` | The acquired image while this lease is alive. |

## Methods

| Name | Return type | Description |
| --- | --- | --- |
| `Retain()` | `ImageResourceLease` | Creates a separately releasable acquisition of the same image. |
| `Dispose()` | `void` | Releases this acquisition at most once. |

## See also

- [ImageResourceCache](Cerneala.UI.Resources.ImageResourceCache.md)
- [IDrawImage](Cerneala.Drawing.IDrawImage.md)
