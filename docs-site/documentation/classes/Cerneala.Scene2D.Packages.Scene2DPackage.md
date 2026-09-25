# Scene2DPackage Class

## Definition

Namespace: `Cerneala.Scene2D.Packages`

Assembly/Project: `Cerneala.Scene2D.Packages`

Source: `Cerneala.Scene2D.Packages/Scene2DPackage.cs`

Opens a prepared CPV2 scene package and owns independent asynchronous reads of its indexed payloads.

```csharp
public sealed class Scene2DPackage : IDisposable, IAsyncDisposable
```

## Examples

```csharp
await using Scene2DPackage package = await Scene2DPackage.OpenAsync(packageDirectory);
Scene2DPackageLevel level = package.Levels[0];
Scene2DPackageMetadata metadata = await package.LoadMetadataAsync();
bool hasAuthor = metadata.Properties.ContainsKey("Author");
```

Create a package-backed map with `level.CreateTileMap(mapId)`. Keep the package alive while that map can request chunks; detach and await the map's `DisposeAsync()` before awaiting package disposal when a complete shutdown is required.

The optional module references only Cerneala core. A runtime consumer does not
need the Tiled/LDtk importer assembly or its compression dependency.

## Remarks

`OpenAsync` reads and verifies the catalog, validates byte ranges and header
identities, and checks the payload part's length without reading its contents.
It reads small entity geometry/role
headers, not the full entities. It does not load tile cells, definitions, entity
properties/vertices/prototypes, other opaque properties or image pixels. Asset existence and
path checks occur when the local-directory `GetFilePath` is called; image loading
still belongs to the common image resource/cache path. The range-reader overload
serves a prepared CPV2 package through application-provided byte ranges; it does
not decode an arbitrary format or provide remote asset streams.

Each requested payload is read by its own offset and length, checked against
its SHA-256 checksum and decoded with explicit wire types. The package does not
share a mutable file position between reads; a custom reader must support
independent, possibly concurrent ranges. Corruption in an unrequested payload does not prevent
another valid payload from loading. Checksums detect corruption; they are not
signatures or proof of a trusted publisher. A local directory must not change
while in use; a custom range reader must serve one immutable package revision.

Payloads are not cached by the package. A package-backed map coordinates its own
chunk residency; direct metadata, entity and promotion reads return values owned
by the caller. Those values have no lease guard after package disposal. JSON
identity is preserved within each payload, not across independent reads.

`Dispose()` rejects new reads and `GetFilePath` calls and starts reader shutdown;
already admitted reads may complete. It does not wait for or report late cleanup
errors. `DisposeAsync()` waits for those reads and reader disposal and reports
cleanup failure; repeated calls observe the same terminal completion. A non-null
custom reader's ownership transfers at entry to `OpenAsync`, even if options are
invalid or cancellation is already requested. Failed open awaits reader disposal
and preserves both the opening and cleanup failures when both occur. Headers and
previously returned values remain readable after disposal. Cancellation is per
request; it does not poison other reads or add automatic retries. No UI root or
render surface is required for opening or reading.

`GetFilePath(relativePath)` is only for a package opened from a local directory.
It returns an absolute path for a declared existing file, including non-image
referenced files. On a range-reader package a declared path throws
`NotSupportedException`; undeclared paths still throw `ArgumentException`.
Remote images must be supplied separately through the application's resource
provider and image loader, not through this package reader.

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
new metadata tags extend CPV2 and require an updated decoder for those reads;
older CPV2 metadata remains readable with an empty `GridChunks` collection. See
[prepared grid boundaries](Cerneala.Scene2D.Packages.Scene2DPackageWriter.md#prepared-grid-boundaries).

## Properties

| Name | Description |
| --- | --- |
| `Assets` | Read-only atlas identities, root-relative asset paths and declared dimensions. No decoded images. |
| `ReferencedFiles` | Read-only normalized file paths relative to the package's `assets` directory, including atlas files. |
| `Levels` | Read-only level headers and map/value-loading entry points. |

## Methods

| Name | Description |
| --- | --- |
| `OpenAsync(directory, options = null, cancellationToken = default)` | Opens a local prepared directory with encoded-size admission limits. |
| `OpenAsync(reader, options = null, cancellationToken = default)` | Takes ownership of an `IScene2DPackageRangeReader` for a prepared CPV2 package. |
| `LoadMetadataAsync(cancellationToken = default)` | Returns document metadata directly; document metadata has no tilesets. |
| `GetFilePath(relativePath)` | Returns an absolute path to a declared existing local file after containment, kind and reparse-point checks; unavailable for a range-reader package. |
| `Dispose()` | Rejects new operations and starts reader shutdown without waiting for its completion. |
| `DisposeAsync()` | Awaits admitted reads and reader disposal, including cleanup errors; idempotent. |

Invalid catalog data, inconsistent ranges, checksum failures, unexpected payload
types and exceeded read limits throw `InvalidDataException`. File access failures
are propagated. Undeclared file names throw `ArgumentException`; canceled reads
throw cancellation exceptions. Reads after disposal throw `ObjectDisposedException`.

## See also

- [Scene2DPackageWriter](Cerneala.Scene2D.Packages.Scene2DPackageWriter.md)
- [Scene2DPackageLevel](Cerneala.Scene2D.Packages.Scene2DPackageLevel.md)
- [Scene2DPackagePart](Cerneala.Scene2D.Packages.Scene2DPackagePart.md)
- [IScene2DPackageRangeReader](Cerneala.Scene2D.Packages.IScene2DPackageRangeReader.md)
- [Scene2DPackageReadOptions](Cerneala.Scene2D.Packages.Scene2DPackageReadOptions.md)
- [SceneJsonValue2D](Cerneala.UI.Controls.SceneJsonValue2D.md)
- [ImageResourceCache](Cerneala.UI.Resources.ImageResourceCache.md)
