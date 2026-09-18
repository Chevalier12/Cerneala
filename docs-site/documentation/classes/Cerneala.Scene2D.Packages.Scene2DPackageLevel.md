# Scene2DPackageLevel Class

## Definition

Namespace: `Cerneala.Scene2D.Packages`

Assembly/Project: `Cerneala.Scene2D.Packages`

Source: `Cerneala.Scene2D.Packages/Scene2DPackageLevel.cs`

Exposes a resident level header, package-backed tile sources and explicit authoring-data acquisitions.

```csharp
public sealed class Scene2DPackageLevel
```

Instances come from [Scene2DPackage.Levels](Cerneala.Scene2D.Packages.Scene2DPackage.md).

## Examples

```csharp
Scene2DPackageLevel level = package.Levels[0];
TileMapSource2D source = level.TileMaps[0];
using var metadata = await level.LoadMapMetadataAsync(source.Catalog.Id);
IReadOnlyDictionary<string, object?> properties = metadata.Value.Properties;
```

In this game's map-local coordinates, the entity named `npc` is an unrotated
Point marker. The game chooses an 8-by-12 collider/template and a larger motion
envelope; these sizes are not inferred from the marker:

```csharp
string mapId = level.TileMaps[0].Catalog.Id;
SceneSpatialSource2D<object> entities = level.CreateEntitySource(info =>
{
    if (info.MapId != mapId || info.Id != "npc")
        return null;
    DrawRect envelope = new(info.AuthoringBounds.X - 8, info.AuthoringBounds.Y - 8, 64, 32);
    return new SceneSpatialEntry2D(info.Id, envelope, envelope, isSimulated: true);
});

SceneItems2D items = new() { ItemsSource = entities };
items.Templates.Add(new ContentTemplate<Scene2DEntity>("npc", null, 0, context =>
{
    Scene2DEntity entity = context.Data ?? throw new InvalidOperationException("Missing entity.");
    return new Sprite2D
    {
        X = entity.Position.X, Y = entity.Position.Y,
        Width = 8, Height = 12,
        Collider = new BoxCollider2D { Width = 8, Height = 12 }
    };
}));
```

`ContentTemplate<T>` is in `Cerneala.UI.Controls.Templates`. This example creates
the source/materializer, not a clock or a rendered game. The game keeps movement
within the declared envelope or publishes updated envelopes before moving beyond
them. A containing scene applies any desired map/level translation.

## Remarks

The header contains neither whole models nor opaque properties. Acquiring a
chunk returns its cells or placements and complete **used** tile palette,
including collider prototypes and their properties. Source validation checks
that the acquired data matches the declared geometry, dependencies and revision.

Package tile sources are immutable snapshots. Their loaders reject replacement
catalogs; they are not a write-back API. For gameplay mutations, publish an
application-owned source with the affected payload revisions instead of editing
the package or reinterpreting old payloads under a new catalog.

`WorldOffset` remains separate from map-local `Catalog.Offset`; reading does not
create a scene node or apply the level translation. Composition owns that transform.

`LoadEntityAsync` and `LoadPromotionAsync` read individual authoring records. They
do not instantiate templates, spawn NPCs, perform promotions, activate simulation,
or apply collision adapters. In particular, an imported `Spawn` role is not an
automatic simulation policy. `EntityIds` is the ordered identity index;
`Entities` additionally exposes small resident geometry/role headers.

`CreateEntitySource(describe)` synchronously calls the game-supplied callback once
per header in catalog order, with no payload reads or template creation. Returning
`null` excludes that record. Returning an entry explicitly declares the visual
envelope, collision envelope (or `null`) and simulation participation. It must
preserve the header identity and payload version **1**. The returned ordinary
`SceneSpatialSource2D<object>` acquires immutable `Scene2DEntity` values through the
same validated entity loader. It adds no second payload cache; SceneItems2D and
scene residency own the active acquisitions. Disposing the outer source lease
also disposes the underlying entity acquisition.

Runtime `SetEntries` may change selection, ordering, envelopes and `IsSimulated`
without regenerating a package, including selecting a previously excluded known
entity. Such metadata changes do not rewrite the stored entity's position or
properties. A new identity or payload revision cannot be invented for this loader:
loading an unknown ID or a version other than 1 fails. Changed entity payload data
requires a new prepared package or an application-owned source. Removal/revision
retirement and stable active-node identity follow the normal SceneItems2D contract;
this adapter does not persist state for a static node after it is retired.

Nonspatial map and level metadata is separate from chunk acquisition. It includes
all original source definitions, including unused ones, so obtaining a chunk
does not silently lose authoring data or force it all into memory. Access is
explicit through a lease, never hidden I/O in a `Properties` getter.

New grid packages also preserve original chunk extents, revisions and properties
in map metadata's `GridChunks`, without cell arrays. Runtime grid pieces can have
different boundaries because preparation subdivides oversized chunks to at most
16 by 16 cells. This is not a runtime repartitioning step. Earlier metadata lacks
that provenance and decodes with an empty collection; see
[Scene2DPackageMetadata](Cerneala.Scene2D.Packages.Scene2DPackageMetadata.md).

Newly prepared chunk headers carry the writer's conservative decoded-data charge
in `TileMapChunkInfo2D.DataResidencyBytes`, available without acquiring the payload.
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
| `TileMaps` | Ordered package-backed `TileMapSource2D` instances. |
| `EntityIds` | Ordered identities of separately stored entity records. |
| `Entities` | Ordered resident `Scene2DPackageEntityInfo` headers, without entity payloads. |
| `PromotionCells` | Ordered sparse addresses of separately stored promotions. |

## Methods

| Name | Description |
| --- | --- |
| `LoadMetadataAsync(cancellationToken = default)` | Acquires level properties and source tilesets. |
| `LoadMapMetadataAsync(mapId, cancellationToken = default)` | Acquires map properties, its complete authoring palette and stored original grid chunk metadata. |
| `LoadEntityAsync(id, cancellationToken = default)` | Acquires one entity and checks its identity, owning map, role and authored/collider bounds. |
| `CreateEntitySource(describe)` | Adapts the resident headers to explicit game-owned spatial/simulation entries; callback may return `null` to exclude a record. |
| `LoadPromotionAsync(cell, cancellationToken = default)` | Acquires one promotion and checks its sparse address. |

Unknown map/entity/promotion keys throw `KeyNotFoundException`. The package's
read-limit, cancellation, integrity and disposal rules apply to all acquisitions.
`CreateEntitySource` rejects a null callback with `ArgumentNullException` and an
identity/revision-changing result with `ArgumentException`; callback exceptions
propagate. Unsupported IDs/revisions published later fail with
`InvalidOperationException` when loaded, rather than aliasing another payload.

## See also

- [Scene2DPackageMetadata](Cerneala.Scene2D.Packages.Scene2DPackageMetadata.md)
- [Scene2DPackageEntityInfo](Cerneala.Scene2D.Packages.Scene2DPackageEntityInfo.md)
- [SceneSpatialSource2D](Cerneala.UI.Controls.SceneSpatialSource2D_T_.md)
- [TileMapSource2D](Cerneala.UI.Controls.TileMapSource2D.md)
- [SceneItems2D](Cerneala.UI.Controls.SceneItems2D.md)
