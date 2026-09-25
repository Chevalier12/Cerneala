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

For a [TileMap2D](Cerneala.UI.Controls.TileMap2D.md) created from a complete model or a prepared package, `Overlap`, `Raycast`, and `MoveAndCollide` require current collision chunks/adapters throughout their query rectangle. A missing chunk raises [SceneCollisionRegionNotReadyException](Cerneala.UI.Controls.SceneCollisionRegionNotReadyException.md), rather than treating unprepared terrain as empty. Its `EntryId` is the real source-local missing chunk ID, not a synthetic collection index. [SceneItems2D](Cerneala.UI.Controls.SceneItems2D.md) materializes its collection eagerly and does not have unloaded spatial entries; if a requested snapshot failed to commit, preparation propagates its real failure instead of declaring the new state ready. Queries do not load data, move a node, or replay old input. `Intersects` tests only its two specified live colliders and does not require surrounding terrain. Empty collision filters need no coverage.

Use `PrepareRegionAsync` before querying distant terrain or teleporting. Its rectangle uses the same scene coordinates as collision queries, not root DIPs or a map's local coordinates. Include the complete overlap/ray/swept movement envelope. Preparation includes the existing narrow-phase contact epsilon; callers do not add arbitrary padding for that tolerance.

```csharp
// Run from the attached root's UI context; keep its normal frame loop running.
using SceneCollisionRegion2D region = await scene.CollisionWorld.PrepareRegionAsync(
    new DrawRect(2000, 0, 100, 100), cancellationToken);
CollisionHit2D[] hits = scene.CollisionWorld.Raycast(new Vector2(2000, 50), Vector2.UnitX, 100);
```

The returned region retains collision interest until disposed. Overlapping regions and camera/marked-collider interests share map chunk acquisitions. Releasing the last interest permits package-backed payload release; a `FromModel` map still retains its complete model backing. The region does not keep all previously visited terrain resident. `IsReady` is live: map replacement, attachment changes or failed scene-items publication can invalidate readiness after preparation. The query checks again, so an old region is not permission to use stale geometry.

Replacing or removing a map node removes its old terrain from the scene rather than leaving it queryable while replacement data loads. Likewise, collection removals retire their realized collider nodes through the normal scene tree. A previously occupied area can be ready and empty after explicit removal; that differs from declaring an unloaded chunk absent.

Queries do not drain the relay, instantiate templates or perform hidden I/O. Keep the root's normal owner-thread loop running while asynchronous package chunks are prepared.

An already-realized [Collider2D](Cerneala.UI.Controls.Collider2D.md) with `IsSimulated = true` retains terrain near its own active geometry, even off camera. Each marked collider contributes only its own bounds; the marker is not inherited, is not a physics simulation, and does not promise that it can form a contact pair. `Enabled = false`, zero collision layer or effective invisibility suppresses that interest; trigger status and zero collision mask do not. `UIElement.IsEnabled = false` alone does not suppress it. An unloaded custom actor has no collider geometry to contribute, so keep an explicit region for its area until it is realized. This is not prediction of future movement: prepare any query extending beyond current interest explicitly.

Call preparation on the simulation owner's thread and await it without synchronously blocking that thread. An attached surface supplies the context and uses the root relay/frame loop. Without UI, create [SceneSimulationContext2D](Cerneala.UI.Controls.SceneSimulationContext2D.md) for an unowned root scene and keep its ordinary `Update` loop running. Required package-chunk failures propagate; a failed unrelated chunk outside the region does not block it. Cancellation, context disposal, detach or superseded preparation can cancel unfinished work. There is no automatic replay or unbounded retry. Losing the owner invalidates outstanding regions permanently, including after reattachment.

Prepared package chunk headers declare conservative collision envelopes before their data is loaded; package/map validation checks acquired payloads against them. Collision interest retains chunk data and adapters, not atlas images. `TileMap2D.FromModel` instead wraps a complete in-memory model whose backing remains resident. Neither route exposes a public spatial-source or arbitrary-format progressive-loader contract.

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
