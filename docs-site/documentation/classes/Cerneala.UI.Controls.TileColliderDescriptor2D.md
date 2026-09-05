# TileColliderDescriptor2D Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/TileColliderDescriptor2D.cs`

Describes immutable tile-local collision geometry and filtering metadata for a `TileDefinition2D`.

```csharp
public sealed class TileColliderDescriptor2D
```

## Examples

```csharp
var fence = new TileColliderDescriptor2D(
    TileColliderShape2D.Box,
    width: 16,
    height: 4,
    offsetY: 12,
    collisionLayer: 2,
    collisionMask: 1,
    debugIdentity: "village-fence",
    properties: new Dictionary<string, object?>
    {
        ["material"] = "wood"
    });

var fenceTile = new TileDefinition2D(
    42,
    new DrawRect(0, 0, 16, 16),
    colliders: [fence]);
```

## Remarks

Descriptors are model data, not `SceneNode2D` instances and not backend drawing primitives. `TileMap2D` adapts them into the scene collision world while the corresponding cells remain batched tile data.

Offsets and shape coordinates use tile-local destination units. The adapter applies the shape offset, `LocalTransform`, diagonal/horizontal/vertical cell flips, cell placement, layer offset, and inherited scene transforms in that order. Rendering culling does not unload these active collision shapes.

The original constructor uses identity `LocalTransform`. The affine overload requires a finite invertible matrix and validates resulting geometry in the drawing coordinate range. A nonuniformly transformed circle is an exact ellipse; it is not approximated by a circle or polygon. `Segment` requires exactly two points in invariant `x,y x,y` syntax and remains zero-thickness and two-sided.

Full-cell boxes may be coalesced horizontally inside one chunk only when the local affine transform is identity and every collision field, `DebugIdentity`, property key, and property value are equal. Property values use their normal `Equals` semantics. Changing metadata, filtering, trigger state, geometry, or offsets therefore preserves the corresponding semantic boundary.

## Constructors

| Name | Description |
| --- | --- |
| `TileColliderDescriptor2D(TileColliderShape2D, float, float, float, string, float, float, uint, uint, bool, string?, IReadOnlyDictionary<string, object?>?)` | Creates and validates an immutable tile collider descriptor and copies its optional metadata. |
| `TileColliderDescriptor2D(TileColliderShape2D, Matrix3x2, float, float, float, string, float, float, uint, uint, bool, string?, IReadOnlyDictionary<string, object?>?)` | Supplies an explicit local affine transform; other optional arguments retain their defaults. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Shape` | `TileColliderShape2D` | Shape selected for this descriptor. |
| `LocalTransform` | `Matrix3x2` | Finite invertible transform applied after shape offset and before tile flips/placement. |
| `Width` | `float` | Box width. Must be finite and positive when `Shape` is `Box`. |
| `Height` | `float` | Box height. Must be finite and positive when `Shape` is `Box`. |
| `Radius` | `float` | Circle radius. Must be finite and positive when `Shape` is `Circle`. |
| `Points` | `string` | Original polygon or segment point list. |
| `Vertices` | `IReadOnlyList<Vector2>` | Parsed polygon/segment vertices; empty for other shapes. |
| `OffsetX`, `OffsetY` | `float` | Finite tile-local shape offset. |
| `CollisionLayer` | `uint` | Collision bits assigned to adapted colliders. The default is `1`. |
| `CollisionMask` | `uint` | Collision bits accepted by adapted colliders. The default is `uint.MaxValue`. |
| `IsTrigger` | `bool` | Whether adapted colliders report contacts without blocking movement. |
| `DebugIdentity` | `string?` | Optional semantic/debug identity that also prevents incompatible coalescing. |
| `Properties` | `IReadOnlyDictionary<string, object?>` | Copied opaque importer metadata used when checking whether adjacent colliders are semantically identical. |

## Exceptions

- `ArgumentOutOfRangeException` is thrown for an undefined shape, non-finite offsets, or non-positive or non-finite active dimensions.
- `ArgumentException` is thrown for invalid polygon text or geometry. Polygon validation uses the same public contract as `PolygonCollider2D`: at least three finite vertices forming a strictly convex polygon are required.

## Applies to

Project: `Cerneala`

## See also

- [TileColliderShape2D](Cerneala.UI.Controls.TileColliderShape2D.md)
- [TileDefinition2D](Cerneala.UI.Controls.TileDefinition2D.md)
- [TileMap2D](Cerneala.UI.Controls.TileMap2D.md)
- [Collider2D](Cerneala.UI.Controls.Collider2D.md)
