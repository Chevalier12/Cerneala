# TileColliderShape2D Enum

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/TileColliderDescriptor2D.cs`

Identifies the collision shape stored by a tile definition.

```csharp
public enum TileColliderShape2D
```

## Examples

```csharp
var fenceCollider = new TileColliderDescriptor2D(
    TileColliderShape2D.Box,
    width: 16,
    height: 4,
    offsetY: 12);
```

## Remarks

The value determines which descriptor dimensions are validated and used. `Box` uses `Width` and `Height`, `Circle` uses `Radius`, and `Polygon`/`Segment` use `Points` and the parsed `Vertices` collection. A local affine transform preserves exact ellipses through a transformed circle.

## Fields

| Name | Description |
| --- | --- |
| `Box` | An axis-aligned box in tile-local coordinates before tile and scene transforms are applied. |
| `Circle` | A circle in tile-local coordinates before tile and scene transforms are applied. |
| `Polygon` | A convex polygon described by tile-local vertices. |
| `Segment` | Exactly two distinct vertices defining a zero-thickness two-sided segment. |

## Applies to

Project: `Cerneala`

## See also

- [TileColliderDescriptor2D](Cerneala.UI.Controls.TileColliderDescriptor2D.md)
- [TileDefinition2D](Cerneala.UI.Controls.TileDefinition2D.md)
