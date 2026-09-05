# CollisionWorld2D Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/CollisionWorld2D.cs`

Indexes a root `Scene2D` and provides exact overlap, ray, and continuous movement queries.

```csharp
public sealed class CollisionWorld2D
```

## Examples

```csharp
CollisionWorld2D world = scene.CollisionWorld;

bool touching = world.Intersects(playerCollider, doorCollider);
CollisionHit2D[] nearby = world.Overlap(playerCollider);
CollisionHit2D[] sight = world.Raycast(playerPosition, Vector2.UnitX, 128);
MoveCollisionResult2D move = world.MoveAndCollide(playerCollider, desiredMovement);
```

## Remarks

The root scene owns one collision world. Calling `CollisionWorld` on a nested `Scene2D` returns that same root-owned instance. Attach, detach, reparent, visibility, filtering, shape, offset, and transform changes update it through the normal `UiProperty` mutation path before the next query.

The broadphase is an internal sparse grid. Every public result is confirmed by an exact narrow phase, so a grid candidate is not itself a collision. Boxes and convex polygons use SAT fast paths. Similarity-transformed circles use analytic tests; non-uniformly scaled or skewed circles retain their affine ellipse and use support mapping with GJK/EPA. Continuous casts use conservative advancement and include edge contact with epsilon `1e-5` scene units.

Zero-thickness `SegmentCollider2D` shapes use the same support-mapped distance/cast path rather than area-based polygon SAT. Ray queries retain finite endpoints and collinear contact; both sides can produce contact.

Pair filtering is bilateral: `(a.CollisionMask & b.CollisionLayer) != 0` and `(b.CollisionMask & a.CollisionLayer) != 0`. A collider with zero layer or mask participates in no pair. Triggers can be queried but never block `MoveAndCollide`.

Overlap results use stable attachment order. Ray and movement results use fraction, distance, then attachment ordinal. Methods do not mutate scene nodes and do not run a physics simulation.

## Methods

| Name | Returns | Description |
| --- | --- | --- |
| `Intersects(Collider2D, Collider2D)` | `bool` | Tests one active, bilaterally compatible pair exactly. |
| `Overlap(Collider2D, CollisionQuery2D)` | `CollisionHit2D[]` | Returns exact current overlaps, excluding the source collider. |
| `Raycast(Vector2, Vector2, float, CollisionQuery2D)` | `CollisionHit2D[]` | Returns all ray hits up to the finite non-negative maximum distance. A zero direction is rejected. |
| `MoveAndCollide(Collider2D, Vector2, CollisionQuery2D)` | `MoveCollisionResult2D` | Casts a collider continuously without mutating it. |
| `GetDiagnosticsSnapshot()` | `CollisionWorld2DDiagnosticsSnapshot` | Captures current index, work, update, and timing counters. |

## Applies to

Project: `Cerneala`

## See also

- `Scene2D`
- `Collider2D`
- `CollisionQuery2D`
- `CollisionHit2D`
- `MoveCollisionResult2D`
