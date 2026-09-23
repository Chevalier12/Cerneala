# RenderSurface3DFrame Class

## Definition
Namespace: `Cerneala.UI.Controls`  
Assembly/Project: `Cerneala`  
Source: `UI/Controls/RenderSurface3DFrame.cs`

Provides the callback-scoped writer and immutable camera/viewport snapshot for a `RenderSurface3D` recording.

```csharp
public sealed class RenderSurface3DFrame
```

## Examples
```csharp
surface.Draw += (_, frame) =>
{
    frame.DrawMarker(Vector3.Zero, Color.Coral, diameter: 8);
    frame.DrawLineBatch(lines, Matrix4x4.Identity);
};
```

## Remarks
The framework creates the frame; it has no public constructor. Every operation validates lifetime before its other work. Calls after callback completion or abort throw `ObjectDisposedException`, including empty batches. Empty active batches are no-ops. Batch data is copied during the call, so caller storage may be reused after it returns.

Matrices follow `local * model * view * projection`. Models must be finite and affine; singular affine models are allowed. Thickness and diameter are finite positive local DIPs. Primitive alpha must be 255. Invalid vectors, colors, sizes, or models throw `ArgumentOutOfRangeException`; a batch whose native byte count overflows `int` throws `OverflowException`.

Lines are finite, butt-ended segments with thickness measured in local DIPs. Markers are camera-facing circular discs whose center supplies their depth and whose diameter is measured in local DIPs; they are not spheres. The backend rasterizes their coverage into the isolated depth-tested 3D pass. A callback that records no lines or markers completes normally and leaves only the control's clear color in that pass.

## Properties
| Name | Description |
| --- | --- |
| `Bounds` | Logical local viewport bounds. |
| `PixelWidth`, `PixelHeight` | Physical raster dimensions, rounded up from logical size and scale. |
| `RasterScale` | Captured physical-pixels-per-DIP scale. |
| `FrameTime` | Captured framework frame time. |
| `ViewMatrix`, `ProjectionMatrix` | Captured matrices shared by all commands in this frame. |

## Methods
| Name | Description |
| --- | --- |
| `void DrawLine(Vector3 start, Vector3 end, Color color, float thickness = 1)` | Records one opaque line using the identity model matrix. |
| `void DrawLine(Vector3 start, Vector3 end, Color color, Matrix4x4 model, float thickness = 1)` | Records one opaque line using `model`. |
| `void DrawMarker(Vector3 center, Color color, float diameter = 1)` | Records one opaque camera-facing marker using the identity model matrix. |
| `void DrawMarker(Vector3 center, Color color, Matrix4x4 model, float diameter = 1)` | Records one opaque camera-facing marker using `model`. |
| `void DrawLineBatch(ReadOnlySpan<DrawLineSegment3D> lines)` | Copies and records `lines` using the identity model matrix. |
| `void DrawLineBatch(ReadOnlySpan<DrawLineSegment3D> lines, Matrix4x4 model)` | Copies and records `lines` using `model`. |
| `void DrawMarkerBatch(ReadOnlySpan<DrawMarker3D> markers)` | Copies and records `markers` using the identity model matrix. |
| `void DrawMarkerBatch(ReadOnlySpan<DrawMarker3D> markers, Matrix4x4 model)` | Copies and records `markers` using `model`. |

All eight methods throw `ObjectDisposedException` when the callback has completed or aborted. Lifetime validation occurs before empty-span handling. Active empty spans record nothing and do not validate the unused model matrix.

## Applies to
`Cerneala`
