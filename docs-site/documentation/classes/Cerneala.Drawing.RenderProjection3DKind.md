# RenderProjection3DKind Enumeration

## Definition
Namespace: `Cerneala.Drawing`  
Assembly/Project: `Cerneala`  
Source: `Drawing/RenderProjection3D.cs`

Identifies the projection represented by a `RenderProjection3D` descriptor.

```csharp
public enum RenderProjection3DKind { Perspective, Orthographic }
```

## Examples
```csharp
RenderProjection3D projection = RenderProjection3D.Orthographic(4, 0.01f, 100);
bool isOrthographic = projection.Kind == RenderProjection3DKind.Orthographic;
```

## Remarks
Create a descriptor with `RenderProjection3D.Perspective` or `Orthographic`; the tag and scalar are set together. The all-zero default descriptor is invalid, even though it has the enum's zero value.

| Value | Meaning |
| --- | --- |
| `Perspective` (`0`) | The scalar is a vertical field of view in radians. |
| `Orthographic` (`1`) | The scalar is a vertical view height. |

## Applies to
`Cerneala`
