# TilePromotion2D Class

## Definition

Namespace: `Cerneala.UI.Controls`
Assembly/Project: `Cerneala`
Source: `UI/Controls/Scene2DDocument.cs`

Preserves a sparse imported cell address and application metadata for individual scene composition.

```csharp
public sealed class TilePromotion2D
```

## Examples

```csharp
var candidate = new TilePromotion2D(
    new TileCellKey2D("Doors", 2, 3),
    properties: new Dictionary<string, object?> { ["InitialState"] = "Closed" });
```

## Remarks

Construction requires a nonempty map identity and an optional positive override ID. `Scene2DLevel` validates that the map and cell exist, that the resolved tile ID is defined, and that no address is duplicated. An existing empty cell needs an explicit override.

This class is metadata, not a UI node or an automatic extraction operation. Tiled/LDtk keep the external `CernealaRole="Promote"`, `TileLayer`, `TileX`, and `TileY` conventions; the source layer identity becomes `Cell.MapId`.

Application composition decides whether to represent a candidate as an ordinary [Sprite2D](Cerneala.UI.Controls.Sprite2D.md). It must explicitly remove any replaced static cell from the published source, select the image/source rectangle, convert the grid coordinate to pixels, and preserve any required source offsets, tint, opacity, and scene order. Adding a sprite does not automatically suppress a static cell or inherit its collider. Restore static content by publishing the original source content and removing the sprite. Changed grid content must follow the chunk version contract.

The Scene World sample opens a build-prepared package and explicitly acquires its
promotion record. Its application-owned door source reconstructs the requested
chunk with cell `("4", 14, 9)` cleared; it does not retain the imported document
or rewrite the package. A peer door sprite at pixel position `(224, 144)` owns
the live collider, animation state, routed input, Motion trigger, and Prism.
`InitialState` initializes application state; neither importing nor opening a
package executes it. Other dynamic entities use [SceneItems2D](Cerneala.UI.Controls.SceneItems2D.md)
templates. The sample's edited payloads declare an unknown data-residency charge,
so they are loaded for required interests rather than admitted into optional
warm residency as supposedly zero-cost data.

## Constructors

| Name | Description |
| --- | --- |
| `TilePromotion2D(TileCellKey2D cell, int? tileId = null, IReadOnlyDictionary<string, object?>? properties = null)` | Copies metadata and validates address identity/override; the owning level resolves the cell. |

## Properties

| Name | Description |
| --- | --- |
| `Cell` | Stable map/coordinate address. |
| `TileId` | Optional positive replacement tile ID. |
| `Properties` | Shallow copied application/source metadata. |

## See also

- [Sprite2D](Cerneala.UI.Controls.Sprite2D.md)
- [TileMap2D](Cerneala.UI.Controls.TileMap2D.md)
- [TileCellKey2D](Cerneala.UI.Controls.TileCellKey2D.md)
- [Scene2DDocument](Cerneala.UI.Controls.Scene2DDocument.md)
