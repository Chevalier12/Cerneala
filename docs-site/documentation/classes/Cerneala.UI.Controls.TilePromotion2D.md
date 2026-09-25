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

Application composition decides whether to represent a candidate as an ordinary [Sprite2D](Cerneala.UI.Controls.Sprite2D.md). It must explicitly clear any replaced static cell in a replacement immutable model, select the image/source rectangle, convert the grid coordinate to pixels, and preserve any required model offsets, tint, opacity, and scene order. Adding a sprite does not automatically suppress a static cell or inherit its collider. Restore static content by replacing the map from the original model data and removing the sprite. Changed grid content is expressed in a replacement immutable chunk/model, not a live cell setter.

For a package-backed promoted door, an application can load the promotion record
directly and use `Scene2DPackageLevel.LoadMapModelAsync` to obtain a complete
editable model. It constructs a replacement immutable chunk/model with the
promoted cell cleared, then replaces the map node through `TileMap2D.FromModel`.
It does not rewrite the package. A peer door sprite can own the live collider,
animation state, routed input, Motion trigger, and Prism. `InitialState` is
application metadata; neither importing nor opening a package executes it. Other
dynamic entities can use [SceneItems2D](Cerneala.UI.Controls.SceneItems2D.md)
templates. The full-model edit replaces map identity and can discard prior warm
residency; it is not a single-chunk package publication.

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
