# Scene2DEntity Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Scene2DDocument.cs`

Preserves object geometry, placement, role, primitive metadata, and an optional collider descriptor without creating a scene node.

```csharp
public sealed class Scene2DEntity
```

## Remarks

Shape is exactly Box, Ellipse, Polygon, Polyline, or Point. Role is exactly Metadata, Spawn, Collider, or Promote; comparisons are case-sensitive. The default Point/Metadata entity has no collider. Box/Ellipse dimensions must be positive. Polygon text uses the same strict convexity rules as PolygonCollider2D. Polyline text has at least two points, remains open, and rejects degenerate consecutive segments. Collider role requires non-point geometry and exactly one valid descriptor.

Positions and sizes use scene units. Rotation is in radians. Pivot retains normalized source anchor metadata; it does not execute a transform. Composition applies the entity's placement/rotation and its owning map/level offsets. The parser preserves geometry and the optional descriptor but does not attach Aspect, Motion, Prism, input, or gameplay.

## Constructors

| Name | Description |
| --- | --- |
| `Scene2DEntity(id, mapId, position, size, shape = "Point", points = "", rotation = 0, pivot = default, role = "Metadata", collider = null, order = 0, isVisible = true, opacity = 1, properties = null)` | Validates geometry/role and stores one optional immutable descriptor plus copied metadata. |

## Properties

| Name | Description |
| --- | --- |
| `Id`, `MapId` | Stable entity and owning map identity. |
| `Position`, `Size` | Map-local placement and dimensions. |
| `Shape`, `Points`, `Vertices` | Shape kind, original invariant point text, and read-only parsed vertices. |
| `Rotation`, `Pivot` | Finite rotation and source pivot metadata. |
| `Role` | Explicit composition role. |
| `Collider` | Optional validated local collider descriptor. It is required when `Role` is `Collider`. |
| `Order` | Source object order. |
| `IsVisible`, `Opacity` | Presentation metadata; opacity is finite in [0,1]. |
| `Properties` | Shallow copied opaque source properties/fields. |
