# SceneSpatialEntry2D Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/SceneSpatialSource2D.cs`

Describes the identity, conservative bounds, simulation participation, and payload revision of an entry without creating its payload.

```csharp
public sealed class SceneSpatialEntry2D
```

## Examples

```csharp
var npc = new SceneSpatialEntry2D(
    "npc-17", new DrawRect(120, 80, 32, 48), isSimulated: true);
```

## Remarks

IDs are nonempty, case-sensitive strings. Distinct occurrences require distinct IDs even when their data compares equal. Bounds require finite coordinates, dimensions, and endpoints. Negative coordinates and zero-area bounds are permitted; negative dimensions and overflowing endpoints are rejected. Versions must be positive.

Bounds are metadata supplied by the source, not measured from a template or image. A provider must publish new metadata when its spatial description changes. Changing the bounds without changing the version describes the same payload at a new location. Change the version when a new acquisition must represent different payload data.

For [SceneItems2D](Cerneala.UI.Controls.SceneItems2D.md), bounds are in the materializer's local coordinate space and must include the templated subtree's conservative visual influence, including its own effects. A moving simulated payload must publish current bounds or use an envelope covering its motion. Stable IDs and payload versions preserve realized node identity across catalog reordering; template contexts do not receive positional indices.

`IsSimulated` allows an interested consumer to retain an entry outside the requested rectangle. It does not start a simulation, register a collider, update a node position, or disable drawing by itself.

`CollisionBounds` is a separate conservative envelope for the payload's collision geometry in the same local coordinate space. It can extend beyond visual `Bounds`. The original constructor defaults it to `Bounds`; pass explicit null only when the payload contains no collision data. This is a provider promise, not something the framework can validate before loading the payload. Non-null collision bounds obey the same finite/nonnegative validation as visual bounds. A zero-area rectangle is valid and is not equivalent to null.

SceneItems preparation selects collision-interest intersections as well as visual and simulated entries. Simulated entries contribute their published collision envelopes to the root world's terrain interest. Publish current envelopes or a conservative motion envelope; this does not predict an arbitrary future cast or automatically move template nodes. Call [CollisionWorld2D.PrepareRegionAsync](Cerneala.UI.Controls.CollisionWorld2D.md) for unavailable terrain needed by a query or teleport.

## Constructors

| Name | Description |
| --- | --- |
| `SceneSpatialEntry2D(string id, DrawRect bounds, bool isSimulated = false, long version = 1)` | Validates metadata and defaults collision bounds to the visual bounds. |
| `SceneSpatialEntry2D(string id, DrawRect bounds, DrawRect? collisionBounds, bool isSimulated = false, long version = 1)` | Stores independently declared collision bounds; null means no collision data. |

## Properties

| Name | Description |
| --- | --- |
| `Id` | Stable ordinal identity within a source. |
| `Bounds` | Source-coordinate conservative visual rectangle. |
| `CollisionBounds` | Source-coordinate conservative collision rectangle, or null for no collision data. |
| `IsSimulated` | Whether a simulation-inclusive request selects this entry independently of its rectangle. |
| `Version` | Positive payload revision. |

## See also

- [ISceneSpatialSource2D&lt;T&gt;](Cerneala.UI.Controls.ISceneSpatialSource2D_T_.md)
- [SceneSpatialResidency2D&lt;T&gt;](Cerneala.UI.Controls.SceneSpatialResidency2D_T_.md)
