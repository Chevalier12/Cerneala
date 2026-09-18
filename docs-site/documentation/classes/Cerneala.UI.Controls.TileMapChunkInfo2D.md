# TileMapChunkInfo2D Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/TileMapCatalog2D.cs`

Describes one static chunk's identity, conservative bounds, representation, count, dependencies and optional data-residency charge before its data is acquired.

```csharp
public sealed class TileMapChunkInfo2D
```

## Remarks

`Spatial` supplies a nonempty stable ID, positive payload revision, finite map-local visual bounds, and an optional independent collision envelope. Static chunks cannot set `IsSimulated`; active entities belong in the scene's simulation source rather than this static-map data contract.

The grid overload records a positive cell rectangle, unique positive tile IDs and unique image dependencies; ID zero is empty and is not included in the dependency list. The free-placement overload records a positive tile count and unique image references. Inputs are copied. Neither overload receives cell arrays or full definitions, creates nodes or invokes a loader. Direct image references remain borrowed existing objects, not file-backed image metadata.

Visual bounds must conservatively contain the chunk's actual geometry. Collision bounds must contain the complete transformed descriptor geometry, including geometry outside its visual rectangle and zero-thickness segments. `ExpandedColliderCount` is the exact number of non-empty cell/placement descriptors before coalescing, not the eventual number of live collision adapters. A positive count requires a non-null collision envelope. A non-null conservative envelope with a zero count is allowed.

`TileMapCatalog2D` checks grid bounds, catalog-wide limits and overlap without loading cells or definitions. `TileMapSource2D` validates actual returned payloads against these promises. A supplied dependency list may conservatively include unused IDs/images, but may not omit an actual dependency. The acquired palette itself contains only definitions used by its cells. Unloaded data is not represented by a zero tile count: use an absent catalog entry for absent terrain, and loading failure for unavailable declared data.

### Data-residency charge

`DataResidencyBytes` is a nullable, nonnegative `long`. It declares a conservative byte charge for payload data whose residency depends on the acquisition: chunk data objects, cell/placement arrays, used definitions and collider prototypes, private data buffers and opaque property values retained through that data. The source is responsible for this declaration. Cell counts and compressed file sizes alone do not determine it. The source does not inspect arbitrary object graphs or measure process memory to verify the declared amount.

Null, the default, means **unknown**, not zero. Unknown data is not eligible for optional preloading under a byte budget. Zero is an explicit declaration that acquiring the payload adds no data residency, for example when an in-memory source already retains that entire immutable payload independently. Do not use zero to hide an unknown allocation. Negative values are rejected by both constructors. Values above a consumer's optional budget, including `long.MaxValue`, are valid metadata; they do not make the chunk invalid for required loading.

The charge excludes independently retained backing data, catalog metadata, graphical resources and consumer-side lease/index/batch bookkeeping. Those have separate owners/accounting; this property is not a cap on total managed, native or GPU memory. A shared payload is one data-residency cost, not a fresh copy for each interested region. Releasing a lease does not guarantee immediate garbage collection.

The declaration does not itself reserve bytes, start I/O or evict data. Direct `TileMapSource2D.LoadAsync` and required `SceneSpatialResidency2D` acquisitions remain available for unknown and oversized declarations. The `TileMap2D` control includes optional source-owned data charges in its bounded warm working set; required camera, simulation and explicit collision preparation is not cut to fit that optional budget.

The optional [package writer](Cerneala.Scene2D.Packages.Scene2DPackageWriter.md#decoded-chunk-data-charges)
declares this charge from its explicit decoder ownership model. Earlier package
headers with a null charge remain unknown until regenerated. In contrast,
`TileMapSource2D.FromModel` retains its complete in-memory backing payloads
independently of acquisitions and therefore declares zero additional data
residency; that does not make the backing model unloadable.

## Constructors

| Name | Description |
| --- | --- |
| `TileMapChunkInfo2D(SceneSpatialEntry2D spatial, TileMapBounds2D cells, IEnumerable<int> tileIds, IEnumerable<ImageReference> images, int expandedColliderCount = 0, long? dataResidencyBytes = null)` | Declares grid cells and image dependencies; tile count is width times height. The default data charge is unknown. |
| `TileMapChunkInfo2D(SceneSpatialEntry2D spatial, int tileCount, IEnumerable<ImageReference> images, int expandedColliderCount = 0, long? dataResidencyBytes = null)` | Declares ordered free placements without their payload. The default data charge is unknown. |

## Properties

| Name | Description |
| --- | --- |
| `Spatial` | Identity, revision and map-local envelopes. |
| `Cells` | Cell-coordinate rectangle for a grid chunk, or null for free placements. |
| `TileCount` | Exact positive cell or placement count, including empty grid cells. |
| `ExpandedColliderCount` | Exact descriptor instance count before coalescing. |
| `DataResidencyBytes` | Nullable source-declared conservative data charge in bytes; unknown by default, nonnegative when supplied. Not a measured process-memory limit. |
| `TileIds` | Unique positive declared grid dependencies; empty for free placements. |
| `Images` | Unique declared image dependencies for either representation. |

## See also

- [SceneSpatialEntry2D](Cerneala.UI.Controls.SceneSpatialEntry2D.md)
- [TileMapCatalog2D](Cerneala.UI.Controls.TileMapCatalog2D.md)
- [TileMapChunkData2D](Cerneala.UI.Controls.TileMapChunkData2D.md)
- [TileMapSource2D](Cerneala.UI.Controls.TileMapSource2D.md)
