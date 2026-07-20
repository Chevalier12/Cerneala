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
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Graph;

using IBackdropFrameLease lease = new FrameBackdropLease(metadata);
PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(new DrawCommandList());
DrawingFrameContext context = new(analysis, lease);

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

The interface intentionally exposes no graphics API, texture, or resource-ownership member. A hosting integration attaches a concrete lease to `DrawingFrameContext` without making the generic drawing contract depend on MonoGame or another graphics backend.

The application keeps ownership of the rendered scene and its graphics resources. The lease borrows them only until the current drawing submission ends. `UiHost` disposes an acquired lease exactly once after submission, including when the drawing backend throws; consumers must not cache or use the lease in another frame.

Backend adapters may derive a graphics-API-specific lease from this interface.
For MonoGame, `IMonoGameBackdropFrameLease` exposes the borrowed `Texture2D`
without changing the backend-neutral host contract or transferring ownership.

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
- `Cerneala.Drawing.MonoGame.Prism.IMonoGameBackdropFrameLease`
- `Cerneala.Drawing.Prism.IBackdropFrameSource`
- `Cerneala.Drawing.Prism.Graph.PrismBackdropRequirement`
