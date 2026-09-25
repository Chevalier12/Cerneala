# Scene2DPackageGridChunkMetadata Class

## Definition

Namespace: `Cerneala.Scene2D.Packages`

Assembly/Project: `Cerneala.Scene2D.Packages`

Source: `Cerneala.Scene2D.Packages/Scene2DPackageGridChunkMetadata.cs`

Preserves an original authored grid chunk's extent, revision and properties without retaining its cell data.

```csharp
public sealed class Scene2DPackageGridChunkMetadata
```

There is no public constructor. Obtain instances from a loaded map metadata value.

## Examples

```csharp
Scene2DPackageMetadata metadata = await level.LoadMapMetadataAsync(mapId);
foreach (Scene2DPackageGridChunkMetadata original in metadata.GridChunks)
{
    TileMapBounds2D authoredExtent = original.Cells;
    long authoredRevision = original.Version;
}
```

## Remarks

The package writer subdivides oversized grid chunks for regional runtime reads.
This type records the boundaries before that transformation, including smaller
chunks that were not subdivided. Coordinates are integer map-grid coordinates,
not pixels or scene/world coordinates. Each generated piece inherits its original
chunk's revision and properties; this metadata does not duplicate its cells.

Instances live in explicitly loaded map metadata, not in the resident package
index. Getters do not perform I/O. Collection membership and the property
dictionary are read-only snapshots, but opaque values such as byte arrays are
not universally immutable. Release references to the metadata value and any
separately retained objects to permit collection. Shared object identity is preserved
within one payload, not across independent chunk/metadata reads.

Earlier CPV2 packages did not store this collection and cannot supply missing
authored boundaries retroactively. Regenerate them to obtain it.

## Properties

| Name | Description |
| --- | --- |
| `Cells` | Original positive rectangular grid extent, including empty cells. |
| `Version` | Original positive chunk revision. |
| `Properties` | Read-only original chunk property dictionary, with supported values preserved. |

## See also

- [Scene2DPackageMetadata](Cerneala.Scene2D.Packages.Scene2DPackageMetadata.md)
- [Scene2DPackageWriter](Cerneala.Scene2D.Packages.Scene2DPackageWriter.md)
- [TileChunk2D](Cerneala.UI.Controls.TileChunk2D.md)
