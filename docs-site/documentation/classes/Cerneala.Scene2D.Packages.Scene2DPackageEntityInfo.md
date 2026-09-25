# Scene2DPackageEntityInfo Class

## Definition

Namespace: `Cerneala.Scene2D.Packages`

Assembly/Project: `Cerneala.Scene2D.Packages`

Source: `Cerneala.Scene2D.Packages/Scene2DPackageEntityInfo.cs`

Exposes the resident identity, role and authoring geometry of a separately stored package entity.

```csharp
public sealed class Scene2DPackageEntityInfo
```

Instances come from [Scene2DPackageLevel.Entities](Cerneala.Scene2D.Packages.Scene2DPackageLevel.md).
There is no public constructor.

## Remarks

The header retains no `Scene2DEntity`, opaque properties, vertex list, collider
prototype or template. Reading it performs no payload I/O. The writer derives
its two bounds from `Scene2DEntity.GetAuthoringBounds()` and
`GetCollisionBounds()`. Entity rotation and translation are included; source
pivot metadata, map offsets and level offsets are not.

An authored Point has zero area even if the game draws a large sprite at that
position. `CollisionBounds` describes only the authored descriptor, not a collider
later added by a template. The package does not interpret `Spawn` as an active
NPC, approximate unknown template bounds, or inspect entity properties to make
that decision. The application selects which IDs to load and which values to
place in a scene-items collection; it marks an already-realized actor collider
explicitly when that geometry should retain nearby terrain.

The complete entity remains available through `Scene2DPackageLevel.LoadEntityAsync(Id)`.
Loading checks identity, map, role and both authored
bounds against the header before returning the entity.

## Properties

| Name | Description |
| --- | --- |
| `Id` | Stable entity identity within the level. |
| `MapId` | Identity of its owning map in that level. |
| `Role` | Authored, case-sensitive `Metadata`, `Spawn`, `Collider` or `Promote`; not a simulation policy. |
| `AuthoringBounds` | Finite map-local axis-aligned authored shape bounds. Points may have zero area. |
| `CollisionBounds` | Optional finite map-local descriptor bounds, independent of the authoring bounds. |

## See also

- [Scene2DEntity](Cerneala.UI.Controls.Scene2DEntity.md)
- [Scene2DPackageLevel](Cerneala.Scene2D.Packages.Scene2DPackageLevel.md)
- [SceneItems2D](Cerneala.UI.Controls.SceneItems2D.md)
