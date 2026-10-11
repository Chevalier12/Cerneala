# IBackdropFrameSource Interface

## Definition
Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/IBackdropFrameSource.cs`

Provides a frame-scoped, readonly lease over an application-owned scene that has already been rendered.

```csharp
public interface IBackdropFrameSource
```

## Examples

```csharp
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;

sealed class SceneFrameSource(BackdropFrameMetadata metadata)
    : IBackdropFrameSource
{
    public bool IsCompatibleWith(IDrawingBackend drawingBackend) => true;

    public IBackdropFrameLease AcquireFrame(
        in BackdropFrameRequest request) =>
        new SceneFrameLease(metadata);
}

sealed class SceneFrameLease(BackdropFrameMetadata metadata)
    : IBackdropFrameLease
{
    public BackdropFrameMetadata Metadata { get; } = metadata;

    public void Dispose()
    {
    }
}
```

## Remarks

The application retains ownership of its scene, render targets, textures, and source object. Cerneala borrows only the already-rendered frame represented by the returned lease. `AcquireFrame` must not transfer texture ownership, and callers must not retain the lease after the drawing frame ends.

`UiHost` validates the source when `UiHost.Backend` is assigned: the setter throws `InvalidOperationException` when the backend has a source but no drawing backend, or when `IsCompatibleWith` returns `false`. `UiHost.Draw(IDrawingBackend)` passes the configured source only when its argument is the configured backend's drawing backend; for any other drawing backend it passes no source. The host acquires only when its internal analysis of the submitted command list requires a backdrop, and it acquires at most one lease for that drawing frame. When a backdrop is required but no source is configured, the host records the failure in its internal counters and draws without a lease; it does not throw. A compatible source must return a non-null lease per request: a `null` lease makes the draw throw `InvalidOperationException`, and an exception from `AcquireFrame` is recorded and rethrown. The host disposes the lease exactly once after submission, including exceptional exits; disposing the lease releases the borrow, not the application-owned scene.

Compatibility describes whether the backend can consume the leases, not whether
the source owns that backend instance. Concrete sources enforce their own
device and lifetime compatibility requirements.

Providers must keep `BackdropFrameMetadata.ContentVersion` monotonic and update it whenever source pixels or pixel-affecting metadata change.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `IsCompatibleWith(IDrawingBackend)` | `bool` | Reports whether leases from this source can be consumed by the specified live drawing backend. |
| `AcquireFrame(in BackdropFrameRequest)` | `IBackdropFrameLease` | Acquires the one frame-scoped lease shared by all compatible backdrop consumers in the request. |

## Applies to

Cerneala host integrations that expose application-owned scene pixels to Prism.

## See also

- `Cerneala.Drawing.Prism.IBackdropFrameLease`
- `Cerneala.Drawing.Prism.BackdropFrameRequest`
- `Cerneala.UI.Hosting.IUiBackend`
