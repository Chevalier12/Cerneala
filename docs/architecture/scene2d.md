# Scene2D Packages And Importers

> Code: `Cerneala.Scene2D.Packages/`, `Cerneala.Scene2D.Importers/`, `Tools/Cerneala.Scene2D.PackageCompiler/`, the package path of `UI/Controls/TileMap2D*.cs` · Verified at commit `0cb32300` (2026-10-10)

## Responsibility

Scene2D turns an editor map (Tiled or LDtk) into a prepared package directory at build time, and reads that package at run time in small, checked byte ranges. It does not render, schedule loads or decide residency: `TileMap2D` owns those. The package only transports bytes and validates them.

## Components

| Type | File | Role | Owner |
|---|---|---|---|
| `TiledScene2DImporter`, `LdtkScene2DImporter` | `Cerneala.Scene2D.Importers/` | Parse one editor file into a validated `Scene2DDocument`, or into diagnostics | build time, one call |
| `Scene2DImportOptions` | `Scene2DImportOptions.cs` | Asset root and input budgets | caller |
| `Scene2DImportResult` | `Scene2DImportResult.cs` | `Success`, `Document`, `AssetRootDirectory`, `ReferencedFiles`, `Diagnostics`, `DiagnosticsTruncated` | caller |
| `Scene2DPackageWriter` | `Cerneala.Scene2D.Packages/Scene2DPackageWriter.cs` | Writes a new package directory from a document | build time, one call |
| `Scene2DPackage` | `Scene2DPackage.cs` | Opens a package, keeps its index in memory, serves payload reads | the application (caller) |
| `Scene2DPackageLevel` | `Scene2DPackageLevel.cs`, `Scene2DPackageLevel.Model.cs` | One level's headers; creates maps and loads entities, promotions, metadata | its `Scene2DPackage` |
| `IScene2DPackageRangeReader`, `Scene2DPackagePart` | `IScene2DPackageRangeReader.cs` | Public byte transport: `GetLengthAsync` and `ReadAsync` on `Catalog` or `Payloads` | passed to `Scene2DPackage`, which then owns it |
| `LocalPackageRangeReader` (internal) | `LocalPackageRangeReader.cs` | The file-system reader used by `OpenAsync(string directory, ...)` | its `Scene2DPackage` |
| `Scene2DPackageReadOptions`, `Scene2DPackageMetadata` | `Scene2DPackageMetadata.cs` | Read limits; loaded property and tile-set metadata | caller |
| `PackageIndex` and its records (internal) | `PackageIndex.cs` | Selection data and byte ranges only: `PackageBlock(Offset, Length, Hash)`, `PackageMap`, `PackageEntity`, `PackagePromotion`, `PackageLevel` | its `Scene2DPackage` |
| `PackageValueCodec` (internal) | `PackageValueCodec.cs` | The CPV2 binary encoder and decoder | static |
| `PackageFiles` (internal) | `PackageFiles.cs` | File names and portable path rules | static |
| Package compiler | `Tools/Cerneala.Scene2D.PackageCompiler/Program.cs` | Command line: import, then write | a developer; the `Playground` build |
| `TileMap2D`, `SceneSpatialResidency2D`, `CollisionWorld2D`, `RenderSurface2D` | `UI/Controls/` | The core controls that consume a package map | the scene tree |

Project references: `Cerneala.Scene2D.Packages` and `Cerneala.Scene2D.Importers` reference only `Cerneala.csproj`; the compiler references both. All three target `net8.0`. The importers also use `SharpZipLib` `1.4.2` for compressed Tiled layer data. The core assembly gives the packages project access to its internals (`Properties/AssemblyInfo.cs:5`, `InternalsVisibleTo("Cerneala.Scene2D.Packages")`); the core has no reference to the package types.

## Data Flow

### Package directory

A package is a directory with three entries (`PackageFiles`):

| Entry | Content |
|---|---|
| `catalog.c2d` | the CPV2-encoded `PackageIndex`, then 32 bytes: the SHA-256 of the encoded index |
| `payloads.c2d` | all payload blocks, back to back. Each block is one CPV2 value; its offset, length and SHA-256 are in the index |
| `assets/` | copies of the referenced files, for example the atlas images |

