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

For overlap and movement, the normal points from the returned collider toward the queried or moving collider, for every shape pair: box and polygon SAT, analytic circles, and the GJK/EPA path of segments, cones, and non-uniformly scaled or skewed circles. So the two `Overlap` queries of one pair return opposite normals. A raycast normal is the hit surface normal opposing the ray. Edge contact is included with an internal comparison epsilon of `1e-5` scene units; a ray uses the same `1e-5` scene units at any `maxDistance`.

A static overlap normal is the minimum-translation direction: the direction in which the queried collider moves the shortest distance to separate from the returned one. For two boxes or polygons the candidates are the edge normals of both shapes; for example, a 10x0.1 box that reaches 2 units into the left side of a 4x6 box gets `(-1, 0)` (2 units to separate), not a vertical normal (more than 3 units). GJK/EPA returns the same minimum-translation direction for its shapes. When both ways along an axis are equally short, the side of the queried collider's center decides; for two equal shapes with the same center the direction is unspecified.

A movement impact reached from separation uses its approach direction to resolve equally shallow polygon contact axes. An introduced floor/chunk seam therefore does not select a tangent side face merely because static overlap tie-breaking would prefer that axis. Analytic curved-surface normals are retained; the normal is not simply the opposite of the requested displacement. Initial penetration, movement into an initial contact, and zero movement use the static contact result at zero distance and fraction. For nonzero movement, an initial contact without penetration does not produce a blocking hit when separation or nonpenetrating tangential movement preserves a supporting contact plane (within the contact tolerance). Static overlap queries and initial trigger hits still report touching regardless of movement direction.

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
