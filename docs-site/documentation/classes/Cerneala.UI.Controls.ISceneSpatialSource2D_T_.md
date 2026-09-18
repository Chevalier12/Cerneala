# ISceneSpatialSource2D&lt;T&gt; Interface

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/SceneSpatialSource2D.cs`

Separates spatial metadata from asynchronous acquisition of reference-type payloads.

```csharp
public interface ISceneSpatialSource2D<T> where T : class
```

## Remarks

`Entries` is an immutable catalog snapshot. Keep the same snapshot instance until publishing a replacement; never mutate a published snapshot. Each entry has a unique ordinal ID, conservative source-coordinate bounds, simulation participation, and a positive payload revision. Enumerating the catalog must not materialize payloads.

Publish a replacement catalog before raising `Changed`. Existing snapshots and acquisitions remain usable under their existing lifetimes. Consumers use snapshot identity to detect obsolete region selections, and `(Id, Version)` to share payload acquisitions. Reuse a revision only for the same payload; moving its bounds does not by itself require a new payload revision.

`LoadAsync` returns a non-null, live lease. Cancellation, missing data, invalid data, and I/O errors must remain explicit failures, not successful empty payloads. Implementations should honor cancellation, although residency also handles late completion by releasing an unwanted lease. A source remains responsible for its backing store and any application state that must survive an acquisition's release.

The interface does not prescribe a UI thread. Residency can invoke loaders concurrently and continuations can run on worker threads. Marshal changes to attached scene nodes through the UI owner rather than performing them in a loader. `Changed` subscribers must account for the source's publishing thread.

This contract does not itself virtualize `SceneItems2D`, stream a `TileMap2DModel`, decode an image, or prepare a collision world.

## Properties

| Name | Description |
| --- | --- |
| `Entries` | The current immutable, metadata-only `IReadOnlyList<SceneSpatialEntry2D>` snapshot. |

## Methods

| Name | Description |
| --- | --- |
| `LoadAsync(SceneSpatialEntry2D entry, CancellationToken cancellationToken = default)` | Acquires the requested payload as `ValueTask<SceneSpatialLease2D<T>>`. |

## Events

| Name | Description |
| --- | --- |
| `Changed` | Notifies consumers that a replacement catalog has been published. |

## See also

- [SceneSpatialSource2D&lt;T&gt;](Cerneala.UI.Controls.SceneSpatialSource2D_T_.md)
- [SceneSpatialResidency2D&lt;T&gt;](Cerneala.UI.Controls.SceneSpatialResidency2D_T_.md)