CPV2 is a closed wire format:

- every encoded block (the index and each payload) starts with the magic `0x32565043` ("CPV2", little endian);
- each value has one of 42 fixed tags (`Null`, `Int32`, `String`, `TileSet`, `Level`, `Index`, ...); no CLR type names and no reflection-based serialization;
- encoding deeper than 128 levels throws `Payload nesting exceeds 128 levels.`;
- decoding rejects a wrong magic (`Unsupported payload format.`) and bytes left after the value (`Trailing payload data.`). Other decode errors become `InvalidDataException("Invalid scene payload.")`.

### Import and write (build time)

```powershell
dotnet Cerneala.Scene2D.PackageCompiler.dll tiled village.tmj SceneWorldAssets out/tiled
```

1. The compiler checks its arguments. It needs exactly 4, and the first must be `tiled` or `ldtk`. Otherwise it prints `Usage: Cerneala.Scene2D.PackageCompiler <tiled|ldtk> <input-map> <asset-root> <new-output-directory>` and returns `2`.
2. The importer reads the map:
   - Tiled: only JSON format version `1.11` (`Only Tiled JSON format version 1.11 is supported.`);
   - LDtk: only `jsonVersion` `1.5.3` (`Only LDtk JSON version 1.5.3 is supported.`);
   - each problem becomes a `Scene2DDiagnostic` with a code (`SCN2D001`, `SCN2D003`, `SCN2D004`, `SCN2D010`, ...), a message, a file path and a JSON path that starts with `$`. Examples: an unknown field gives `SCN2D004`; a rooted, network or stream asset path gives `SCN2D010`; a missing asset gives `SCN2D001`.
3. The compiler prints every diagnostic to stderr as `<code>: <message> (<file>, <json-path>)`. If `Success` is `false`, it returns `1`. A failed result has `Document == null`, `AssetRootDirectory == null` and no referenced files.
4. `Scene2DPackageWriter.WriteAsync` writes the package:
   1. The destination must not exist. Otherwise: `IOException("A package writer never overwrites an existing destination.")`. Its parent directory must exist.
   2. The document is validated again (`Scene2DModelValidator`). Every file path is normalized; two paths that differ only by case throw.
   3. It creates a staging directory `.cerneala-package-<GUID>` next to the destination.
   4. It writes `payloads.c2d`: document metadata, then per level the level metadata, per map the map metadata and every chunk, then entities and promotions. Grid chunks larger than 16 × 16 cells are split into 16 × 16 pieces; empty pieces are kept.
   5. It copies the referenced files into `assets/`, then writes `catalog.c2d` and its hash. Every file uses `FileMode.CreateNew`.
   6. `Directory.Move(staging, destination)` publishes the package in one step.
   7. On any failure, it deletes only the files and directories it created itself, then rethrows.
5. The compiler prints `Prepared '<input>' -> '<output>' (<n> levels, <m> files).` and returns `0`. Ctrl+C prints `Package preparation canceled.` and returns `130`. An `IOException`, `UnauthorizedAccessException`, `ArgumentException` or `NotSupportedException` prints its message and returns `1`.

`Playground/Cerneala.Playground/SceneWorldPackages.targets` runs this compiler during the build (target `PrepareSceneWorldPackages`, skipped in design-time builds). It compiles `SceneWorldAssets/village.tmj` and `village.ldtk` into `obj/SceneWorldPackages/<Configuration>/<TargetFramework>/{tiled,ldtk}`, then includes the outputs as `Content` under `SceneWorldPackages/`. Because the compiler never overwrites, the target deletes those two folders first, and only inside its own `obj` subtree.

### Open (run time)

```csharp
await using Scene2DPackage package = await Scene2DPackage.OpenAsync("SceneWorldPackages/tiled");
Scene2DPackageLevel level = package.Levels[0];
TileMap2D map = level.CreateTileMap(level.TileMapIds[0]);
```

