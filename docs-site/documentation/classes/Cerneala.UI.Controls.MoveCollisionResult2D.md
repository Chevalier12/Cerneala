# MoveCollisionResult2D Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/MoveCollisionResult2D.cs`

Returns the immutable result of a continuous `CollisionWorld2D.MoveAndCollide` query.

```csharp
public sealed class MoveCollisionResult2D
```

## Examples

```csharp
Vector2 requested = new(12, 0);
MoveCollisionResult2D result = scene.CollisionWorld.MoveAndCollide(playerCollider, requested);
player.TranslateX += result.Travel.X;
player.TranslateY += result.Travel.Y;
```

## Remarks

The query computes a shape cast but does not mutate the collider, its entity, or gameplay state. `Travel` stops at the first blocking contact; `Remainder` is always `RequestedDisplacement - Travel`. Cerneala does not perform sliding, response, iteration, or a hidden physics simulation.

Triggers never limit `Travel` and cannot become `Collision`. Matching trigger contacts along the requested cast are returned in `TriggerHits`, ordered by fraction, distance, and stable attachment ordinal.

For an impact reached from separation, `Collision.Normal` describes the contacted surface, using the approach direction to disambiguate equal polygon contact axes at a seam. It is not a sliding vector or the negated movement direction. Initial contact without penetration permits nonzero separating or nonpenetrating tangential movement, using the existing `1e-5` scene-unit contact tolerance; such a contact is not returned as `Collision`. Other obstacles along the movement still limit `Travel`. Initial penetration, movement into a contact, and zero movement retain the static contact result at zero travel. Initial trigger contacts remain in `TriggerHits` regardless of movement direction. See [CollisionHit2D](Cerneala.UI.Controls.CollisionHit2D.md).

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `RequestedDisplacement` | `Vector2` | Full displacement supplied by the caller. |
| `Travel` | `Vector2` | Displacement up to the first blocking contact, or the full request. |
| `Remainder` | `Vector2` | Unconsumed displacement. |
| `Collision` | `CollisionHit2D?` | First blocking hit, or `null`. |
| `TriggerHits` | `IReadOnlyList<CollisionHit2D>` | Trigger contacts found along the requested cast. |

## Applies to

Project: `Cerneala`

## See also

- `CollisionWorld2D.MoveAndCollide`
- `CollisionHit2D`
