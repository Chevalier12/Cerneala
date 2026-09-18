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

`GetAuthoringBounds()` returns the map-local axis-aligned bounds of the authored
shape after entity rotation and translation. A Point has zero area regardless of
`Size`; Polygon/Polyline use the parsed vertices, and Ellipse uses its transformed
elliptical extents rather than the bounds of a rotated rectangle. `Pivot`, owning
map offsets and level offsets are not applied by this method.

These are **authoring bounds**, not the envelope of an application template or a
moving NPC. A game's spatial adapter must declare any larger visual or simulation
envelope. `GetCollisionBounds()` independently bounds the optional descriptor,
including its offset and local affine transform followed by entity placement.
The collider may extend outside the authoring bounds. Neither method creates a
node, loads a payload or reads images/properties from a file.

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

## Methods

| Name | Description |
| --- | --- |
| `GetAuthoringBounds()` | Returns map-local authored geometry as a `DrawRect`. Throws `InvalidOperationException` if finite bounds cannot be computed, or `ArgumentOutOfRangeException` when bounds violate the existing drawing coordinate range. |
| `GetCollisionBounds()` | Returns the map-local descriptor bounds, or `null` when there is no descriptor. Throws `ArgumentException` if the transformed geometry is not finite, or `ArgumentOutOfRangeException` for drawing-range violations. |

Existing constructor validation is unchanged. `DrawPoint` and `DrawSize` already
reject nonfinite components. `DrawRect` additionally limits coordinates, sizes and
computed edges to its drawing range. A finite authoring value can exceed that
range or overflow after transformation; neither failure becomes an empty region.

## See also

- [SceneSpatialEntry2D](Cerneala.UI.Controls.SceneSpatialEntry2D.md)
- [DrawRect](Cerneala.Drawing.DrawRect.md)
- [Scene2DPackageEntityInfo](Cerneala.Scene2D.Packages.Scene2DPackageEntityInfo.md)