`OpenAsync` reads only the catalog, never a payload:

1. `MaxCatalogBytes` and `MaxPayloadBytes` must be positive. The defaults are 16 MiB and 64 MiB.
2. The catalog length must be more than 32 bytes, and the index part must fit in `MaxCatalogBytes`. Otherwise: `The package catalog exceeds its read limit or is truncated.`
3. It reads the index and the 32-byte hash and compares them with `CryptographicOperations.FixedTimeEquals`. A difference throws `Package catalog checksum mismatch.`
4. It decodes the index and validates it:
   - file paths are normalized and unique, ignoring case; each asset is a declared file with finite size;
   - level, map, entity and promotion identities are unique; each entity and promotion belongs to a map of its level;
   - each chunk image refers to a declared asset with the same size;
   - budgets: at most 4096 assets, 4096 levels and 4096 maps, 65,536 entities plus promotions, 1,048,576 cells and 65,536 chunks;
   - the blocks, sorted by offset, start at 0, follow each other with no gap or overlap, are each at least 5 bytes long with a 32-byte hash, and end exactly at `DataLength`.
5. The length of `payloads.c2d` must equal `DataLength`. Otherwise: `Package data length differs from its catalog.`

Every failure is an `InvalidDataException`. On failure, `OpenAsync` disposes the reader it was given. If that cleanup also fails, both errors are thrown together in an `AggregateException`.

### Payload reads

`Scene2DPackage.LoadMetadataAsync`, and on a level `LoadMetadataAsync`, `LoadMapMetadataAsync`, `LoadEntityAsync`, `LoadPromotionAsync` and `LoadMapModelAsync`, all go through one internal path:

1. After dispose: `ObjectDisposedException`.
2. A block longer than `MaxPayloadBytes` throws `The payload exceeds the configured read limit.` before any buffer is allocated.
3. It reads the exact byte range, checks the SHA-256 (`Scene payload checksum mismatch.`) and decodes the value. A value of the wrong type throws `Unexpected scene payload type.`
4. Entities and promotions are also compared with their catalog header. A difference throws, for example `Entity payload geometry or identity differs from its catalog.`

`LoadMapModelAsync` loads every chunk of one map and rebuilds a full `TileMap2DModel` for editing. Its cost grows with the whole map. It rejects pieces that overlap, leave holes or fall outside the authored extent.

`GetFilePath(relativePath)` returns the path of a declared file under `assets/`. An undeclared file throws `ArgumentException`. A package opened from a custom `IScene2DPackageRangeReader` has no directory, so it throws `NotSupportedException("A package range reader does not provide local file paths.")`.

### From package to controls

`CreateTileMap(mapId)` is synchronous and reads nothing. It builds an internal `TileMapSource2D` from the map's catalog entry and wraps it with the internal `TileMap2D.FromSource`. The source's loader reads one chunk block per request through the package. Example: the camera moves right, so `TileMap2D` asks for chunk `c7`, and the loader reads that chunk's byte range from `payloads.c2d`, checks its hash and returns the decoded chunk. A request for a replacement catalog throws `A prepared package source cannot load a replacement catalog. Publish a new application-owned source.`

What happens next belongs to the core controls, not to the package:

- when the map has a live simulation context, `TileMap2D` creates a `SceneSpatialResidency2D` for the source. Its default limit is 4 concurrent loads (`maximumConcurrentLoads = 4`);
- the map's warm cache is limited to `TileMap2D.WarmCacheBudgetBytes` = 1,048,576 bytes, and a `RenderSurface2D` prepares at most `WarmPreparationTileBudget` = 256 warm tiles per pass;
- `CollisionWorld2D.PrepareRegionAsync` asks every `ISceneSpatialParticipant2D` in the scene, including `TileMap2D`, to load collision data for a rectangle. A collision query over a chunk that is not loaded throws `SceneCollisionRegionNotReadyException`; see [CollisionWorld2D](../../docs-site/documentation/classes/Cerneala.UI.Controls.CollisionWorld2D.md);
- `SpriteAnimation` and `Sprite2D` are not used by the package or importer projects. Entities come out of a package as `Scene2DEntity` data; the application decides what to create from them.

