# CollisionHit2D Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/CollisionHit2D.cs`

Describes one immutable scene-space collision result.

```csharp
public sealed class CollisionHit2D
```

## Examples

```csharp
CollisionHit2D? first = scene.CollisionWorld
    .Raycast(origin, Vector2.UnitX, 256)
    .FirstOrDefault();

if (first is not null && !first.IsTrigger)
{
    Vector2 surfaceNormal = first.Normal;
}
```

## Remarks

`Point` and `Normal` are expressed in root scene coordinates. `Distance` is scene-space travel from the query origin and `Fraction` is its normalized value in `[0, 1]`. Initial overlap reports zero for both.

For overlap and movement, the normal points from the returned collider toward the queried or moving collider. A raycast normal is the hit surface normal opposing the ray. Edge contact is included with an internal comparison epsilon of `1e-5` scene units.

A movement impact reached from separation uses its approach direction to resolve equally shallow polygon contact axes. An introduced floor/chunk seam therefore does not select a tangent side face merely because static overlap tie-breaking would prefer that axis. Analytic curved-surface normals are retained; the normal is not simply the opposite of the requested displacement. Initial touching or overlap still uses the static contact result at zero distance and fraction, including zero or separating movement.

`Entity` is the closest non-collider `SceneNode2D` ancestor of `Collider`. For live colliders this is the owning `Sprite2D`; static tile adapters identify their containing `TileMap2D`. A static tile placement is immutable model data, not a per-tile UI entity.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Collider` | `Collider2D` | Collider producing the hit. |
| `Entity` | `SceneNode2D` | Structurally associated scene entity. |
| `Point` | `Vector2` | Contact point in scene coordinates. |
| `Normal` | `Vector2` | Unit contact normal. |
| `Distance` | `float` | Scene-space query distance. |
| `Fraction` | `float` | Normalized query fraction. |
| `IsTrigger` | `bool` | Whether the returned collider is a trigger. |

## Applies to

Project: `Cerneala`

## See also

- `CollisionWorld2D`
- `MoveCollisionResult2D`
