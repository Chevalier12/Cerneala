# SceneSpatialResidency2D&lt;T&gt; Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/SceneSpatialResidency2D.cs`

Shares asynchronous spatial payload acquisitions between prepared regions and releases each acquisition when its last region releases it.

```csharp
public sealed class SceneSpatialResidency2D<T> : IDisposable, IAsyncDisposable
    where T : class
```

## Examples

```csharp
var source = new SceneSpatialSource2D<string>(
    new[] { new SceneSpatialEntry2D("npc", new DrawRect(2000, 0, 32, 32), true) },
    static (entry, _) => ValueTask.FromResult(new SceneSpatialLease2D<string>(entry.Id)));

await using var residency = new SceneSpatialResidency2D<string>(source);
using var region = await residency.AcquireAsync(
    new DrawRect(0, 0, 640, 360), includeSimulated: true);
string npc = region.GetValue("npc");
```

## Remarks

An acquisition selects catalog entries whose bounds intersect the requested rectangle, including touching edges. With `includeSimulated: true`, every simulated entry is also selected regardless of location. Null bounds mean no rectangular interest, not the entire world; they can be used to acquire only the simulation set. Bounds are validated using the spatial entry contract.

Each request uses one catalog snapshot and preserves its order. Overlapping regions share one source acquisition per `(Id, Version)`. Acquire the next region before disposing the previous one to keep shared payload identity alive across transitions. There is no idle payload cache: the last release removes the resident acquisition. An application or source that separately retains the same payload can still keep it in memory.

The method completes only when every selected payload has loaded. It does not return partial success or treat unavailable data as empty space. On failure it releases that request's interests, including successful parts. A later request can retry once the failed acquisition is no longer shared. Cancellation of one request does not cancel another request's shared acquisition. When the last interest disappears, the loader is cancelled; a loader that ignores cancellation has its eventual lease released instead of published into a newer request.

Catalog replacement does not revoke existing regions. Check `SceneSpatialRegion2D<T>.IsCurrent` before publishing a prepared snapshot into a live consumer. This class does not subscribe to `Changed`, refresh regions automatically, dispatch node changes, draw objects, advance simulation, or register collision geometry. Region acquisition currently scans catalog metadata; no steady-frame or indexed-query performance guarantee is made.

The default concurrency cap is four active loaders per residency instance. It is a limit, not a measured optimal setting. Synchronously completing callbacks may execute on the requesting thread; queued loads and asynchronous continuations may execute on worker threads. The source is borrowed and is not disposed by residency.

`Dispose` invalidates outstanding regions, releases resident acquisitions, and requests cancellation without waiting for uncooperative I/O. `DisposeAsync` additionally waits for pending loads and releases; it reports failures from release callbacks of late arrivals as an `AggregateException`. An uncooperative loader that never completes can therefore keep asynchronous disposal pending. Prefer asynchronous disposal when the caller must observe complete I/O teardown. Release failures do not prevent attempts to release the other acquisitions.

## Constructors

| Name | Description |
| --- | --- |
| `SceneSpatialResidency2D(ISceneSpatialSource2D<T> source, int maximumConcurrentLoads = 4)` | Borrows a source and requires a positive concurrency cap. |

## Properties

| Name | Description |
| --- | --- |
| `ResidentCount` | Number of live, published payload acquisitions, not the number of regions. |
| `PendingLoadCount` | Number of queued or unfinished loads, including cancelled loads that have not finished. |

## Methods

| Name | Description |
| --- | --- |
| `AcquireAsync(DrawRect? bounds, bool includeSimulated = false, CancellationToken cancellationToken = default)` | Returns a prepared `SceneSpatialRegion2D<T>` that pins all selected payloads. |
| `Dispose()` | Invalidates regions and initiates teardown without awaiting late I/O. |
| `DisposeAsync()` | Initiates teardown, awaits outstanding operations, and reports late release failures. |
