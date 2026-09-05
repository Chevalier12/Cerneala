# Scene2DLevel Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Scene2DDocument.cs`

Stores a tile map, world placement, entities, and sparse promotion references as immutable data.

```csharp
public sealed class Scene2DLevel
```

## Remarks

Construction copies collections and validates entity/layer associations, unique entity IDs, promotion addresses, and placed geometry. Entity and promotion order is preserved. The level ID must be nonempty; the document checks uniqueness across levels.

Entities and promotions are each capped at 65,536 entries. The sum of entity collider descriptors is also capped at 65,536, including descriptors shared by multiple entities.

WorldOffset is data for scene composition, not an automatic transform. Tile layers retain their own offsets. Importing or constructing a level never promotes cells or materializes entity nodes.

## Constructors

| Name | Description |
| --- | --- |
| `Scene2DLevel(id, tileMap, worldOffset = default, entities = null, promotions = null, properties = null)` | Creates and validates a level snapshot. |

## Properties

| Name | Description |
| --- | --- |
| `Id` | Stable level identity. |
| `TileMap` | Validated immutable tile model. |
| `WorldOffset` | Level placement in world scene units. |
| `Entities` | Read-only entity sequence. |
| `Promotions` | Read-only sparse promotion metadata, not live nodes. |
| `Properties` | Copied opaque source metadata. |

## See also

- [Scene2DEntity](Cerneala.UI.Controls.Scene2DEntity.md)
- [TilePromotion2D](Cerneala.UI.Controls.TilePromotion2D.md)
