# TileMap2D Class

## Definition

Namespace: `Cerneala.UI.Controls`
Assembly/Project: `Cerneala`
Source: `UI/Controls/TileMap2D.cs`

Records one retained stratum of streamed static tile data without creating a public scene node for each tile.

```csharp
public sealed class TileMap2D : SceneNode2D, IAsyncDisposable
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `SceneNode2D` -> `TileMap2D`

## Examples

Inline static authoring lowers to a complete in-memory map model:

```xml
<RenderSurface2D xmlns:r="clr-namespace:Cerneala.UI.Resources;assembly=Cerneala">
  <RenderSurface2D.Resources>
    <r:ImageResource Name="Grass" Source="grass.png" />
    <r:ImageResource Name="House" Source="house.png" />
  </RenderSurface2D.Resources>
  <RenderSurface2D.Scene>
    <Scene2D OrderMode="Layer">
      <TileMap2D Layer="0">
        <Tile Image="$Grass" X="0" Y="0" ImageWidth="32" ImageHeight="32" />
        <Tile Image="$Grass" X="32" Y="0" />
        <Tile Image="$House" X="210" Y="125" Width="24" Height="20" />
      </TileMap2D>
    </Scene2D>
  </RenderSurface2D.Scene>
</RenderSurface2D>
```

`ImageWidth` and `ImageHeight` are paired, positive literal metadata for one image throughout the containing map, not properties stored on each [Tile](Cerneala.UI.Controls.Tile.md). Each omitted destination axis uses the corresponding metadata axis independently. Equal repeated declarations are allowed; conflicting declarations are errors. The generator does not decode an image to determine bounds.

For already-owned in-memory data:

```csharp
var map = TileMap2D.FromModel(model, imageSizes);
scene.Children.Add(map);
```

Here `model` is a [TileMap2DModel](Cerneala.UI.Controls.TileMap2DModel.md), `imageSizes` is an optional `IReadOnlyDictionary<string, DrawSize>` keyed by resource ID, and `scene` is the containing [Scene2D](Cerneala.UI.Controls.Scene2D.md). The factory returns a detached node with `Layer` set to `model.Order`. Its internal adapter retains the complete model; this is not file-backed streaming. For prepared CPV2 maps, use [Scene2DPackageLevel.CreateTileMap](Cerneala.Scene2D.Packages.Scene2DPackageLevel.md) instead.

## Remarks

### Map creation and lifecycle

Create a map from a complete model with `FromModel`, from a prepared package with `Scene2DPackageLevel.CreateTileMap`, or declare direct static `Tile` children in markup. There is no public `Source` or `SourceProperty`, and no public spatial catalog or chunk-loader contract. A map cannot contain live sprites or scene groups. Compose peer maps, sprites and [SceneItems2D](Cerneala.UI.Controls.SceneItems2D.md) through a containing scene. `FromModel` copies `model.Order` to the new node's inherited `Layer`.

A prepared package carries private immutable chunk headers with spatial bounds, payload revisions, image dependencies and collision envelopes before loading cells. The map's internal index retains those headers, not full definitions or cell arrays. Package-backed chunk data, chunk-local collision adapters, retained graphical batches and image leases have separate lifetimes. `FromModel` uses a complete in-memory model instead; it does not make that model lazy or unloadable.

Both attached and nonvisual scenes use [SceneSimulationContext2D](Cerneala.UI.Controls.SceneSimulationContext2D.md). An attached surface uses its root's existing relay and frame loop. A nonvisual scene requires an explicit context, explicit region preparation and bounded `Update` pumping. `FromModel` constructs a node and internal adapter; it does not prepare data or create a hidden root or query-time I/O path.

`TileMap2D` implements `IAsyncDisposable` for terminal cleanup. Detach it first, then call `DisposeAsync()` on the owner thread (or its creator thread if never attached). Calling while attached throws `InvalidOperationException` without consuming disposal; after a valid terminal call, reattachment throws `ObjectDisposedException`. Repeated valid calls observe the same completion. Disposal waits for current and previously retired generations, including delayed releases and their failures. Ordinary detach releases interest but does not itself provide an awaitable drain. Keep a package alive until its package-backed maps have been detached and drained, then await package disposal.

### Required presentation

The frame update establishes camera interest. Required payloads and metadata-declared atlas images can prepare concurrently. Images use [IAsyncImageLoader](Cerneala.UI.Resources.IAsyncImageLoader.md); the map does not dispatch an arbitrary synchronous loader to a worker.

The containing surface omits the whole scene while required presentation is loading or erroneous, using its `Loading`, `Ready` and `Error` presentation states. Ordinary UI and required simulation continue. The scene is not replaced with stale pixels, partial terrain, fabricated collision or replayed input. See [RenderSurface2D](Cerneala.UI.Controls.RenderSurface2D.md).

`Preparation` tracks current required **data** preparation, not optional warm work or completion of atlas decoding. `PreparationError` exposes a current data-preparation failure. The surface's `PresentationError` also includes image/presentation failures. Independent collision regions wait only for their intersecting required data, not unrelated delayed visible chunks or images.

Recording uses already-prepared payloads and resident images. It emits only chunks intersecting the required input footprint. Camera transforms reuse unchanged batches. Grid cells group by atlas inside a chunk; free placements merge only adjacent equal-image runs, preserving painter order even across their consecutive internal chunks. Their independently omitted dimensions come from declared image-size metadata, not the current decoded image dimensions. A replacement image cannot silently resize prepared geometry.

A map or scene-ancestor Prism containing only supported pointwise adjustments or paint styles preserves ordinary viewport selection for data and atlas preparation. Image masks retain their separate resource dependency, including feathering, without selecting extra map chunks. Covered local neighborhood filters automatically expand required input by their composed sampling support, selecting and recording needed off-camera chunks and atlases without selecting the full catalog. Capture coordinate bounds remain based on the complete catalog, not the resident subset. Every active composition without supported automatic input selection requires a finite `PrismInputDomain` on its own map or scene owner, including global filters, the seven remaining styles, wrapped edge modes and unclassified filters. Required chunks and atlases intersecting that complete domain are prepared and recorded, including off-camera input; the final camera clip is applied after the effect. Missing required declarations report a surface presentation error rather than loading the entire world. See [Scene2D's Prism input rules](Cerneala.UI.Controls.Scene2D.md#prism-input-and-streamed-children) for the exact operation/edge-mode coverage. Non-invertible or unbounded transforms can still make geometric selection conservative, and a finite domain must still fit raster and memory limits. The optional warm set below is separate from required effect input.

### Bounded optional working set

For a finite local viewport, the optional warm rectangle extends half its width and height on each side. Optional chunks are prepared but not drawn until required. They retire automatically when they leave this region, cease to fit the budget, lose required dependencies or the map is replaced/detached. This is not an unbounded history of visited terrain.

The default optional charge budget is **1,048,576 bytes per map**. Admission includes:

- the internally declared decoded-data charge for optional acquisition-owned payloads;
- measured managed batch-construction allocations, including temporary builders;
- logical `Width * Height * 4` image bytes used only by optional work, charged once per image.

Unknown data costs do not count as zero and do not qualify for optional loading. Cold optional images require known dimensions before admission. Already-resident images may supply their existing dimensions. Unknown or oversized optional dependencies do not trigger speculative I/O. Required rendering, simulation and explicitly prepared collision data are not cut to fit this optional budget.

These are conservative declared/construction/logical-image charges, not total process RAM or GPU measurements. They exclude independently retained model/package-index data, backend-native textures, staging, frame-owned acquisitions and consumer bookkeeping. `FromModel` declares zero acquisition-owned data cost because its backing store retains the complete payload independently; that does not make the model's memory free or unloadable.

All maps on one surface share at most **256 optional cells or placements per recording**, with rotating priority after required scene recording. Starting optional data/image work and constructing batches use this shared allowance. Larger required chunks remain allowed; optional chunks are not partially built. Existing retained work consumes memory but does not consume the construction allowance again.

Optional requests can complete in later owner-loop updates. Completion does not grant extra preparation work outside the shared recording budget. Superseded requests are cancelled and late results retired, including after detach. Preparation does not install a timer to fill an idle on-demand surface. It does not guarantee a latency bound for arbitrary camera jumps or oversized chunks.

Warm retention and required data ownership use the same last-coordinated spatial viewport. If input or a `Draw` callback changes the camera after that update, recording may use already-resident chunks at the new camera, but does not start optional work for the new viewport or evict data using a mismatched selection. The next ordinary spatial update reconciles both interests, reusing prepared acquisitions rather than releasing and rereading them during the handover. This does not grant collision coverage to optional chunks or delay map-node removal.

Every cached batch owns independent image acquisitions. A collision-only payload does not retain an atlas through the map; another consumer or recorded frame may independently keep the same image alive. Use [Detective.CaptureTileMap](Cerneala.UI.Detective.Detective.md) to inspect rendering and warm-cache counters.

### Static collision and region readiness

A free placement's optional `Tile.Collider` and a grid definition's optional `TileDefinition2D.Collider` are immutable data. Their adapters identify the map as the hit entity, not a public per-cell object. Application child-collection mutation cannot reparent these adapters. Image dimensions never resize collision shapes.

Grid and free-placement payloads remain collision-resident for visual interest, active simulation envelopes and explicitly prepared regions. Their complete conservative collider envelopes account for actual shape geometry and transforms; a visible chunk retains its full collider geometry even if it extends outside its visual rectangle. Leaving every interest retires the adapters and the map's data acquisition.

Before querying distant terrain, moving into an unprepared area or teleporting, acquire [CollisionWorld2D.PrepareRegionAsync](Cerneala.UI.Controls.CollisionWorld2D.md) for the whole query/swept envelope and retain its returned region while needed. A map without a simulation owner or without required coverage raises [SceneCollisionRegionNotReadyException](Cerneala.UI.Controls.SceneCollisionRegionNotReadyException.md). It does not synchronously load, pretend the region is empty or fabricate a wall.

Collision readiness does not require images or UI. In a nonvisual context, pump `Update` while asynchronous work is pending, then perform the query on the owner thread. Dispose the region when its interest ends. Detaching a scene invalidates its prepared regions.

Replacing a map node removes its old terrain from the scene rather than leaving it queryable until replacement data loads. Collision adapter reuse within one map remains chunk-local. Adjacent horizontal full-cell boxes coalesce only when geometry, filters, trigger flags and metadata agree; chunk boundaries remain boundaries.

### Editing and mutation

`TileMap2D` has no public source setter or incremental chunk-publication API. A prepared package is immutable. To edit it, load a complete `TileMap2DModel` through `Scene2DPackageLevel.LoadMapModelAsync`, construct replacement immutable cell/chunk/model data and replace the scene's map with `FromModel`. This can load the full map and replaces node identity and warm residency; it is not a constant-cost cell transaction. `TileChunk2D.Version` is positive revision metadata used by a map's internal chunk cache. A newly created map node has a fresh cache; the public replacement path does not require a version bump merely to evict an old node's cache.

`FromModel` retains a complete in-memory backing model. The map still selects intersecting chunks for recording and collision interest, but releasing that interest does not unload the caller's complete backing model. The factory does not provide a custom loader or disk-backed acquisition for an arbitrary external format. An application may decode its own complete model and pass it to this factory; prepared CPV2 packages use `Scene2DPackageLevel.CreateTileMap` for progressive payload reads.

Use `Refresh()` to request fresh reconciliation and retry current failed data/optional preparation on the owner thread. It does not change model/package content or revisions. Cache release and detach supersede obsolete optional work; a failed required recording does not execute its pending optional batch work afterward.

Model/package offset, tint, opacity and visibility compose with the map's normal scene presentation state. Static tiles have no per-placement events, binding, Motion or Prism. Use a peer sprite for an individual interactive/animated object. Clear the corresponding cell in replacement model data and replace the map node; adding a sprite does not implicitly suppress static data.

### Limits

- Free placements draw complete images without per-placement cropping or flips and need no uniform grid size.
- Positive grid IDs resolve through each acquired payload's used palette; ID `0` is empty.
- Grid chunks cannot overlap. Finite grid bounds contain every chunk; sparse bounds do not allocate coordinate gaps.
- Direct images remain borrowed. Resource-cache images remain valid while their explicit acquisitions are held.
- Import parsing, navigation, animation and pointer routing remain separate facilities.

## Constructors

| Name | Description |
| --- | --- |
| `TileMap2D()` | Creates a detached map with no model data. Prefer `FromModel` or a package level's `CreateTileMap` for a populated map. |

## Properties

| Name | Default | Description |
| --- | --- | --- |
| `Offset` | `default` | Pixel translation composed with model/package offset. |
| `Tint` | `Color.White` | Presentation tint composed with model/package tint. |
| `TransformOrigin` | `default` | Local scene-space origin for inherited transforms. |
| `Preparation` | completed task | Current required data-preparation task. |
| `PreparationError` | `null` | Current required data-preparation failure, if any. |
| `Layer` (inherited) | `0` | Ordering key interpreted by the containing scene. |

`Offset`, `Tint` and `TransformOrigin` have corresponding public UI-property fields. The preparation properties are read-only CLR observations, not writable UI properties.

## Methods

| Name | Description |
| --- | --- |
| `FromModel(TileMap2DModel model, IReadOnlyDictionary<string, DrawSize>? imageSizes = null)` | Creates a detached map from a complete model and copies `model.Order` to `Layer`; forwards intrinsic image-size metadata for geometry validation. |
| `Refresh()` | Reconciles current map data and retries failed preparation on its owner thread. |
| `DisposeAsync()` | Terminally drains current and retired map generations after detach, including release failures. |

## See also

- [Tile](Cerneala.UI.Controls.Tile.md)
- [Scene2DPackageLevel](Cerneala.Scene2D.Packages.Scene2DPackageLevel.md)
- [SceneSimulationContext2D](Cerneala.UI.Controls.SceneSimulationContext2D.md)
- [TileMap2DModel](Cerneala.UI.Controls.TileMap2DModel.md)
