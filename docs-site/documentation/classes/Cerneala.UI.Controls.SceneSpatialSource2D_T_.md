# SceneSpatialSource2D&lt;T&gt; Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/SceneSpatialSource2D.cs`

Adapts an application-provided loader to a validated spatial catalog.

```csharp
public sealed class SceneSpatialSource2D<T> : ISceneSpatialSource2D<T>
    where T : class
```

## Examples

```csharp
var source = new SceneSpatialSource2D<string>(
    new[] { new SceneSpatialEntry2D("region-a", new DrawRect(0, 0, 256, 256)) },
    static (entry, cancellation) =>
    {
        cancellation.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new SceneSpatialLease2D<string>(entry.Id));
    });
```

## Remarks

Construction and `SetEntries` copy the supplied entry sequence. Null entries and duplicate ordinal IDs are rejected before publishing the replacement. An invalid replacement leaves the old snapshot and subscriptions unchanged. `SetEntries` atomically publishes a new read-only catalog, then raises `Changed` synchronously on the calling thread. Previously returned snapshots do not change.

`LoadAsync` first checks cancellation and verifies that the requested ID and version exist in the current catalog. Missing or obsolete revisions throw `InvalidOperationException` without invoking the loader. The original requested descriptor is passed to the callback; bounds can change while retaining the same payload revision.

The callback supplies acquisition, cancellation, and release behavior. The adapter does not cache payloads, perform I/O on its own, dispatch work to the UI, or revoke an existing lease when the catalog changes. A load already in flight may finish against an older snapshot; its consumer must check whether that result is still wanted.

[SceneItems2D](Cerneala.UI.Controls.SceneItems2D.md) treats a catalog applied on its owning UI thread as immediately authoritative: deleted IDs and obsolete payload versions are retired without waiting for unrelated replacement loading. Unchanged IDs/versions preserve their realized nodes. This consumer policy does not revoke independent leases held by other callers. Worker notifications are marshaled through the root relay; the source adapter itself does not synchronously mutate an attached tree from a worker.

## Constructors

| Name | Description |
| --- | --- |
| `SceneSpatialSource2D(IEnumerable<SceneSpatialEntry2D> entries, Func<SceneSpatialEntry2D, CancellationToken, ValueTask<SceneSpatialLease2D<T>>> load)` | Copies and validates the initial catalog and stores the loader. |

## Properties

| Name | Description |
| --- | --- |
| `Entries` | Current immutable catalog snapshot. |

## Methods

| Name | Description |
| --- | --- |
| `SetEntries(IEnumerable<SceneSpatialEntry2D> entries)` | Validates and publishes a replacement snapshot before notifying subscribers. |
| `LoadAsync(SceneSpatialEntry2D entry, CancellationToken cancellationToken = default)` | Validates a current revision and delegates acquisition to the callback. |

## Events

| Name | Description |
| --- | --- |
| `Changed` | Raised synchronously after a successful catalog publication. |
