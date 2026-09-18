# Scene2DDocument Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Scene2DDocument.cs`

Groups validated backend-neutral levels, atlas declarations, and source metadata.

```csharp
public sealed class Scene2DDocument
```

## Examples

```csharp
var document = new Scene2DDocument(
    [new Scene2DLevel("Level", [model])],
    [new Scene2DAsset(new ResourceId<ImageResource>("Atlas"),
        "atlas.png", new DrawSize(32, 16))]);
```

## Remarks

The constructor copies collections, checks the core schema version, and runs the shared document validator. Invalid identities, references, atlas rectangles, and budgets throw an annotated argument exception before the document can be returned. The core schema version is 2, independent of Tiled/LDtk format versions. Each level exposes an ordered `TileMaps` sequence instead of a single multilayer model; schema 1 is not accepted. Source catalog/grid/extent metadata stays on the level even when it has no maps.

No file is parsed or opened, no image is decoded, and no UI node, resource registration, promotion, or collision world is created. Composition owns publication. Metadata dictionaries are shallow snapshots, like existing tile model dictionaries; opaque values are not deep-cloned.

### Import, package preparation and declarative composition

Verbatim JSON metadata published by the importers uses
[SceneJsonValue2D](Cerneala.UI.Controls.SceneJsonValue2D.md). Access its `Value`
instead of casting the property directly to `JsonElement`. The wrapper preserves
detached content and reference identity without reflective equality. Other
mapped metadata types and the document's shallow-copy semantics are unchanged.

The optional [Tiled](Cerneala.Scene2D.Importers.TiledScene2DImporter.md) and
[LDtk](Cerneala.Scene2D.Importers.LdtkScene2DImporter.md) importers return this
same core document only after validation succeeds. They are synchronous map-load
operations, not source-generator parsers. Their canonical pages specify the
closed format/version matrices, supported fields and explicit non-goals.

The [Scene World Playground composition](../../../Playground/Cerneala.Playground/SceneWorldShowcase.crn)
binds package-backed sources to peer `TileMap2D` nodes and declares an ordinary
door sprite, colliders, animation sets, and dynamic templates in markup. Its build
uses these importers and the optional [package writer](Cerneala.Scene2D.Packages.Scene2DPackageWriter.md)
to prepare two autonomous directories. Runtime opening uses
[Scene2DPackage](Cerneala.Scene2D.Packages.Scene2DPackage.md), not an importer or
a resident complete document.

The [application adapter](../../../Playground/Cerneala.Playground/SceneWorldPackage.cs)
acquires the spawn and door metadata explicitly, streams the six authored box
entities for spatial interests, and reconstructs only affected chunk payloads
for its two supported cell edits. The [code-behind](../../../Playground/Cerneala.Playground/SceneWorldShowcase.crn.cs)
prepares player collision interests before movement/reset, publishes through the
UI relay, and handles gameplay through routed input and `MoveAndCollide`.
It does not create one UI node per static tile or modify the package on disk.

In this sample, `SceneWorldState.PlantAsync(CancellationToken)` prepares a decorative
flower edit before UI-thread publication. It replaces the former synchronous
`Plant()` sample method; callers await the operation. Plant toggles Buildings cell
(11,10), preserving the grass in Terrain and the authored neighboring flower. The
prepared managed payload is shared with current scene interests only during
publication, then staging is released; later acquisitions reconstruct it from the
package. Cancellation before publication or a superseded operation prevents the
pending edit from being published. This does
not change `RenderSurface2D`'s whole-scene Loading behavior for missing required
data, and is not a general mutable-map API.

The separate Tiled and LDtk fixtures preserve equivalent tile content and gameplay
entities. Preparation preserves the authored provenance in leased metadata and
subdivides oversized grids into at most 16-by-16 pieces. The application-owned
door source clears the cell drawn by the peer sprite; the original package
payload remains unchanged. See [sparse composition metadata](Cerneala.UI.Controls.TilePromotion2D.md).

## Constructors

| Name | Description |
| --- | --- |
| `Scene2DDocument(levels, assets, schemaVersion = 2, properties = null, validationOptions = null)` | Constructs and validates one complete core document. |

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `CurrentSchemaVersion` | 2 | Exact accepted core document schema. |

## Properties

| Name | Description |
| --- | --- |
| `SchemaVersion` | Core schema version. |
| `Levels` | Copied read-only level sequence; IDs must be unique. |
| `Assets` | Copied read-only atlas declarations; resource IDs must be unique. |
| `Properties` | Copied opaque provenance/metadata dictionary. |

## See also

- [Scene2DLevel](Cerneala.UI.Controls.Scene2DLevel.md)
- [Scene2DAsset](Cerneala.UI.Controls.Scene2DAsset.md)
- [Scene2DModelValidator](Cerneala.UI.Controls.Scene2DModelValidator.md)
