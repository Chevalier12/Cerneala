# CollisionQuery2D Struct

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/CollisionQuery2D.cs`

Defines immutable layer, mask, trigger, and exclusion filters for collision queries.

```csharp
public readonly struct CollisionQuery2D
```

## Examples

```csharp
CollisionHit2D[] solids = scene.CollisionWorld.Overlap(
    playerCollider,
    new CollisionQuery2D(
        collisionLayer: 1,
        collisionMask: 4,
        includeTriggers: false,
        exclude: playerCollider));
```

## Remarks

The zero-initialized value (`default`) means all layer bits, all mask bits, include triggers, and no excluded collider. Filtering is bilateral: a candidate passes when the query mask includes its layer and the candidate mask includes the query layer. A zero query layer or mask matches nothing.

`Exclude` removes one collider before exact shape testing. It does not alter that collider or the scene world.

## Constructors

| Name | Description |
| --- | --- |
| `CollisionQuery2D(uint, uint, bool, Collider2D?)` | Creates a filter. All parameters are optional and use the `default` semantics described above. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `CollisionLayer` | `uint` | Query-side layer bits used by bilateral filtering. |
| `CollisionMask` | `uint` | Candidate layer bits accepted by the query. |
| `IncludeTriggers` | `bool` | Whether trigger colliders may be returned. |
| `Exclude` | `Collider2D?` | One collider omitted from the query, or `null`. |

## Applies to

Project: `Cerneala`

## See also

- `CollisionWorld2D`
- `Collider2D`
