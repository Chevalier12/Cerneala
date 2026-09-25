# Scene2DPackageLevel Class

## Definition

Namespace: `Cerneala.Scene2D.Packages`

Assembly/Project: `Cerneala.Scene2D.Packages`

Source: `Cerneala.Scene2D.Packages/Scene2DPackageLevel.cs`

Exposes a resident level header, package-backed map factories and explicit authoring-data reads.

```csharp
public sealed class Scene2DPackageLevel
```

Instances come from [Scene2DPackage.Levels](Cerneala.Scene2D.Packages.Scene2DPackage.md).

## Examples

```csharp
Scene2DPackageLevel level = package.Levels[0];
string mapId = level.TileMapIds[0];
Scene2DPackageMetadata metadata = await level.LoadMapMetadataAsync(mapId);
TileMap2D map = level.CreateTileMap(mapId);
scene.Children.Add(map);
```

Keep the package alive while `map` can request chunks. Remove the node from its scene and await `map.DisposeAsync()` before disposing the package when shutting down completely. `LoadMapModelAsync(mapId)` is a separate full-map read for application-owned editing, not the progressive rendering path.

Load an entity value by its resident header's ID:

```csharp
Scene2DPackageEntityInfo header = level.Entities[0];
Scene2DEntity entity = await level.LoadEntityAsync(header.Id);
```

The application chooses which IDs to load and which values to put in a [SceneItems2D](Cerneala.UI.Controls.SceneItems2D.md) collection. No package method chooses a template, spawns an actor or applies `WorldOffset`.

## Remarks

The header contains neither whole models nor opaque properties. `TileMapIds` is
the ordered map identity index. Each `CreateTileMap(mapId)` call creates a new
detached `TileMap2D` with per-instance chunk residency and warm cache backed by
the package's private prepared index. The map loads required chunks progressively;
it does not first materialize every tile from `TileMapIds`. Loaded chunks are
validated against their declared geometry, dependencies and revision.

Package data is immutable, not a write-back API. `LoadMapModelAsync(mapId)` reads
and validates **all** of a map's chunks, reassembles prepared grid pieces into
authored chunk extents, and returns a complete `TileMap2DModel` for editing in
application code. Its I/O and memory cost scale with the whole map. Older CPV2
grid package with prepared chunks but no authored extents cannot reconstruct a complete model;
regenerate that package before using this editing path. To publish an edit,
construct replacement immutable model data, call `TileMap2D.FromModel` and
replace the scene's map node. Supply image dimensions from `package.Assets` when
the model has free placements with natural-size axes. The replacement has a new
node/cache identity while the authored map ID is preserved; it may lose the old
map's warm residency. This is not incremental chunk publication.

`WorldOffset` remains separate from map-local map offsets; reading does not
create a scene node or apply the level translation. Composition owns that transform.

`LoadEntityAsync` and `LoadPromotionAsync` return individual authoring records. They
do not instantiate templates, spawn NPCs, perform promotions, activate simulation,
or apply collision adapters. In particular, an imported `Spawn` role is not an
automatic simulation policy. `EntityIds` is the ordered identity index;
`Entities` additionally exposes small resident geometry/role headers.

The application selects headers, loads the desired values by ID and owns its
ordinary scene-items collection. The package does not automatically choose
visual/collision envelopes, retain an entity before load, or infer simulation
from an imported `Spawn` role. Mark each already-realized actor collider with
`Collider2D.IsSimulated` when its own active geometry must keep nearby terrain
resident. Use an explicit `SceneCollisionRegion2D` for collision interest before
a lazy custom actor has been loaded. This is narrower than the removed
pre-load entity-selection contract.

Nonspatial map and level metadata is separate from chunk acquisition. It includes
all original source definitions, including unused ones, so obtaining a chunk
does not silently lose authoring data or force it all into memory. Access is
an explicit asynchronous value read, never hidden I/O in a `Properties` getter.

New grid packages also preserve original chunk extents, revisions and properties
in map metadata's `GridChunks`, without cell arrays. Runtime grid pieces can have
different boundaries because preparation subdivides oversized chunks to at most
16 by 16 cells. This is not a runtime repartitioning step. Earlier metadata lacks
that provenance and decodes with an empty collection; see
[Scene2DPackageMetadata](Cerneala.Scene2D.Packages.Scene2DPackageMetadata.md).

Newly prepared chunk headers carry the writer's conservative decoded-data charge
in the package's private index, available without acquiring the payload.
The charge is not encoded file length or a physical-memory ceiling. Earlier CPV2
packages with null charges remain readable, but those unknown costs do not qualify
for optional warm loading. Required regions are not discarded to satisfy an
optional preparation budget. See [decoded chunk data charges](Cerneala.Scene2D.Packages.Scene2DPackageWriter.md#decoded-chunk-data-charges).

## Properties

| Name | Description |
| --- | --- |
| `Id` | Stable level identity. |
| `WorldOffset` | Authored level translation, not automatically applied. |
| `TileSize` | Optional source grid metadata. |
| `Bounds` | Optional finite source cell extent. |
| `TileMapIds` | Ordered map IDs, unique within this level. |
| `EntityIds` | Ordered identities of separately stored entity records. |
| `Entities` | Ordered resident `Scene2DPackageEntityInfo` headers, without entity payloads. |
| `PromotionCells` | Ordered sparse addresses of separately stored promotions. |

## Methods

| Name | Description |
| --- | --- |
| `CreateTileMap(mapId)` | Creates a detached, package-backed `TileMap2D` with instance-owned residency. |
| `LoadMetadataAsync(cancellationToken = default)` | Returns level properties and source tilesets directly. |
| `LoadMapMetadataAsync(mapId, cancellationToken = default)` | Returns map properties, its complete authoring palette and original grid chunk metadata directly. |
| `LoadMapModelAsync(mapId, cancellationToken = default)` | Reads and reconstructs the complete authored map as a `TileMap2DModel`. |
| `LoadEntityAsync(id, cancellationToken = default)` | Returns one entity after checking identity, owning map, role and authored/collider bounds. |
| `LoadPromotionAsync(cell, cancellationToken = default)` | Returns one promotion after checking its sparse address. |

Unknown map/entity/promotion keys throw `KeyNotFoundException`. The package's
read-limit, cancellation, integrity and disposal rules apply to these reads.
`CreateTileMap` also rejects a disposed package. Keep the package open while
returned maps can request more data.

## See also

- [Scene2DPackageMetadata](Cerneala.Scene2D.Packages.Scene2DPackageMetadata.md)
- [Scene2DPackageEntityInfo](Cerneala.Scene2D.Packages.Scene2DPackageEntityInfo.md)
- [TileMap2D](Cerneala.UI.Controls.TileMap2D.md)
- [TileMap2DModel](Cerneala.UI.Controls.TileMap2DModel.md)
- [SceneItems2D](Cerneala.UI.Controls.SceneItems2D.md)
