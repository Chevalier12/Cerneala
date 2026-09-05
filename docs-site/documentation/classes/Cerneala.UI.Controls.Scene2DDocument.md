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
    [new Scene2DLevel("Level", model)],
    [new Scene2DAsset(new ResourceId<ImageResource>("Atlas"),
        "atlas.png", new DrawSize(32, 16))]);
```

## Remarks

The constructor copies collections, checks the core schema version, and runs the shared document validator. Invalid identities, references, atlas rectangles, and budgets throw an annotated argument exception before the document can be returned. The core schema version is independent of Tiled/LDtk format versions.

No file is parsed or opened, no image is decoded, and no UI node, resource registration, promotion, or collision world is created. Composition owns publication. Metadata dictionaries are shallow snapshots, like existing tile model dictionaries; opaque values are not deep-cloned.

### Runtime import and declarative composition

The optional [Tiled](Cerneala.Scene2D.Importers.TiledScene2DImporter.md) and
[LDtk](Cerneala.Scene2D.Importers.LdtkScene2DImporter.md) importers return this
same core document only after validation succeeds. They are synchronous map-load
operations, not source-generator parsers. Their canonical pages specify the
closed format/version matrices, supported fields and explicit non-goals.

The [Scene World Playground composition](../../../Playground/Cerneala.Playground/SceneWorldShowcase.crn)
binds the imported tile model to `TileMap2D`, declares its atlas resource,
promoted door, colliders, animation sets and dynamic templates in markup.
Its [code-behind](../../../Playground/Cerneala.Playground/SceneWorldShowcase.crn.cs)
loads/validates the document, maps the sample's six box entities to template
data, and handles gameplay through routed input and `MoveAndCollide`. It does
not rebuild the declared scene in C# or create one UI node per static tile.

The separate original Tiled and LDtk fixtures represent 4,096 equal tile cells
and equivalent gameplay entities. Tiled retains 64 sparse chunks; LDtk retains
two finite tile-layer chunks. Chunk representation and source provenance are
not asserted identical. See [sparse promotion and the interactive door](Cerneala.UI.Controls.TilePromotion2D.md).

## Constructors

| Name | Description |
| --- | --- |
| `Scene2DDocument(levels, assets, schemaVersion = 1, properties = null, validationOptions = null)` | Constructs and validates one complete core document. |

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `CurrentSchemaVersion` | 1 | Exact accepted core document schema. |

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
