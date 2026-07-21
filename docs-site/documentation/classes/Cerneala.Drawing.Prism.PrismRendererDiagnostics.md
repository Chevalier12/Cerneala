# PrismRendererDiagnostics Struct

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/PrismRendererOptions.cs`

Provides an immutable snapshot of Prism retained-cache work and GPU-surface
usage.

```csharp
public readonly struct PrismRendererDiagnostics
```

## Examples

```csharp
using Cerneala.Drawing.Prism;

PrismRendererDiagnostics diagnostics = host.PrismDiagnostics;

Console.WriteLine($"Final hits: {diagnostics.FinalHitCount}");
Console.WriteLine($"Retained bytes: {diagnostics.RetainedByteCount}");
Console.WriteLine(
    $"Capacity evictions: " +
    $"{diagnostics.GetEvictionCount(PrismCacheEvictionReason.Capacity)}");
```

## Remarks

Hit, miss, lookup, promotion, rejection, eviction, peak-byte, and saved-work
counters are cumulative for the lifetime of the underlying Prism executor.
Entry, pinned-entry, and current-byte properties describe the instant at which
the snapshot was obtained. A miss is counted per eligible cache candidate, not
per application frame.

`SavedCaptureCount` and `SavedPassCount` count work pruned by final or
intermediate hits. `LastMissReason` and `LastEvictionReason` identify the most
recent recorded event. Reason-specific methods return cumulative counts and
return zero for the `None` value.

`LastDependencyChange` describes the most recently prepared frame only when
`PrismRendererOptions.EnableDevelopmentDiagnostics` is enabled. Otherwise it
remains `PrismDependencyChange.None` and the renderer skips dependency-diff
work.

The snapshot owns no texture, render target, UI element, delegate, or lease.
Reading it does not pin cache entries or extend GPU resource lifetimes.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `RetainedCacheEnabled` | `bool` | Gets whether the executor that produced the snapshot has retained-cache lookup and promotion enabled. |
| `FinalHitCount` | `long` | Gets the cumulative number of final-composition hits. |
| `IntermediateHitCount` | `long` | Gets the cumulative number of intermediate-node hits. |
| `MissCount` | `long` | Gets the cumulative number of missed cache candidates. |
| `LastMissReason` | `PrismCacheMissReason` | Gets the reason recorded for the most recent miss. |
| `LookupCount` | `long` | Gets the cumulative number of retained-cache lookups. |
| `PromotionCount` | `long` | Gets the cumulative number of successful transient-to-retained promotions. |
| `RejectedPromotionCount` | `long` | Gets the cumulative number of promotions rejected by budget, pinning, or allocation constraints. |
| `EvictionCount` | `long` | Gets the cumulative number of retained entries evicted or removed. |
| `LastEvictionReason` | `PrismCacheEvictionReason` | Gets the reason for the most recent eviction. |
| `RetainedEntryCount` | `int` | Gets the current retained entry count. |
| `PinnedEntryCount` | `int` | Gets the current number of entries protected by active draw leases. |
| `TransientByteCount` | `long` | Gets the bytes currently owned by transient Prism surfaces. |
| `RetainedByteCount` | `long` | Gets the bytes currently owned by retained Prism surfaces. |
| `TotalByteCount` | `long` | Gets the current combined transient and retained byte count. |
| `PeakTotalByteCount` | `long` | Gets the lifetime peak combined byte count. |
| `SavedCaptureCount` | `long` | Gets the cumulative control captures skipped by cache hits. |
| `SavedPassCount` | `long` | Gets the cumulative graph passes skipped by cache hits. |
| `LastDependencyChange` | `PrismDependencyChange` | Gets the dependency categories changed in the most recently prepared frame when development diagnostics are enabled. |

## Methods

| Name | Return type | Description |
| --- | --- | --- |
| `GetMissCount(PrismCacheMissReason reason)` | `long` | Returns the cumulative miss count for one known reason. `None` returns zero. |
| `GetEvictionCount(PrismCacheEvictionReason reason)` | `long` | Returns the cumulative eviction count for one known reason. `None` returns zero. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `GetMissCount` | `ArgumentOutOfRangeException` | `reason` is not a defined `PrismCacheMissReason` value. |
| `GetEvictionCount` | `ArgumentOutOfRangeException` | `reason` is not a defined `PrismCacheEvictionReason` value. |

## Applies to

Cerneala MonoGame Prism rendering and UI hosting.

## See also

- `Cerneala.Drawing.Prism.PrismRendererOptions`
- `Cerneala.Drawing.Prism.PrismCacheMissReason`
- `Cerneala.Drawing.Prism.PrismCacheEvictionReason`
- `Cerneala.Drawing.Prism.PrismDependencyChange`
- `Cerneala.Drawing.MonoGame.MonoGameDrawingBackend`
