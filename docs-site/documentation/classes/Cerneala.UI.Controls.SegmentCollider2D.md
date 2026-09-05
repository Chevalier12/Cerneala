# SegmentCollider2D Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/SegmentCollider2D.cs`

Defines an open, zero-thickness, two-sided segment collider from local (0,0) to (EndX,EndY).

```csharp
public sealed class SegmentCollider2D : Collider2D
```

## Examples

```xml
<SegmentCollider2D EndX="24" EndY="8" OffsetX="4" CollisionLayer="2" />
```

## Remarks

Endpoints must be finite, distinct by more than the collision epsilon, and have finite squared distance. Use inherited OffsetX/OffsetY or scene transforms to place the starting point. The default is the unit horizontal segment. A rejected endpoint mutation leaves the previous geometry installed; when changing to a vertical segment, set a nonzero EndY before setting EndX to zero.

Ray queries include endpoints and collinear overlap. Support-mapped overlap and continuous casts retain finite endpoints and contact from either side. No thickness, implicit closing edge, extrusion, or physics simulation is added.

Like other colliders, the node emits no visual commands. Shape properties are UiProperties with AffectsHitTest; inherited visibility/filter/transform/input behavior follows Collider2D. Debug drawing is a separate presentation concern.

## Fields

| Name | Description |
| --- | --- |
| `EndXProperty`, `EndYProperty` | Identify the float endpoint UiProperties. |

## Properties

| Name | Default | Description |
| --- | ---: | --- |
| `EndX` | 1 | End X relative to the local start. |
| `EndY` | 0 | End Y relative to the local start. |

## See also

- [Collider2D](Cerneala.UI.Controls.Collider2D.md)
- [TileColliderDescriptor2D](Cerneala.UI.Controls.TileColliderDescriptor2D.md)

