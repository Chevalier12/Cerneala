# SceneCollisionRegionNotReadyException Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/CollisionWorld2D.Streaming.cs`

Reports a synchronous collision query whose region contains unavailable spatial collision data.

```csharp
public sealed class SceneCollisionRegionNotReadyException : InvalidOperationException
```

## Remarks

[CollisionWorld2D](Cerneala.UI.Controls.CollisionWorld2D.md) throws this exception instead of reporting unprepared `TileMap2D` terrain as empty, whether the map wraps a complete model or a prepared package. `Bounds` describes the query rectangle in scene coordinates. `EntryId` identifies the first unavailable private chunk entry encountered, local to its map source; it is not globally unique or a list of every missing entry. Plain [SceneItems2D](Cerneala.UI.Controls.SceneItems2D.md) collections do not fabricate entry IDs.

A required chunk may be absent, still loading, or obsolete for the map's current private index. Explicitly prepare the required region before issuing the current query and keep the root relay/frame loop running. The exception does not load resources, move the actor, synthesize a collision, drain the relay, or schedule old input for replay. Preparation can separately propagate loader failures, collection-publication failures or cancellation; not every preparation error is converted into this exception.

A spatially resident `TileMap2D` grid can also report a chunk whose adapters are not currently retained. Its diagnostic entry identity includes the model ID, chunk origin, and dimensions; it is not a public per-cell entity or a promise of global uniqueness.

## Constructors

| Name | Description |
| --- | --- |
| `SceneCollisionRegionNotReadyException(DrawRect bounds, string entryId)` | Stores a finite rectangle with nonnegative dimensions and a nonempty, non-whitespace entry ID. Invalid bounds throw `ArgumentOutOfRangeException`; invalid IDs throw an argument exception. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Bounds` | `DrawRect` | Collision query's scene-space rectangle. |
| `EntryId` | `string` | Source-local identity of the first unavailable private chunk entry. |

## See also

- [CollisionWorld2D](Cerneala.UI.Controls.CollisionWorld2D.md)
- [SceneCollisionRegion2D](Cerneala.UI.Controls.SceneCollisionRegion2D.md)
