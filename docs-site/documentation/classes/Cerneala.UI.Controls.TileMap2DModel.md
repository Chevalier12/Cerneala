# TileMap2DModel Class

## Definition

Namespace: `Cerneala.UI.Controls`
Assembly/Project: `Cerneala`
Source: `UI/Controls/TileMap2DModel.cs`

Defines a backend-neutral immutable/versioned tile map.

```csharp
public sealed class TileMap2DModel
```

Inheritance:
`object` -> `TileMap2DModel`

## Examples

```csharp
var model = new TileMap2DModel(
    new DrawSize(16, 16),
    tileSets,
    layers,
    new TileMapBounds2D(0, 0, 128, 96),
    version: 1);
```

## Remarks

The model is backend-neutral: it contains coordinates, atlas resource IDs, source rectangles, layers, and chunks, but no GPU handles. Constructor collections and property dictionaries are copied into read-only views. Values stored inside an opaque `Properties` dictionary are not interpreted or deep-cloned by the renderer.

Positive tile IDs are global to the map and must resolve to exactly one definition; ID `0` is empty. A null `Bounds` value identifies a sparse map and permits negative or remote chunk coordinates. It does not cause the runtime to enumerate the rectangle between remote chunks. Finite maps reject chunks outside their declared bounds.

`Version` is a positive publication stamp, not an automatically incremented mutable counter. Model components are immutable after construction. Publish changed content by constructing replacement objects with changed versions and assigning the replacement map to `TileMap2D.Model`. In particular, a changed chunk must receive a new `TileChunk2D.Version`, and changed tile definitions or atlas identity must receive a new `TileSet2D.Version` or resource version. Mutating data behind an unchanged cache-visible version is unsupported.

Layer `Version` and map `Version` let importers and application state identify publications. The retained drawing cache additionally compares chunk versions, tileset versions, tile size, composed tint, promoted-cell suppression, atlas resource version, and resolved image identity.

Construction bounds tileset/layer enumeration to 4,096 entries each, validates chunk placement against the drawing coordinate range, and limits expanded tile collider descriptors to 65,536 before coalescing. Invalid data is rejected before a presentation node can materialize collision adapters. Atlas sizes are external information: validate source rectangles and resource references using [Scene2DModelValidator](Cerneala.UI.Controls.Scene2DModelValidator.md), or construct a [Scene2DDocument](Cerneala.UI.Controls.Scene2DDocument.md), which performs that validation before returning.

Aggregate definitions and cells are each capped at 1,048,576; aggregate chunks at 65,536. Reusing immutable component references does not bypass aggregate caps. At recording time, `TileMap2D` validates all resolved atlas dimensions/source rectangles before building any chunk commands. Missing runtime resources keep their existing deferred-resolution behavior; a document instead requires complete atlas declarations.

## Properties

| Name | Description |
| --- | --- |
| `TileSize` | Destination size of one tile in scene units. |
| `Bounds` | Finite tile-coordinate bounds, or null for a sparse map. |
| `TileSets` | Immutable tileset view. |
| `Layers` | Immutable layer-model view. |
| `Version` | Positive cache-visible model version. |
| `Properties` | Copied opaque importer metadata. |

## Methods

| Name | Description |
| --- | --- |
| `TryResolveTile` | Resolves a positive global tile ID to its tileset and definition. |
| `TryGetLayer` | Resolves a layer by its ordinal stable ID. |

## Applies to

Project: `Cerneala`

## See also

- [TileMap2D](Cerneala.UI.Controls.TileMap2D.md)
- [TileSet2D](Cerneala.UI.Controls.TileSet2D.md)
- [TileLayer2DModel](Cerneala.UI.Controls.TileLayer2DModel.md)
- [TileChunk2D](Cerneala.UI.Controls.TileChunk2D.md)
