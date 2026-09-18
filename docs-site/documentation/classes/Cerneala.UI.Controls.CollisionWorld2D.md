# CollisionWorld2D Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/CollisionWorld2D.cs`

Region preparation: `UI/Controls/CollisionWorld2D.Streaming.cs`

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

Noninitial movement impacts resolve equal polygon contact axes using the cast's approach direction, rather than the static overlap axis preference. This preserves a floor's surface normal at introduced collider/chunk seams. Curved analytic contacts retain their geometric normals; support-mapped impacts use the separating approach normal. Initial touching/overlap keeps its static normal and zero travel. The approach direction is not an automatic response or a replacement for the surface normal on oblique casts.

Pair filtering is bilateral: `(a.CollisionMask & b.CollisionLayer) != 0` and `(b.CollisionMask & a.CollisionLayer) != 0`. A collider with zero layer or mask participates in no pair. Triggers can be queried but never block `MoveAndCollide`.

Overlap results use stable attachment order. Ray and movement results use fraction, distance, then attachment ordinal. Methods do not mutate scene nodes and do not run a physics simulation.

When the scene has a UI or independent simulation owner, queries and diagnostics require its owner thread. Permanent in-memory colliders can still be queried without a context; asynchronous source preparation requires the explicit lifecycle below.

### Spatial data readiness

For [SceneItems2D](Cerneala.UI.Controls.SceneItems2D.md) sources and spatially resident [TileMap2D](Cerneala.UI.Controls.TileMap2D.md) grids, `Overlap`, `Raycast`, and `MoveAndCollide` require current collision payloads/adapters throughout their query rectangle. Missing or obsolete data raises [SceneCollisionRegionNotReadyException](Cerneala.UI.Controls.SceneCollisionRegionNotReadyException.md), rather than treating unloaded terrain as empty. The methods do not load data, move a node, or replay an old input after loading. `Intersects` tests only its two specified live colliders and does not require surrounding terrain. Empty collision filters need no coverage.

Use `PrepareRegionAsync` before querying distant terrain or teleporting. Its rectangle uses the same scene coordinates as collision queries, not root DIPs or a nested source's local coordinates. Include the complete overlap/ray/swept movement envelope. Preparation includes the existing narrow-phase contact epsilon; callers do not add arbitrary padding for that tolerance.

```csharp
// Run from the attached root's UI context; keep its normal frame loop running.
using SceneCollisionRegion2D region = await scene.CollisionWorld.PrepareRegionAsync(
    new DrawRect(2000, 0, 100, 100), cancellationToken);
CollisionHit2D[] hits = scene.CollisionWorld.Raycast(new Vector2(2000, 50), Vector2.UnitX, 100);
```

The returned lease retains collision interest until disposed. Overlapping leases and camera/simulation interests share payloads. Releasing the last interest permits detachment and payload release; it does not keep all previously visited terrain resident. `IsReady` is live: changing the catalog, payload version, template, or attachment can invalidate readiness after preparation. The query checks again, so an old lease is not permission to use stale collision geometry.

A catalog applied on the owning UI thread is immediately authoritative. Removing an entry or replacing its payload version retires the old node and collision geometry without waiting for unrelated loading. A previously occupied area can then be ready and empty because the object was explicitly deleted, which is different from declaring an unloaded new object absent. Unchanged simulated nodes retain identity and state; new collision data must still be prepared.

A worker can publish a catalog before its UI notification is drained. Until the root applies it, queries intersecting the affected retained collision envelope report an unprepared region instead of returning stale geometry. Queries do not drain the relay, instantiate templates, or perform hidden I/O. Keep the normal root loop running; direct UI-thread publication applies retirement synchronously.

Simulated spatial entries automatically retain terrain intersecting their published `CollisionBounds`, even off camera. Distant simulated envelopes remain separate interests. This is not a prediction of arbitrary future movement: prepare any query extending beyond currently available data explicitly. Direct authored sprites do not implicitly declare simulation regions. Explicitly hidden sources do not participate; camera retirement is not explicit hiding.

Call preparation on the simulation owner's thread and await it without synchronously blocking that thread. An attached surface supplies the context and uses the root relay/frame loop. Without UI, create [SceneSimulationContext2D](Cerneala.UI.Controls.SceneSimulationContext2D.md) for an unowned root scene and keep its ordinary `Update` loop running. The same source residency/publication/coverage path is used in both cases; there is no synchronous in-memory-source bypass. Required source failures propagate; a failed unrelated source outside the collision region does not block it. Cancellation, context disposal, detach, or a superseded source preparation can cancel an unfinished request. There is no automatic replay or unbounded retry. Losing the owner invalidates outstanding leases permanently, including after reattachment.

Coverage depends on truthful source metadata. A provider must publish a conservative collision envelope independently of its visual bounds; explicit null means no collision data. The world cannot discover missing shapes in an unloaded payload with incorrect metadata. Grid catalogs declare collision envelopes and validate acquired payloads against them. Collision interests retain the needed chunk data and adapters, not atlas images. `TileMapSource2D.FromModel` is an in-memory adapter whose source-owned backing remains resident; unloading acquisition-owned data requires a source that does not retain every payload independently.

## Methods

| Name | Returns | Description |
| --- | --- | --- |
| `Intersects(Collider2D, Collider2D)` | `bool` | Tests one active, bilaterally compatible pair exactly. |
| `Overlap(Collider2D, CollisionQuery2D)` | `CollisionHit2D[]` | Returns exact current overlaps, excluding the source collider. |
| `Raycast(Vector2, Vector2, float, CollisionQuery2D)` | `CollisionHit2D[]` | Returns all ray hits up to the finite non-negative maximum distance. A zero direction is rejected. |
| `MoveAndCollide(Collider2D, Vector2, CollisionQuery2D)` | `MoveCollisionResult2D` | Casts a collider continuously without mutating it. |
| `PrepareRegionAsync(DrawRect, CancellationToken)` | `ValueTask<SceneCollisionRegion2D>` | Prepares source collision data in a finite, nonnegative scene-space rectangle and returns a disposable interest. Requires an independent or UI-hosted simulation context and its owner thread. |
| `GetDiagnosticsSnapshot()` | `CollisionWorld2DDiagnosticsSnapshot` | Captures current index, work, update, and timing counters. |

## Applies to

Project: `Cerneala`

## See also

- `Scene2D`
- `Collider2D`
- `CollisionQuery2D`
- `CollisionHit2D`
- `MoveCollisionResult2D`
- [SceneCollisionRegion2D](Cerneala.UI.Controls.SceneCollisionRegion2D.md)
- [SceneCollisionRegionNotReadyException](Cerneala.UI.Controls.SceneCollisionRegionNotReadyException.md)
