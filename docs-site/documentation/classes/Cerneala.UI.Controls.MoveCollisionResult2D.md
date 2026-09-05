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
