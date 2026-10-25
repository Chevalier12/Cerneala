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

### Collider contact events

`Collider2D.Entered` and `Exited` belong to the core Scene2D controls in `UI/Controls`, not to the package layer this file describes. The canonical page is [Collider2D, Contact events](../../docs-site/documentation/classes/Cerneala.UI.Controls.Collider2D.md#contact-events).

- **Contact rule.** The same rule as `CollisionWorld2D.Intersects`: the shapes overlap, or their edges touch within `1e-5`. The layer/mask filter must pass in both directions. Triggers count the same as solid colliders.
- **Where the state lives.** Each collider holds its reported contacts (`reportedContacts` and `reportedBy` in `UI/Controls/Collider2D.cs`). The world holds only `trackedEntries` (colliders that have an entry in the world and at least one contact handler) and the dirty colliders (the ones to re-evaluate at the next delivery). The world does not keep the reported contacts: `Reset` empties it (`CollisionWorld2D.cs:354` calls `ForgetContactEntries`) and `Rebuild` refills it (`:383-393`, wrapped in `BeginContactRebuild` and `EndContactRebuild`), so contacts kept there would be lost.
- **Marking.** `AddOrUpdate` and `Remove` reach `MarkContactDirty` (`CollisionWorld2D.Contacts.cs:39-49`). It marks nothing when marking is suspended during a rebuild, when the scene has no simulation context, or when the world tracks no collider with a handler and the collider has no contact state. A scene without contact handlers marks nothing and runs no new exact tests: `ColliderContactEventTests.ASceneWithoutContactHandlersDoesNoExactTestsWhenAThousandCollidersMove`.
- **Delivery.** `UiHost.UpdateCore` calls `UIRoot.DeliverSceneContacts` once per update (`UiHost.cs:189`), right after `InputBridge.Dispatch` (`:186`) and before input processing. Phase 1 calls `CollisionWorld.PrepareContacts` for every registered scene: it computes the contact difference, updates the reported contacts and queues the events (`CollisionWorld2D.Contacts.cs:214-216`). Phase 2 calls `CollisionWorld2D.RaiseContacts` once. It raises all `Exited` before all `Entered`, then orders by scene (the order in which the surfaces' contexts registered with the root), by the attachment ordinal of the collider that receives the event, by the ordinal of `Other`, and by queue sequence. A handler that throws does not stop the others: `RaiseContacts` throws one `AggregateException` after all events. A collider change made by a handler waits for the next delivery. A headless `SceneSimulationContext2D` delivers in `SceneSimulationContext2D.Update()` instead.
- **Wake.** `CollisionWorld2D.HasPendingContacts` is true while a change waits for delivery. `UIRoot.HasPendingSceneContacts` aggregates it over the scenes registered with the root. `PumpOnce` renders while it is true (`WindowApplicationRuntime.cs:672`, see [Hosting and Platform](hosting-and-platform.md)) and Servo is not idle while it is true (`ServoContext.cs:121`, see [Servo](servo.md)). For `PumpOnce` it matters only for `RenderSurface2DRedrawMode.OnDemand`: a `Continuous` surface (the default) already asks for a frame on every pump. Servo checks it in every redraw mode. The wake is conservative: a changed collider whose evaluation raises no event still costs one more frame.

Tests: `tests/Cerneala.Tests/Controls/ColliderContactEventTests.cs`, `tests/Cerneala.Tests/Controls/ColliderContactCharacterizationTests.cs`, `WindowRuntimeTests.ContactChangesMadeByAContactHandlerKeepPumpOnceRenderingUntilTheyAreDelivered` and `ServoSynchronizationTests.WaitForIdleIsNotIdleWhileContactChangesWaitForDelivery`.

### Vision cones

`Sprite2D.Vision` and `Scene2D.Vision` each hold one `ConeCollider2D`: a cone-shaped sensor in a slot of its own, apart from the body. Like the contact events, it belongs to the core Scene2D controls in `UI/Controls`, not to the package layer this file describes. The canonical pages are [ConeCollider2D](../../docs-site/documentation/classes/Cerneala.UI.Controls.ConeCollider2D.md), [Sprite2D, Vision](../../docs-site/documentation/classes/Cerneala.UI.Controls.Sprite2D.md#vision) and [Scene2D, Vision](../../docs-site/documentation/classes/Cerneala.UI.Controls.Scene2D.md#vision). The decisions behind it are in [the Vision plan](../plans/2026-10-25-scene2d-vision.md) (Romanian).

**The cone shape.** The public properties are in degrees. The shape the collision world uses is in radians.

- `ConeCollider2D.GetLocalShape(flip)` (`ConeCollider2D.cs:80-113`) builds `ColliderLocalShape2D.Cone(radius, startAngle, sweep)` (`ColliderGeometry2D.cs:63-64`). The sweep is `Angle` in radians (`ConeCollider2D.cs:112`). The start is `Direction - Angle / 2`, moved into [0, 2π) (`:100-110`). Example: `Direction = 30` and `Angle = 90` give a sweep of π/2, and the start plus half the sweep is 30°. Test: `ConeCollider2DTests.AConeCoversAnAngleAroundItsDirectionAndTheLocalShapeIsInRadians`.
- The flip mirrors `Direction` and the apex (decision D18, revised on 2026-10-25). For `Direction` horizontal gives `180 - Direction` and vertical gives `-Direction` (`ConeCollider2D.cs:90-98`). For `Direction = 30` the four flips give 30, 150, -30 and 210. Test: `ConeCollider2DTests.FlipMirrorsTheDirectionOfTheLocalShape`.
- The flip is the one the image is drawn with: `Sprite2D.Flip` composed (xor) with the `Flip` of the current animation frame. `ComputeVisionMirror` (`Sprite2D.cs:742-757`) composes them with `ComposeFlip` (`:856-859`), the same call that `ResolveGeometry` makes for the draw (`:781`). The cone reads it through `ReadVisionMirror` (`:721-725`) in `GetLocalShape()` (`ConeCollider2D.cs:70-71`). Tests: `SceneVisionMirrorTests.AFrameThatFlipsTheImageMakesTheConeLookLeftWhileTheSpriteFlipIsNone` and `TheSpriteFlipAndTheFrameFlipComposeLikeTheImage`.
- The apex mirrors across the centre of the sprite's local image rectangle. An image flips only by swapping texture coordinates inside a destination rectangle that does not move (the destination point transform ignores the flip, `DrawImageOptions.cs:206-218`; the texture coordinates swap at `:256-263`), so that rectangle is the mirror. `ConeCollider2D.GetShapeOffset` (`:74-77`) returns `SpriteMirror2D.MirrorOffset` (`SpriteMirror2D.cs:17-19`): `OffsetX` becomes `2 * centreX - OffsetX` on a horizontal flip, and `OffsetY` likewise on a vertical flip. The rectangle is the sprite's hit-test local bounds (`GetHitTestLocalBounds`, used at `Sprite2D.cs:749`), so it follows `Width`, `Height`, `Origin`, the image or frame size and, for a trimmed frame, the logical frame. When those bounds are not known, the centre is the anchor `(0, 0)` and the offsets change sign (`:750`). Example: a 32 by 32 image at `Origin = (0, 0)` has centre lines `x = 16` and `y = 16`; with `OffsetX = 28`, `OffsetY = 10`, a horizontal flip puts the apex at `(4, 10)`. The geometry takes the offset from the virtual `Collider2D.GetShapeOffset` (`Collider2D.cs:290`), which every other collider leaves at `OffsetX` and `OffsetY`; `SceneGeometry2D.TryCreateColliderGeometry` (`SceneGeometry2D.cs:286`) and `GetInputBounds` (`:80`) both call it. Tests: `SceneVisionMirrorTests.TheApexMirrorsAcrossTheCentreLinesOfTheImage`, `AnImageCentredOnTheAnchorKeepsTheApexAtTheAnchorAndMirrorsAnyOffsetAroundIt`, `AnImageDrawnAtADeclaredSizeMirrorsAcrossTheCentreOfThatSize`, `ADeclaredSizeWithoutAnImageIsTheRectangle`, `ATrimmedFrameMirrorsAcrossTheCentreOfItsLogicalFrameNotOfItsVisiblePixels`, `WithoutAKnownRectangleTheAxisIsTheAnchorOfTheSprite` and `SceneVisionTests.FlipMirrorsTheApexAcrossTheCentreOfTheImageAndTheDirectionOfTheVision`.
- When the collision world is told. `Sprite2D` keeps the mirror the cone last used (`visionMirror`, `Sprite2D.cs:52`). `RefreshVisionMirror` (`:731-740`) computes the current mirror and, only when it differs, stores it and sends a `Geometry` collision mutation for the cone. A centre is zero on an axis that is not flipped (`SpriteMirror2D.cs:9`, `Sprite2D.cs:755-756`), so a change of size on an unflipped axis is not a change. It returns first for a sprite without a `Vision` (`:733`). It runs from `OnPropertyChanged` for `Flip`, `Image`, `Width`, `Height`, `Origin`, `SourceX`, `SourceY`, `SourceWidth`, `SourceHeight`, `Animations` and `AnimationState` (`:699-702`, list at `:706-717`; after the playback has followed the new selection), from `AdvanceAnimation` (`:338`), from `AdvanceScheduledAnimation` (`:366`) and from `RestartAnimation` (`:323`). The last three run only when the frame's presentation changed. Tests: `SceneVisionMirrorTests.ChangingAPropertyThatMovesTheMirrorCentreNotifiesTheVisionOnce`, `ChangingAPropertyThatLeavesTheMirrorUnchangedNotifiesNothing`, `AFrameAdvanceNotifiesTheVisionOnlyWhenTheComposedFlipChanges`, `RestartingTheAnimationOntoAFrameWithAnotherFlipNotifiesTheVision`, `SwitchingTheAnimationStateToAnUnflippedClipMirrorsTheConeBackAtTheNextUpdate`, `AFrameAdvanceOfASpriteWithoutAVisionMutatesNothing` and `ASpriteWithoutAVisionMutatesNothingWhenItsImageOrFrameChanges`.
- Timing. The frame advance runs in the time phase of the update: `UiHost.UpdateCore` calls `TimeSensitiveRenderInvalidator.Invalidate` (`UiHost.cs:166`), which reaches `RenderSurface2D.UpdateRenderTime` and its `AdvanceAnimation` and `AdvanceScheduledAnimation` calls (`RenderSurface2D.cs:200`, `:230`), and only later calls `DeliverSceneContacts` (`UiHost.cs:189`). So a frame advance that flips the image changes contacts in the same update. Test: `SceneVisionMirrorTests.AFrameAdvanceToAFrameWithAnotherFlipMirrorsTheConeInTheSameUpdate` (rate 1 follows the surface clock, any other rate advances on every frame).
- Culling. `SceneItems2D` parks a culled sprite whose frames cannot change its bounds, and a parked sprite is not visited for frame changes (`SceneItems2D.cs:607`, `RenderSurface2D.cs:366-377`). A sprite with a `Vision` is never parked (`Sprite2D.cs:355`), and assigning a `Vision` to a parked sprite unparks it (`:77-80`). Tests: `ACulledSpriteWithAVisionKeepsFollowingItsFrameFlips` and `AssigningAVisionToAParkedSpriteUnparksIt`. This also keeps the mutation out of the record phase: the only advance that recording can start is unparking (`SceneItems2D.cs:597`).
- Known limit. When a resource image finishes loading, nothing tells the sprite from a safe place. The completion only invalidates the sprite (`ImageResourceResolver.cs:99-103`). The place that notices a resident image, the presentation check, also runs while a frame is recorded (`RenderSurface2D.cs:637`, `:647`; `Sprite2D.CheckPresentation`), and recording must not change collision state. So the cone keeps the centre it had until the next change of the sprite in the collision world, such as a new `X`. Test: `SceneVisionMirrorTests.AResourceImageThatFinishesLoadingMovesTheApexOnlyAtTheNextChangeOfTheSprite`.
- Cost, not measured per frame. Per frame change of a sprite with a `Vision` the added work is one `ComposeFlip`, a struct compare, and, for a flipped sprite, one `GetHitTestLocalBounds` read, which is cached by property version, frame and image; a mutation is sent only when the mirror changed. Reading the mirror for the cone's geometry allocates nothing once warm: `SceneVisionMirrorTests.ReadingTheMirroredShapeAndOffsetOfAFlippedConeAllocatesNothingOnceWarm`. A sprite without a `Vision` adds one field read per property change and per frame change.
- Motion. `AnimatablePropertyRegistry` lists `ConeCollider2D.Radius`, `Angle` and `Direction` (`AnimatablePropertyRegistry.cs:74-76`) with the entry that `CircleCollider2D.Radius` has (`:73`): `FloatMixer`, the 180 ms collider tween, no implicit animation. `Direction` is interpolated in a straight line like `UIElement.Rotation`, so 350 to 10 sweeps 340 degrees and 350 to 370 takes the short way. The C# call `element.Motion().Animate(property).To(value).With(spec)` does not read the registry (`MotionAnimationBuilder.cs:36-53`); the registry decides whether a Motion transaction animates a write (`MotionTransactionContext.cs:86`) and supplies the default spec of a markup `@animate` without a spec (`GeneratedMarkupMotion.cs:365`, `:417`). A value the cone refuses (`ConeCollider2D.cs:115-137`) is skipped by an animated write. Tests: `ConeColliderMotionTests`, `CollisionStageOneContractTests.MotionRegistryInterpolatesGeometryAndRejectsDiscreteColliderProperties`. No markup test animates a collider property, so markup `@animate` on a cone is not tested.
- `Angle = 360` returns a circle shape (`ConeCollider2D.cs:84-87`). Test: `ConeCollider2DTests.ThreeHundredSixtyDegreesGivesTheSameResultAsACircleOfTheSameRadius`.
- GJK needs a convex shape, and a cone is convex up to a half circle. For a cone of at most 180° the support function (`CollisionNarrowPhase2D.cs:947-948`, `GetConeSupport` at `:959-988`) returns the point on the arc when the direction lies inside the arc, and otherwise the best of the apex and the two arc ends. The cone skips the polygon fast paths (`TryFastContact`, `:359-365`).
- A wider cone is the union of two cones of half the sweep. `TryContact` tests each half (`:30-39`; `TrySplitWideCone` at `:990-1007`). The limit is π plus `1e-5` (`ConvexConeSweep`, `:13`), so a cone of exactly 180° that float rounding makes a hair wider is not split. Test: `ConeCollider2DTests.AConeWiderThan180IsHitThroughBothSidesOfItsNotchButNotInsideTheNotch`.
- The index bounds are tight: the apex, the two arc ends, and the arc points where the transformed x or y is extreme (`SceneGeometry2D.cs:446-466`). Example: `Radius = 100`, `Angle = 30`, `Direction = 0` gives a box 100 wide and 51.76 tall, not 200 by 200. Tests: `ConeCollider2DTests.ANarrowConeHasBoundsAroundItsApexAndArcNotAroundTheWholeCircle` and `WideConeBoundsCoverTheApexTheArcAndItsExtremePoints`. `SceneGeometry2D.ContainsPoint` has an exact cone case too (`:328`; `ContainsConePoint` at `:335`), but picking never reaches it for a `Vision`, because picking skips sensors.
- A cone cannot be raycast or shape-cast. `ThrowIfCone` (`CollisionNarrowPhase2D.cs:1021-1027`, called at `:84` and `:141-142`) guards the narrow phase; the public queries never reach it, because they skip sensors (see the sensor role).

**The Vision slot.**

- `ColliderSlot2D` has a role, `Body` or `Vision` (`ColliderSlot2D.cs:4-8`). `Sprite2D` owns one slot of each (`Sprite2D.cs:56-57`). `Scene2D` owns only the vision slot (`Scene2D.cs:51`).
- The body slot refuses a cone before anything changes (`ColliderSlot2D.cs:28-32`), so a body already in `Collider` stays. Tests: `ConeCollider2DTests.ASpriteBodySlotRejectsAConeWithAMessageThatNamesVision` and `ARejectedConeLeavesTheExistingBodyInPlace`.
- Ownership: `Collider2D.AllowsCollisionParent` accepts only a `Sprite2D` (`Collider2D.cs:32`), `ConeCollider2D` accepts a `Sprite2D` or a `Scene2D` (`ConeCollider2D.cs:66`), and `ValidateParentChange` (`Collider2D.cs:19-30`) refuses any change that the owner's slot did not make. So `scene.Children.Add(cone)` and `scene.LogicalChildren.Add(cone)` throw. Test: `SceneVisionTests.ASceneOwnsItsVisionAsALogicalChildAndNeverAsAChild`.
- A `Scene2D` keeps its `Vision` after its `Children` in `LogicalChildren`. The slot appends a new cone at the end, or puts a replacement at the index of the old one (`ColliderSlot2D.cs:34-46`). `Scene2D.ChildCollection` inserts a child into `LogicalChildren` at the child's own `Children` index (`Scene2D.cs:494`), which is never past the cone, so an insertion pushes the cone to the right and never goes after it. Example: a group with one child and a `Vision` has `LogicalChildren` = [child, cone]; `Children.Insert(0, other)` gives [other, child, cone]. Tests: `SceneVisionTests.ChildrenOfASceneWithAVisionAddInsertReplaceAndRemoveWithoutTouchingTheCone` and `ReplacingAVisionKeepsItAfterTheChildrenOfItsGroupAndNeverMovesTheBodyOfASprite`.
- The child-bounds index skips colliders (`SceneChildBoundsIndex2D.cs:528-534`). So its length stays equal to `Scene2D.Children.Count`, and a group with a `Vision` keeps the culled fast path of `Scene2D.Record`. The cone adds no visible bounds and no input candidate. Tests: `SceneVisionTests.AVisionOnAGroupDoesNotChangeWhichChildrenTheGroupAndItsParentRecordAndOrder` and `ALargeVisionOnAGroupAddsNoVisibleBoundsAndNoInputCandidate`.
- The slot sets `Collider2D.VisionOwner` before it inserts the cone (`ColliderSlot2D.cs:65`) and clears it after it removes the cone (`:60`). `IsVisionSensor` is `VisionOwner is not null` (`Collider2D.cs:37-39`).

**The sensor role.** The collision world copies `VisionOwner` into the entry as `SensorOwner` when it creates or refreshes the entry (`CollisionWorld2D.cs:454-465`, `:488`), and `IsSensor` reads it (`:670`).

- Queries: `PassesQueryFilter` rejects a sensor target (`CollisionWorld2D.cs:582-583`; used at `:91`, `:151` and `:213`). So `Overlap`, `Raycast` and `MoveAndCollide` of other colliders never return a `Vision`. Tests: `SceneVisionTests.OverlapOfAnotherColliderDoesNotReturnAVision`, `RaycastThroughAVisionDoesNotReturnIt` and `MoveAndCollideThroughAVisionDoesNotBlockAndReportsNoTriggerHit`.
- Pairs: `PassesPairRules` (`:544-546`) is the sensor rule followed by the layer and mask filter. `Intersects`, `Overlap` and contact events use it (`:58`, `:92`; contacts through `IntersectsEntries`, `CollisionWorld2D.Contacts.cs:174`). The sensor rule (`PassesSensorRule`, `:550-566`) says that two sensors never touch, and a sensor never touches a collider inside the subtree of its owner. This is decision V1: `bee.Vision` ignores `bee.Collider`; `dragon.Vision` on a `Scene2D` ignores every collider of the group; `head.Vision` on a sprite of the group ignores only `head.Collider`. Tests: `SceneVisionTests.AVisionNeverSeesItsOwnersBodyEvenWhenTheBodyOverlapsTheCone`, `AGroupVisionIgnoresEveryColliderInsideTheGroupSubtree`, `AChildSpriteVisionIgnoresOnlyItsOwnBodyAndSeesTheSiblingPartsOfItsGroup`, `TwoOverlappingVisionsRaiseNoEventsForEachOther` and `IntersectsTellsWhetherABodyTouchesAVisionButNeverForTwoVisionsOrTheOwnersSubtree`.
- One-sided contacts: `UpdateContact` reports to a receiver when the pair touches, the receiver has handlers, and either the receiver is the sensor or the other collider is not a sensor (`CollisionWorld2D.Contacts.cs:209-211`). In a body-and-sensor pair, only the sensor gets `Entered` and `Exited`, with the body as `Other`. Tests: `SceneVisionTests.TheBodyThatEntersAVisionReceivesNoEventForIt` and `AVisionOnASpriteRaisesOneEnteredAndOneExitedForTheBodyThatComesAndGoes`.
- Picking: `CollectPointHits` skips a sensor entry (`CollisionWorld2D.cs:293-296`). Test: `SceneVisionTests.AClickInsideTheVisionButOutsideTheBodyDoesNotHitTheBee`.
- Input route: `Collider2D.ParticipatesInInputRoute` is `false` for a sensor (`Collider2D.cs:41-44`; the base value is `SceneNode2D.cs:287`), so `IInputSubtreeHost.GetInputSubtreeChildren` (`SceneNode2D.cs:289-291`) skips the cone. The cone is then not a node of the input route (`ElementInputRouteBuilder.cs:95-104`), not a Servo semantics node (`SemanticsProvider.cs:34-40`) and not part of the input bounds of its owner (`SceneGeometry2D.cs:95-101`). Servo aims a click at the centre of those bounds. Example: a bee with a body at x 38..48, y 95..105 and a cone of `Radius = 150` in front of it has the root bounds of the body alone, so `ClickAsync` on the bee clicks the body. A body collider keeps its place in the route. Tests: `SceneVisionTests.AVisionDoesNotChangeTheInputBoundsOfItsOwner`, `ServoClickOnTheOwnerOfAVisionHitsTheOwnerAndNotTheEmptyPlaceInFrontOfIt` and `AVisionIsNeitherInTheInputRouteNorInTheServoTreeWhileTheBodyIs`.
- `MoveAndCollide(vision, ...)` throws `InvalidOperationException` right after the argument checks, before it looks the cone up in the world (`CollisionWorld2D.cs:185-189`), so it also throws when the cone is disabled or its owner is outside the scene (decision V2). Test: `SceneVisionTests.MoveAndCollideOfAVisionThrowsAnErrorThatNamesVision`.
- Debug overlay: a `Vision` is drawn as two rays and an arc in `VisionColor` (190, 120, 255), whatever its layer or trigger flag. A `CollisionMask` of zero still gives the grey of a masked collider. The label says `vision`, or `masked` for a zero mask (`Scene2DDebugOverlay.cs:59`, `:157-160`, `:185`; `DrawCone` at `:190-205`). Tests: `SceneDebugOverlayTests.ASpritesVisionIsDrawnAsTwoRaysAndAnArcInTheVisionColourAndItsBodyKeepsItsUsualColour`, `AFullCircleVisionThatIsATriggerIsStillDrawnInTheVisionColourAndLabelledVision` and `AVisionWithAnEmptyMaskKeepsTheMaskedGreyAndTheMaskedLabel`.

**Markup.** A cone is declared in `<Sprite2D.Vision>` or `<Scene2D.Vision>`, and only there (decision V4).

- Editor and build share one rule set, in the language model. `GetColliderPlacementError` (`Cerneala.Language/Semantics/CernealaSemanticModel.Tiles.cs:64-111`, called at `CernealaSemanticModel.cs:280`) accepts exactly one cone as the child of a `Vision` property element and rejects every other element there. It also rejects a cone in any other place, with `CERNEALAUI005` and a message that names `Vision`. `ReportVisionPropertyElementShape` (`CernealaSemanticModel.Tiles.cs:37-58`, called at `CernealaSemanticModel.cs:225`) reports a `Vision` element that is empty, that has text other than whitespace and comments, or that has an attribute other than an `xmlns` declaration (all three: "requires exactly one ConeCollider2D child"), and a second `Vision` element on the same owner. A `Vision` does not count as a second collider: the "at most one collider" count (`CernealaSemanticModel.cs:305-318`) takes only direct children and the children of a `Collider` property element, and a `Vision` property element is neither.
- The source generator stops before it emits when the language model reports an error (`UiMarkupGenerator.cs:489`, `TryGetEmissionDocument` at `:632-652`). So the `CERNEALAUI005` of a misplaced cone in a build is the language model's diagnostic. When there is no error, `EmitVisionPropertyElement` (`UiMarkupPropertyElementEmitter.cs:256-295`, called at `UiMarkupElementEmitter.cs:238`) emits `owner.Vision = cone;`. `IsVisionPropertyElement` (`UiMarkupPropertyElementEmitter.cs:238-254`) is in the list of property elements that are not content children (`:125`), so the cone is not also emitted as `Collider` or into `Children`. The emitter repeats the shape check (`UiMarkupPropertyElementEmitter.cs:276-291`: exactly one cone, no text, no attribute except namespace declarations) with the same message, as a safety net. The language model reports those cases first, so the build normally shows the language model's diagnostic.
- Completion offers `ConeCollider2D` only inside an empty `Vision` element, and never offers the four body shapes there (`CernealaCompletionService.cs:287-290`, `:318-319`).
- Tests: `tests/Cerneala.Tests.SourceGen/UiMarkupGeneratorVisionTests.cs` (for example `SpriteVisionMarkupKeepsTheBodyAndTheVisionInSeparateSlots` and `ConeInTheSpriteBodySlotMarkupIsRejectedWithAMessageThatNamesVision`) and `tests/Cerneala.Tests.Language/ColliderOwnershipLanguageTests.cs` (for example `VisionMarkupRaisesNoColliderOwnershipOrOtherErrorDiagnostics` and `VisionCompletionOffersTheConeOnlyInsideAnEmptyVisionSlot`).

Cost of the sensor rule per frame was not measured. The extra work is two `null` checks per evaluated pair, one climb through `LogicalParent` for each body-and-sensor pair, and one `bool` check per candidate in `CollectPointHits`.

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
