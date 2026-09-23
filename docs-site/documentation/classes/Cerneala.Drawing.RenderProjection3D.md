# RenderProjection3D Struct

## Definition
Namespace: `Cerneala.Drawing`  
Assembly/Project: `Cerneala`  
Source: `Drawing/RenderProjection3D.cs`

Describes the projection used by a `RenderSurface3D` camera.

```csharp
public readonly record struct RenderProjection3D
```

## Examples
```csharp
surface.Projection = RenderProjection3D.Perspective(MathF.PI / 3, 0.01f, 1000);
// Or keep the apparent size of geometry constant with depth:
surface.Projection = RenderProjection3D.Orthographic(4, 0.01f, 1000);
```

## Remarks
The factories validate finite values and `0 < nearPlane < farPlane`. Perspective vertical FOV must be strictly between zero and π radians; orthographic height must be positive. Invalid factory arguments throw `ArgumentOutOfRangeException`. `default(RenderProjection3D)` is invalid and cannot be assigned to `RenderSurface3D.Projection`. The projection matrix uses the current physical raster aspect ratio; the control's initial projection is perspective with FOV π/3, near `0.01`, far `1000`.

`RenderSurface3D` uses right-handed, Y-up coordinates, view-space forward `-Z`, and row-vector `model * view * projection` order. Clip-space x/y are `[-w,+w]`, z is `[0,w]`.

## Properties
| Name | Description |
| --- | --- |
| `Kind` | Perspective or orthographic tag. |
| `VerticalFieldOfViewOrHeight` | Vertical FOV in radians for perspective, vertical world-space height for orthographic. |
| `NearPlane`, `FarPlane` | Positive near and greater far clipping distances. |

## Methods
| Name | Description |
| --- | --- |
| `Perspective(float verticalFieldOfView, float nearPlane, float farPlane)` | Creates a validated perspective descriptor. |
| `Orthographic(float height, float nearPlane, float farPlane)` | Creates a validated orthographic descriptor. |

## Applies to
`Cerneala`
