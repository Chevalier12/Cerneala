# Scene2DPackageWriter Class

## Definition

Namespace: `Cerneala.Scene2D.Packages`

Assembly/Project: `Cerneala.Scene2D.Packages`

Source: `Cerneala.Scene2D.Packages/Scene2DPackageWriter.cs`

Prepares a self-contained runtime directory from a validated in-memory scene document.

```csharp
public static class Scene2DPackageWriter
```

## Examples

```csharp
Scene2DImportResult imported = TiledScene2DImporter.Import(inputMap, importOptions);
if (!imported.Success)
    throw new InvalidOperationException("Map import failed.");

await Scene2DPackageWriter.WriteAsync(
    newPackageDirectory,
    imported.Document!,
    imported.AssetRootDirectory!,
    imported.ReferencedFiles);
```

This build/import example references `Cerneala.Scene2D.Importers`; the package
library itself has no importer dependency. C# callers can pass a constructed
`Scene2DDocument` and their explicit file dependency list directly.

## Remarks

The writer runs at preparation time, not in a camera update. Its input document
is already fully materialized. It reuses core validation and `FromModel` spatial
metadata derivation, writes independent chunk payloads with complete used
palettes, and stores nonspatial document/level/map metadata, entities and
promotions separately. Unused source definitions remain accessible through
explicit metadata loads; they are not deleted or kept in the resident package index.

### Prepared grid boundaries

Grid chunks wider or taller than 16 cells are subdivided into pieces of at most
16 by 16 cells. Each authored chunk is partitioned from its own origin, in row-major
piece order, with smaller pieces at its right/bottom edges. Chunks already within
both limits are preserved; they are not merged or aligned to a different grid.
Empty pieces are retained, so subdivision does not remove authored extent.
Free-placement ranges keep their existing preparation behavior.

This transformation affects only the prepared package. It does not mutate the
input document, change importer output, or repartition `TileMap2D.FromModel`.
Cell coordinates, tile IDs, flip bits, chunk revisions/properties and map-level
transforms remain unchanged. Each piece carries only the palette it uses and has
its own decoded-data charge. A 256-cell bound is not a byte limit: large properties
or prototypes can still exclude a piece from optional preloading.

Runtime identities use the prepared rectangle (`grid:x:y:width:height`), so a
subdivided chunk no longer has the identity/boundary of its authored parent.
Static collider coalescing stops at those new boundaries; adapter identities,
counts and query hit multiplicity can change, not the occupied geometry. Do not
treat runtime chunk IDs as editor object IDs. Map metadata's
[`GridChunks`](Cerneala.Scene2D.Packages.Scene2DPackageMetadata.md) retains every
original chunk's extent, revision and properties, without cell arrays. It is an
explicitly loaded payload, not extra resident index data. Existing validation
budgets still apply; preparation does not raise them to accommodate subdivision.

Entity catalog headers contain identity, owning map, authored role and finite
map-local authoring/collision bounds. Complete geometry, prototypes and properties
remain in individual payloads. Templates, motion envelopes and simulation policy
are supplied explicitly by the game when it loads entity values and realizes
scene nodes, not guessed by the writer. An entity whose transformed geometry cannot provide finite
spatial bounds is rejected rather than assigned empty bounds.

The output consists of `catalog.c2d`, `payloads.c2d` and an `assets` directory.
Distribute the directory as a unit. Atlas files and explicitly supplied referenced
files are copied byte-for-byte under `assets`, retaining their root-relative paths.
Duplicate exact paths are copied once. The writer does not guess dependencies
from arbitrary strings, parse referenced file contents, copy editor JSON merely
because it was the import input, decode images, or add another resource cache.

The destination must not exist, and its parent must exist. Preparation uses a
unique sibling staging directory and publishes it by rename only after successful
writes and copies. Failures/cancellation remove only files and directories created
by that invocation; existing output is never overwritten. Cleanup failures are
reported together with the original failure. This API is not an in-place package
update or hot-reload transaction.

Paths must be portable and root-relative. Traversal, absolute paths, reserved
device names, alternate-stream syntax, case-only collisions, and traversed links/
reparse points are rejected. Inputs and destination parents must remain stable
while preparation runs; these checks are not an operating-system sandbox against
concurrent hostile filesystem mutation.

### Explicit payload types

The wire format supports null, Boolean, `int`, `long`, `uint`, `ulong`, `float`,
`double`, strings, `Color`, `DrawPoint`, `SceneJsonValue2D`, string-keyed read-only
property dictionaries, byte/int arrays, read-only integer/object lists and the
scene model values used by the document. Supported opaque collection shapes may
decode to arrays/read-only collection wrappers, not the original concrete CLR
collection class. Live direct-image objects and arbitrary CLR values are rejected.
Raw `JsonElement` metadata must migrate to `SceneJsonValue2D`.

Serialization uses explicit tags, not reflection or arbitrary-object JSON
serialization. Shared reference identities within a payload are preserved;
distinct equal-content objects are not merged. Cycles and nesting beyond 128
levels are unsupported. The format preserves floating-point bits, UTF-16 string
content, tile flips and revisions. Checksums detect catalog/payload corruption,
not publisher authenticity.

### Decoded chunk data charges

Prepared grid and free-placement headers declare an internal decoded-data charge
before a runtime read. The typed encoder calculates this conservative admission
charge from the decoder's data ownership, not from compressed or encoded file
length. It includes cell/placement storage, used palettes and prototypes,
property collections, parsed vertices, detached JSON content and structural
storage, and retained copies created by model constructors. Shared wire objects
are charged once within a payload; independent payload acquisitions are separate
decoded graphs. Strings and primitive wire values do not acquire shared identity.

This is a source declaration for the optional warm working set, not a measured
managed-heap, process-memory or GPU limit. Images, resident catalogs, transient
read/decoding buffers and consumer-side bookkeeping have separate owners. Required
camera and collision reads are not rejected because their charge exceeds the
optional warm budget. This declaration neither loads images nor makes direct
metadata reads part of automatic chunk preloading.

The current wire revision is **CPV2**. Regenerate earlier CPV1 prototype packages
to include the entity selection headers. Changing only the game's template or
declared motion envelope does not require regeneration; changing stored authoring
records does. This is separate from the core document schema version.

Earlier CPV2 packages with unknown (`null`) charges remain readable by the current
reader, but their optional chunk-data preloading remains disabled. Regenerate them
to obtain declared charges. Newly prepared packages require a reader that accepts
the existing charge field; older prototype readers that rejected non-null charges
cannot open them.

The CPV2 tagged payload vocabulary now also includes authored-grid metadata.
Existing CPV2 metadata without that extension remains readable and exposes an
empty `GridChunks` collection; the reader does not invent missing provenance.
Update older readers before acquiring newly written grid-map metadata, and
regenerate older packages to obtain subdivision and authored-grid metadata.
Unknown tags fail explicitly rather than being interpreted as a different type.

## Methods

| Name | Description |
| --- | --- |
| `WriteAsync(directory, document, assetRootDirectory, referencedFiles = null, cancellationToken = default)` | Validates and prepares a new package directory without modifying its input document or existing output. |

Unsupported metadata throws `NotSupportedException`; invalid contracts/paths
throw argument exceptions; filesystem failures and cancellation are propagated.

## See also

- [Scene2DPackage](Cerneala.Scene2D.Packages.Scene2DPackage.md)
- [Scene2DDocument](Cerneala.UI.Controls.Scene2DDocument.md)
- [Scene2DImportResult](Cerneala.Scene2D.Importers.Scene2DImportResult.md)
