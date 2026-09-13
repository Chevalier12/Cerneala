# Scene2DLevel Class

## Definition

Namespace: `Cerneala.UI.Controls`
Assembly/Project: `Cerneala`
Source: `UI/Controls/Scene2DDocument.cs`

Stores independent tile maps, source catalog/extent metadata, world placement, and entity metadata as immutable data.

```csharp
public sealed class Scene2DLevel
```

## Remarks

`TileMaps` is an ordered sequence of independently renderable models, not one model containing layers. Construction copies collections and validates unique map/entity IDs, entity-to-map associations, promotion addresses, aggregate budgets, and placed geometry. Source order is preserved. The level ID must be nonempty; a document checks uniqueness across levels.

The optional level `TileSets`, `TileSize`, and `Bounds` preserve source catalog/grid/extent metadata, including for a source with no layers. An empty source therefore needs no synthetic map or scene node. A supplied grid must be positive; source bounds require that grid and positive dimensions. These metadata properties do not replace the palettes or bounds of individual maps. Document validation checks source catalog resource references and atlas rectangles even when `TileMaps` is empty.

`WorldOffset` is data for composition, not an automatic transform. Each map retains its own offset. Composition creates map/sprite nodes and applies the level placement; constructing or importing a level creates none.

Maps and source tilesets are each capped at 4,096. Aggregate cells and definitions are bounded at 1,048,576, chunks and expanded tile colliders at 65,536. Shared tileset objects are counted once within the level's definition budget; repeated chunk/cell references still count per map. Entities and promotion references are each capped at 65,536. Every entity owns at most one collider descriptor, with an aggregate entity-collider cap of 65,536.

## Constructors

| Name | Description |
| --- | --- |
| `Scene2DLevel(id, tileMaps, worldOffset = default, entities = null, promotions = null, properties = null, tileSets = null, tileSize = null, bounds = null)` | Copies and validates a level snapshot. |

## Properties

| Name | Description |
| --- | --- |
| `Id` | Stable level identity. |
| `TileMaps` | Read-only ordered models with unique IDs; may be empty. |
| `TileSets` | Read-only source catalog, independent of whether any maps exist. |
| `TileSize` | Optional source grid size. |
| `Bounds` | Optional source extent in grid coordinates. |
| `WorldOffset` | Level placement in world scene units. |
| `Entities` | Read-only entity sequence. |
| `Promotions` | Validated sparse metadata for application composition, not live nodes. |
| `Properties` | Copied opaque source metadata. |

## See also

- [TileMap2DModel](Cerneala.UI.Controls.TileMap2DModel.md)
- [Scene2DEntity](Cerneala.UI.Controls.Scene2DEntity.md)
- [TilePromotion2D](Cerneala.UI.Controls.TilePromotion2D.md)
