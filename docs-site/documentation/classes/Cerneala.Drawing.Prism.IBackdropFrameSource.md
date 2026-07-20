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

`UiHost` validates the source against the selected drawing backend when the backend is assigned. It acquires only when the single `PrismFrameAnalysis` reports a backdrop requirement, and it acquires at most one lease for that drawing frame. A compatible source must return a non-null lease per request. The host disposes the lease exactly once after submission, including exceptional exits; disposing the lease releases the borrow, not the application-owned scene.

Compatibility describes whether the backend can consume the leases, not whether
the source owns that backend instance. For example, the WindowsDX source accepts
live MonoGame backends on the same `GraphicsDevice`, including the temporary
backend used for screenshot rendering.

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
