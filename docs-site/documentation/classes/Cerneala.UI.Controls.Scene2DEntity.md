# Scene2DEntity Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Scene2DDocument.cs`

Preserves object geometry, placement, role, primitive metadata, and collider descriptors without creating a scene node.

```csharp
public sealed class Scene2DEntity
```

## Remarks

Shape is exactly Box, Ellipse, Polygon, Polyline, or Point. Role is exactly Metadata, Spawn, Collider, or Promote; comparisons are case-sensitive. The default Point/Metadata entity has no collider. Box/Ellipse dimensions must be positive. Polygon text uses the same strict convexity rules as PolygonCollider2D. Polyline text has at least two points, remains open, and rejects degenerate consecutive segments. Collider role requires non-point geometry and at least one valid descriptor.

Positions and sizes use scene units. Rotation is in radians. Pivot retains normalized source anchor metadata; it does not execute a transform. Composition applies the entity's placement/rotation and its owning layer/level offsets. The parser preserves geometry and descriptors but does not attach Aspect, Motion, Prism, input, or gameplay.

## Constructors

| Name | Description |
| --- | --- |
| `Scene2DEntity(id, layerId, position, size, shape = "Point", points = "", rotation = 0, pivot = default, role = "Metadata", colliders = null, order = 0, isVisible = true, opacity = 1, properties = null)` | Validates geometry/role and copies descriptors and metadata. |

## Properties

| Name | Description |
| --- | --- |
| `Id`, `LayerId` | Stable entity and owning layer identity. |
| `Position`, `Size` | Layer-local placement and dimensions. |
| `Shape`, `Points`, `Vertices` | Shape kind, original invariant point text, and read-only parsed vertices. |
| `Rotation`, `Pivot` | Finite rotation and source pivot metadata. |
| `Role` | Explicit composition role. |
| `Colliders` | Read-only validated local collider descriptors. |
| `Order` | Source object order. |
| `IsVisible`, `Opacity` | Presentation metadata; opacity is finite in [0,1]. |
| `Properties` | Shallow copied opaque source properties/fields. |

