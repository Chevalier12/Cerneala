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

[CollisionWorld2D](Cerneala.UI.Controls.CollisionWorld2D.md) throws this exception instead of reporting missing terrain as empty. `Bounds` describes the query rectangle in scene coordinates. `EntryId` identifies the first unavailable entry encountered, not a globally unique ID across all sources or a list of every missing entry.

An unavailable entry may be absent, still loading, or realized with an obsolete source/template/payload revision. A newly published worker catalog whose UI notification has not yet been applied can also make affected retained geometry unavailable. Explicitly prepare the required region before issuing the current query and keep the root relay/frame loop running. The exception does not load resources, move the actor, synthesize a collision, drain the relay, or schedule old input for replay. Preparation can separately propagate loader failures or cancellation; not every preparation error is converted into this exception.

A spatially resident `TileMap2D` grid can also report a chunk whose adapters are not currently retained. Its diagnostic entry identity includes the model ID, chunk origin, and dimensions; it is not a public per-cell entity or a promise of global uniqueness.

## Constructors

| Name | Description |
| --- | --- |
| `SceneCollisionRegionNotReadyException(DrawRect bounds, string entryId)` | Stores a finite rectangle with nonnegative dimensions and a nonempty, non-whitespace entry ID. Invalid bounds throw `ArgumentOutOfRangeException`; invalid IDs throw an argument exception. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Bounds` | `DrawRect` | Collision query's scene-space rectangle. |
| `EntryId` | `string` | Source-local ordinal identity of an unavailable entry. |

## See also

- [CollisionWorld2D](Cerneala.UI.Controls.CollisionWorld2D.md)
- [SceneCollisionRegion2D](Cerneala.UI.Controls.SceneCollisionRegion2D.md)
