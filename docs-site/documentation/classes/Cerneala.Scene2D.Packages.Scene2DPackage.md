# Scene2DPackage Class

## Definition

Namespace: `Cerneala.Scene2D.Packages`

Assembly/Project: `Cerneala.Scene2D.Packages`

Source: `Cerneala.Scene2D.Packages/Scene2DPackage.cs`

Opens a prepared scene directory and owns independent asynchronous reads of its indexed payloads.

```csharp
public sealed class Scene2DPackage : IDisposable
```

## Examples

```csharp
Scene2DPackage package = await Scene2DPackage.OpenAsync(packageDirectory);
Scene2DPackageLevel level = package.Levels[0];
TileMap2D map = new() { Source = level.TileMaps[0] };

using (var metadata = await package.LoadMetadataAsync())
{
    bool hasAuthor = metadata.Value.Properties.ContainsKey("Author");
}
// Keep package alive while the scene can request more chunks.
// Dispose it when that scene's package-backed sources are no longer needed.
```

The optional module references only Cerneala core. A runtime consumer does not
need the Tiled/LDtk importer assembly or its compression dependency.

## Remarks

`OpenAsync` reads and verifies the catalog, validates byte ranges and header
identities, and opens the payload file for shared reading. It checks the data
file's length without reading its contents. It reads small entity geometry/role
headers, not the full entities. It does not load tile cells, definitions, entity
properties/vertices/prototypes, other opaque properties or image pixels. Asset existence and
path checks occur when `GetFilePath` is called; image loading still belongs to
the common image resource/cache path.

Each requested payload is read by its own offset and length, checked against
its SHA-256 checksum and decoded with explicit wire types. Reads do not share a
mutable file position. Corruption in an unrequested payload does not prevent
another valid payload from loading. Checksums detect corruption; they are not
signatures or proof of a trusted publisher. The directory must not be modified
while in use.

Payloads are not cached by the package. Scene residency coordinates chunk
acquisitions; explicit metadata acquisitions are independent. Disposing a
[lease](Cerneala.UI.Controls.SceneSpatialLease2D_T_.md) removes its reference to
the acquired object. References retained separately by application code keep
that content alive. JSON identity is preserved within each payload, not across
independent reads.

Disposing the package rejects new acquisitions and `GetFilePath` calls. Already
started reads may complete, and acquired leases remain usable. The data handle
closes after any active reads finish. Headers remain readable after disposal.
Cancellation is per request; it does not poison other reads or add automatic
retries. No UI root or render surface is required for opening or reading.

The current wire revision is **CPV2**, which includes resident entity selection
headers. Earlier CPV1 prototype packages must be regenerated with the writer or
preparation CLI; they are rejected, not silently interpreted as CPV2. This wire
revision is separate from `Scene2DDocument.CurrentSchemaVersion`.

Current prepared chunk headers carry the writer's conservative decoded-data
charge in the existing CPV2 field. It is available without loading the payload
and lets `TileMap2D` consider that data for its bounded optional warm working set.
It is not the block's encoded length or a measured physical-memory ceiling.
Required camera and collision acquisitions remain independent of the optional
budget. See [the writer's accounting contract](Cerneala.Scene2D.Packages.Scene2DPackageWriter.md#decoded-chunk-data-charges).

Earlier CPV2 headers with a null charge remain readable and retain **unknown**
cost semantics, not zero cost. Regenerate those packages to enable optional
chunk-data preloading. Older prototype readers that rejected known charges must
be updated before opening newly prepared packages.

The writer also subdivides oversized grid chunks into pieces of at most 16 by 16
cells. Existing packages are not repartitioned by `OpenAsync`; regenerate them to
obtain these boundaries. Original chunk extents, revisions and properties are
available through map metadata's `GridChunks`, not retained by the catalog. The
new metadata tags extend CPV2 and require an updated reader for those acquisitions;
older CPV2 metadata remains readable with an empty `GridChunks` collection. See
[prepared grid boundaries](Cerneala.Scene2D.Packages.Scene2DPackageWriter.md#prepared-grid-boundaries).

## Properties

| Name | Description |
| --- | --- |
| `Assets` | Read-only atlas identities, root-relative asset paths and declared dimensions. No decoded images. |
| `ReferencedFiles` | Read-only normalized file paths relative to the package's `assets` directory, including atlas files. |
| `Levels` | Read-only level headers and acquisition entry points. |

## Methods

| Name | Description |
| --- | --- |
| `OpenAsync(directory, options = null, cancellationToken = default)` | Opens a prepared directory with explicit encoded-size admission limits. |
| `LoadMetadataAsync(cancellationToken = default)` | Acquires document properties; document metadata has no tilesets. |
| `GetFilePath(relativePath)` | Returns an absolute path to a declared existing file. Checks normalized containment, filesystem kind and reparse points; does not read or decode file contents. |
| `Dispose()` | Ends the package's ability to issue new reads; idempotent. |

Invalid catalog data, inconsistent ranges, checksum failures, unexpected payload
types and exceeded read limits throw `InvalidDataException`. File access failures
are propagated. Undeclared file names throw `ArgumentException`; canceled reads
throw cancellation exceptions. Reads after disposal throw `ObjectDisposedException`.

## See also

- [Scene2DPackageWriter](Cerneala.Scene2D.Packages.Scene2DPackageWriter.md)
- [Scene2DPackageLevel](Cerneala.Scene2D.Packages.Scene2DPackageLevel.md)
- [Scene2DPackageReadOptions](Cerneala.Scene2D.Packages.Scene2DPackageReadOptions.md)
- [SceneJsonValue2D](Cerneala.UI.Controls.SceneJsonValue2D.md)
- [ImageResourceCache](Cerneala.UI.Resources.ImageResourceCache.md)