## Lifecycle And Ownership

| Object | Created by | Released by |
|---|---|---|
| package directory | the writer, in one `Directory.Move` | the build or the developer; the writer never deletes or replaces it |
| `Scene2DPackage` | `OpenAsync` | the caller: `Dispose` or `DisposeAsync` |
| range reader | the caller, or `OpenAsync(string, ...)` | the package. It owns the reader from the moment `OpenAsync` is called, also when opening fails |
| `TileMap2D` from `CreateTileMap` | the caller, one new map per call | the caller: detach it, then `DisposeAsync` |

Dispose order and its effects:

1. `Scene2DPackage.Dispose()` marks the package disposed. New reads throw `ObjectDisposedException`; reads already admitted run to the end.
2. When the last admitted read ends, the package disposes its reader once.
3. `DisposeAsync()` returns a task that completes only after step 2. Calling it again returns the same task.
4. Disposing the package does not dispose maps it created, but after it their chunk loads fail. Detach a map and await `TileMap2D.DisposeAsync()` before disposing the package for a full shutdown. `TileMap2D.DisposeAsync()` on an attached map throws `InvalidOperationException("Detach the map before terminal disposal.")`.

`LocalPackageRangeReader` opens both files with `FileShare.Read` and `FileOptions.Asynchronous | FileOptions.RandomAccess`, so other processes can read the package while it is open.

## Frame Integration

The package has no `FramePhase` and no queue. Its reads are asynchronous (`ValueTask`) and start when `TileMap2D` or `CollisionWorld2D` asks for data. `CreateTileMap` and the level headers are in memory, so creating a map does no I/O. How loaded chunks reach rendering and collision is described in the canonical pages for [TileMap2D](../../docs-site/documentation/classes/Cerneala.UI.Controls.TileMap2D.md) and `CollisionWorld2D`.

## Invariants

