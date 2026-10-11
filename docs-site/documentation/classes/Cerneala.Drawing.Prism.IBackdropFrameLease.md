# IBackdropFrameLease Interface

## Definition
Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/IBackdropFrameLease.cs`

Represents a backend-neutral, frame-scoped borrow of backdrop input and its immutable metadata.

```csharp
public interface IBackdropFrameLease : IDisposable
```

## Examples

```csharp
using Cerneala.Drawing.Prism;

static IBackdropFrameLease Borrow(BackdropFrameMetadata metadata) =>
    new FrameBackdropLease(metadata);

sealed class FrameBackdropLease(BackdropFrameMetadata metadata)
    : IBackdropFrameLease
{
    public BackdropFrameMetadata Metadata { get; } = metadata;

    public void Dispose()
    {
    }
}
```

## Remarks

The interface intentionally exposes no graphics API, texture, or resource-ownership member. An application supplies leases through an `IBackdropFrameSource`. `UiHost` acquires the lease and places it in the `DrawingFrameContext` it passes to the drawing backend; `DrawingFrameContext` has no public constructor, so application code cannot attach a lease directly. The generic drawing contract therefore does not depend on a graphics backend.

The application keeps ownership of the rendered scene and its graphics resources. The lease borrows them only until the current drawing submission ends. `UiHost` disposes an acquired lease exactly once after submission, including when the drawing backend throws; consumers must not cache or use the lease in another frame.

Backend adapters may implement a graphics-API-specific lease behind this
interface without changing the backend-neutral host contract or transferring ownership.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Metadata` | `BackdropFrameMetadata` | Gets the immutable raster, color, coordinate, and content-version metadata for the borrowed frame. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Dispose()` | `void` | Ends the frame-scoped borrow without disposing the application-owned scene. |

## Applies to

Cerneala backdrop-aware frame hosting and backend composition.

## See also

- `Cerneala.Drawing.DrawingFrameContext`
- `Cerneala.Drawing.Prism.IBackdropFrameSource`
