# Scene2DPackageMetadata Class

## Definition

Namespace: `Cerneala.Scene2D.Packages`

Assembly/Project: `Cerneala.Scene2D.Packages`

Source: `Cerneala.Scene2D.Packages/Scene2DPackageMetadata.cs`

Contains explicitly loaded document, level or map authoring metadata outside the resident package index.

```csharp
public sealed class Scene2DPackageMetadata
```

There is no public constructor. Obtain an instance through a package or level metadata-loading method.

## Examples

```csharp
Scene2DPackageMetadata metadata = await level.LoadMetadataAsync();
if (metadata.Properties.TryGetValue("$source", out object? value) &&
    value is SceneJsonValue2D json)
{
    string originalJson = json.Value.GetRawText();
}
```

## Remarks

Properties and collection membership are read-only snapshots. Opaque values are
not universally immutable: for example, a stored byte array remains a byte array.
The package does not retain this object after returning it. The caller retains
the value for as long as needed; package disposal does not invalidate its
getters. Release references to it and any separately retained values to allow
collection. The getters perform no I/O.

Document metadata contains document properties and no tilesets. Level metadata
contains level properties and the level's original source tilesets. Map metadata
contains map properties and the complete original map palette, including unused
definitions. Map `GridChunks` contains the original authored grid extents,
revisions and properties in authored order, without cell arrays. These boundaries
can differ from the prepared runtime pieces, which are at most 16 by 16 cells.
The original grid identity can be derived from its extent using
`grid:x:y:width:height`; no editor-specific identity is invented.
Required chunk palettes are loaded independently and contain only definitions
used by that chunk.

Document, level and free-placement metadata have no authored grid chunks.
Earlier CPV2 metadata also decodes with an empty `GridChunks` collection because
it did not store this information. Regenerate that package to preserve original
grid provenance explicitly; an empty collection alone does not distinguish an
older payload from a scope with no grid chunks. The new tagged extension requires
an updated reader when acquiring newly prepared grid-map metadata.

JSON content uses [SceneJsonValue2D](Cerneala.UI.Controls.SceneJsonValue2D.md).
Shared object identities are preserved within one decoded payload; equal content
with distinct identities is not merged. Independent reads do not promise
the same object identity.

## Properties

| Name | Description |
| --- | --- |
| `Properties` | Read-only dictionary of the acquired metadata scope. |
| `TileSets` | Read-only complete source palette for the scope, or empty for document metadata. |
| `GridChunks` | Read-only original grid chunk metadata, outside the resident catalog; empty for scopes/older payloads without it. |

## See also

- [Scene2DPackage](Cerneala.Scene2D.Packages.Scene2DPackage.md)
- [Scene2DPackageLevel](Cerneala.Scene2D.Packages.Scene2DPackageLevel.md)
- [Scene2DPackageGridChunkMetadata](Cerneala.Scene2D.Packages.Scene2DPackageGridChunkMetadata.md)
- [TileMap2DModel](Cerneala.UI.Controls.TileMap2DModel.md)