- A corrupt catalog hash, a block with a wrong offset, length or hash, a wrong `DataLength`, an escaping file path or a truncated `payloads.c2d` all fail at open with `InvalidDataException`. `tests/Cerneala.Tests.Scene2DPackages/Scene2DPackageTests.cs:CorruptCatalogChecksumsRangesAndDataLengthsFailAtOpen`.
- The payload limit is checked before allocation, and a rejected read does not break later smaller reads. `Scene2DPackageTests.cs:ReadLimitsAreCheckedBeforePayloadAllocationAndDoNotPoisonSmallerReads`.
- The writer never overwrites: a user file in the destination stays unchanged. A failed or canceled write leaves no output directory and no `.cerneala-package-*` staging directory. `Scene2DPackageTests.cs:WriterNeverOverwritesAndRemovesOnlyItsOwnPartialOutput`.
- Paths such as `../secret.txt`, `C:\secret.txt`, `NUL.txt`, `nested/atlas.bin:stream` and `nested/atlas.bin.` are rejected with `ArgumentException`, and nothing is written. `Scene2DPackageTests.cs:PackagePathsCannotEscapeOrAliasTheAssetRoot`.
- Writing the same document twice gives identical catalog bytes. `Scene2DPackageTests.cs:PreparingTheSameInputProducesIdenticalBytesAndNoEditorFileCopies`.
- A custom range reader gets the same catalog and payload hash checks, and is disposed exactly once after a failed open. `tests/Cerneala.Tests.Scene2DPackages/Stage0PackageContractTests.cs:Stage0_RangeReaderChecksCatalogAndPayloadHashes`, `Stage0_RangeReaderRejectsPayloadLengthThatDisagreesWithCatalog`.
- A failed open reports both the primary error and the reader cleanup error. `Stage0PackageContractTests.cs:Stage0_FailedOpenPreservesPrimaryAndReaderCleanupErrors`.
- After `Dispose`, new reads throw `ObjectDisposedException`; `DisposeAsync` completes only after the admitted read ends, and the reader is not disposed before that. `Stage0PackageContractTests.cs:Stage0_DisposeRejectsNewReadsAndDisposeAsyncDrainsAdmittedReadOnce`.
- The public transport is exactly `Scene2DPackagePart { Catalog, Payloads }`, `IScene2DPackageRangeReader : IAsyncDisposable` with `GetLengthAsync` and `ReadAsync`, and the two `OpenAsync` overloads. `tests/Cerneala.Tests.Scene2DPackages/Scene2DPublicBoundaryContractTests.cs:Stage0_Cpv2RangeReaderHasTheSelectedNonspatialTransportShape`.
- The real compiler returns `0` and prints `Prepared`; the package opens with levels and assets; a second run returns `1`, prints `never overwrites` and leaves the catalog unchanged. `tests/Cerneala.Tests.Scene2DPackages/PackageCompilerTests.cs:RealCompilerCreatesReadablePackageAndRefusesToOverwriteIt` (Tiled and LDtk).
- Bad arguments return `2` with `Usage:`; an import failure returns `1`, prints `SCN2D004` and creates no output. `PackageCompilerTests.cs:ArgumentAndImportFailuresReturnErrorsWithoutPublishing`.
- A failed import never publishes a document: `Success` is `false`, `Document` is `null`, and the diagnostic has a file path and a `$` JSON path. `tests/Cerneala.Tests.Scene2DImporters/TiledHostileInputTests.cs:HostileOrUnsupportedDataCannotPublishAPartialDocument`, `LdtkImporterTests.cs:InvalidSourcesAreLocatedAndNeverPublishPartially`.
- An asset path through a junction or symlink below the asset root that points outside it fails with `SCN2D010`. `TiledContractCoverageTests.cs:ReparsePointBelowRootCannotEscapeTheAssetPolicy`.
- Over 32 camera cycles, `TileMap2D` keeps its warm cache within 1,048,576 bytes and its warm preparation within 256 tiles, and releases everything when the camera is far away. `tests/Cerneala.Tests/Controls/TileMapSpatialResidencyTests.cs:PreparedWorkAndResidencyStayBoundedAcrossThirtyTwoDenseCameraCycles`. This test publishes an in-memory model, not a package.
- The CPV2 nesting limit, the trailing-data check and the compiler's exit code `130`: netestat in this pass (taken from source; `PackageValueCodecTests.cs` exists but was not read).

## Diagnostic

Import problems are `Scene2DDiagnostic` values in `Scene2DImportResult.Diagnostics`, capped by `Scene2DImportOptions.MaxDiagnostics` (128); `DiagnosticsTruncated` says when the cap was hit. The package itself reports only exceptions. `Detective.CaptureTileMap(map)` reports a map's residency, warm cache and draw counts; it does not know whether the map came from a package.

## Known Limitations

- Only Tiled JSON `1.11` and LDtk `1.5.3` are accepted.
- `Scene2DImportOptions` budgets: `MaxFileBytes` 16 MiB, `MaxTotalBytes` 64 MiB, `MaxFiles` 1024, `MaxJsonDepth` 64, `MaxCells` 1,048,576, `MaxChunks` 65,536, `MaxLayers` 4096, `MaxEntities` 65,536, `MaxPoints` 4096.
- The compiler catches only four exception types. Any other exception, for example an `InvalidOperationException` or `InvalidDataException` from the writer, ends the process as an unhandled exception instead of a clean exit code `1`. Derived from `Program.cs:35`; not reproduced, so no issue was raised.
- Read cost (allocation, hashing, decode time) is nemăsurat.
- History: [the stage-0 contract proposal](../plans/2026-09-24-scene2d-stage0-contract-proposal.md) (Romanian) records the decisions behind this design. Some of its statements are now out of date: it calls the internal core-to-package bridge "planned, not existing yet", but `InternalsVisibleTo("Cerneala.Scene2D.Packages")` exists; it calls `TileMap2D : IAsyncDisposable` "selected, not existing", but `TileMap2D.DisposeAsync` exists; and its line references into `Scene2DPackage.cs` no longer match.
