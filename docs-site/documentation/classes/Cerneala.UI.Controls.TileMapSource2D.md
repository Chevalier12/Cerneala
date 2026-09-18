# TileMapSource2D Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/TileMapSource2D.cs`

Separates a complete static-map catalog from validated, asynchronously acquired chunk payloads.

```csharp
public sealed class TileMapSource2D : ISceneSpatialSource2D<TileMapChunkData2D>
```

## Examples

Adapt a complete in-memory model, declaring the dimensions used by natural-size placements:

```csharp
var picture = new ImageReference(new ResourceId<ImageResource>("Tree"));
var model = new TileMap2DModel([new Tile(picture, x: 200, y: 100)]);
var source = TileMapSource2D.FromModel(model,
    new Dictionary<string, DrawSize> { ["Tree"] = new(32, 64) });

await using var residency = new SceneSpatialResidency2D<TileMapChunkData2D>(source);
using var region = await residency.AcquireAsync(new DrawRect(190, 90, 60, 90));
TileMapChunkData2D data = region.GetValue(region.Entries[0].Id);
Tile tree = data.Placements[0];
```

## Remarks

`Catalog` is the current immutable [TileMapCatalog2D](Cerneala.UI.Controls.TileMapCatalog2D.md); `Entries` is that catalog's stable spatial-list instance. Reading either property does not call the loader or resolve image resources. Assign it to [TileMap2D.Source](Cerneala.UI.Controls.TileMap2D.md), or acquire data independently through [SceneSpatialResidency2D&lt;T&gt;](Cerneala.UI.Controls.SceneSpatialResidency2D_T_.md). The control reconciles camera, simulation and explicit collision interests through its common scene owner. Constructing a source alone does not load data.

The loader receives the catalog snapshot and chunk metadata captured for its request, plus a cancellation token. It must return a non-null live lease and keep its payload valid until release. Grid payloads carry their complete used definitions and collider prototypes; the catalog has no hidden full-palette or opaque-properties lookup. The source validates grid kind, coordinates, dimensions and revision, declared tile IDs and image references, loaded definitions against declared image sizes, free-placement bounds, and actual expanded collider count and envelope before returning an acquisition. Missing data, invalid data and I/O errors are failures, not successful empty chunks. A loader is responsible for releasing partial work when it fails before returning its lease.

Chunk metadata can declare `DataResidencyBytes`, a conservative cost for data retained by its acquisition. Null means unknown and is not eligible for optional byte-budgeted preloading; it does not prevent required loading. `LoadAsync` does not apply an optional budget, reject an oversized declared cost or infer costs by traversing opaque payload properties. The source's loader must provide an honest declaration if it wants its data considered for optional preparation. The control includes declared optional data costs in its bounded warm working set; this source class itself does not reserve bytes or perform package I/O.

Validation uses the captured catalog, not a later publication. `SetCatalog` replaces the catalog before raising `Changed` on the publishing thread. Already-owned acquisitions are not revoked by this notification. Consumers must reconcile publication with their own derived state and check region currency; this class does not marshal scene mutations to a UI thread or retain removed scene geometry. Reuse an `(Id, Version)` only for the same immutable payload. Changing placement/cell data, definitions, source rectangles, atlas references, collider prototypes, opaque metadata or representation requires a new revision for every affected chunk payload. A catalog's map-level `Version` or a tileset's `Version` does not replace chunk revisions.

Cancellation is checked before invoking the loader and forwarded to it. If an uncooperative loader returns a valid lease after cancellation, the source validates and returns it to the requester. Residency then retires the unwanted result and can report a throwing late release through its asynchronous disposal. A caller using `LoadAsync` directly must dispose every successfully returned lease, even if its token was cancelled meanwhile. The source does not add a parallel cancellation/retirement owner.

### In-memory adapter

`FromModel` reads the already-supplied model to derive metadata; it performs no file/image decode. Grid payloads reuse the immutable `TileChunk2D` objects and contain per-chunk used palettes: subset tilesets preserve the original set ID, atlas, version and properties, and reuse the used definition objects. Unused definitions are not copied into those payloads. Nonspatial model properties remain with the application model/document, not the spatial catalog. Free placements are divided into consecutive groups of at most 256, preserving declaration/painter order even when chunk bounds overlap. Each group contains the existing immutable `Tile` references. Chunk IDs are local to the source; their adapter-generated spelling is not an application identity contract.

The adapter retains all backing payloads. Releasing a spatial acquisition does **not** unload these source-owned cells/placements or an application-held model. To replace model data, construct a replacement adapter; `SetCatalog` does not replace the adapter's fixed backing dictionary. Direct images remain borrowed and are not disposed by a data lease.

Accordingly, each adapter chunk declares `DataResidencyBytes = 0`: it returns the same source-owned payload object on subsequent acquisitions, rather than creating or extending its data residency. This includes the adapter-created used palettes and free-placement arrays, retained by its backing dictionary. Zero is not a statement that the source, model, lease bookkeeping or images use no memory. Custom loaders that allocate payload data must not copy this declaration without accounting for that data.

For a path-backed free placement, each omitted dimension is obtained from the supplied `imageSizes` dictionary. Missing required metadata is an explicit construction error, not an instruction to decode every image. If both destination dimensions are explicit, they do not need an image-size declaration for spatial geometry. Direct-image dimensions are captured from the already-existing image. Publishing different intrinsic dimensions requires corresponding updated spatial metadata; this source does not silently resize a published catalog from a later image decode.

The catalog and loader are also usable without an in-memory model, for an application-owned backing store. This class does not itself define an indexed package format, open Tiled/LDtk files, load graphical resources, schedule camera preparation, or impose a frame-time bound.

## Constructors

| Name | Description |
| --- | --- |
| `TileMapSource2D(TileMapCatalog2D, Func<TileMapCatalog2D, TileMapChunkInfo2D, CancellationToken, ValueTask<SceneSpatialLease2D<TileMapChunkData2D>>>)` | Installs the initial catalog and application-owned acquisition callback. |

## Properties

| Name | Description |
| --- | --- |
| `Catalog` | Current atomically published map metadata. |
| `Entries` | The current catalog's immutable spatial entries, in catalog order. |

## Methods

| Name | Description |
| --- | --- |
| `FromModel(TileMap2DModel model, IReadOnlyDictionary<string, DrawSize>? imageSizes = null)` | Creates a source backed by the complete in-memory data. |
| `SetCatalog(TileMapCatalog2D replacement)` | Publishes a new immutable catalog and raises `Changed`. |
| `LoadAsync(SceneSpatialEntry2D entry, CancellationToken cancellationToken = default)` | Returns a validated chunk acquisition for a current ID/revision. |

## Events

| Name | Description |
| --- | --- |
| `Changed` | Signals that the current catalog has been replaced; may run on a worker thread. |

## Exceptions

Invalid catalog/payload data produces argument errors. A removed or revised requested entry, or a null lease, produces `InvalidOperationException`. An already-disposed returned lease produces `ObjectDisposedException`. Loader failures propagate. If validation and release both fail, `AggregateException` preserves both failures.

## See also

- [TileMapCatalog2D](Cerneala.UI.Controls.TileMapCatalog2D.md)
- [TileMapChunkInfo2D](Cerneala.UI.Controls.TileMapChunkInfo2D.md)
- [TileMapChunkData2D](Cerneala.UI.Controls.TileMapChunkData2D.md)
- [TileMap2DModel](Cerneala.UI.Controls.TileMap2DModel.md)
- [ISceneSpatialSource2D&lt;T&gt;](Cerneala.UI.Controls.ISceneSpatialSource2D_T_.md)
