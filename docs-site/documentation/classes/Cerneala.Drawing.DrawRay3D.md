# DrawRay3D Struct

## Definition
Namespace: `Cerneala.Drawing`  
Assembly/Project: `Cerneala`  
Source: `Drawing/RenderProjection3D.cs`

Stores a 3D ray origin and direction.

```csharp
public readonly record struct DrawRay3D(Vector3 Origin, Vector3 Direction);
```

## Examples
```csharp
if (surface.TryRootToWorldRay(pointerInRoot, out DrawRay3D ray))
{
    Vector3 pointOneUnitAlongRay = ray.Origin + ray.Direction;
}
```

## Remarks
When produced by `RenderSurface3D.TryRootToWorldRay`, `Origin` is on the camera near plane and `Direction` is a finite normalized vector toward the far plane for both perspective and orthographic projections. The record constructor itself does not validate or normalize caller-supplied vectors. Conversion returns `false` outside the viewport or when the required transform/camera inverse is unavailable; it writes `default` to the output in that case. The control supplies geometry for selection, not a picking policy.

## Properties
| Name | Description |
| --- | --- |
| `Origin` | Ray start in world coordinates. |
| `Direction` | Ray direction; normalized only for successful control conversion. |

## Applies to
`Cerneala`
