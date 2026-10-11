# PrismRendererDiagnostics Struct

## Definition

Namespace: `Cerneala.UI.Detective`

Assembly/Project: `Cerneala`

Source: `UI/Detective/PrismRendererDiagnostics.cs`

Provides an immutable snapshot of Prism retained-cache work and GPU-surface
usage.

```csharp
public readonly struct PrismRendererDiagnostics
```

## Examples

```csharp
using Cerneala.Drawing.Prism;
using Cerneala.UI.Detective;

static void PrintCacheSummary(PrismRendererDiagnostics diagnostics)
{
    Console.WriteLine($"Final hits: {diagnostics.FinalHitCount}");
    Console.WriteLine($"Retained bytes: {diagnostics.RetainedByteCount}");
    Console.WriteLine($"Capacity evictions: {diagnostics.GetEvictionCount(PrismCacheEvictionReason.Capacity)}");
}
```

## Remarks

No public Cerneala API currently returns a `PrismRendererDiagnostics` value, and its constructor is internal. No framework code creates one, including the SDL GPU backend. The only value application code can get is `default`:

```csharp
PrismRendererDiagnostics diagnostics = default;
Console.WriteLine(diagnostics.RetainedCacheEnabled);                                  // False
Console.WriteLine(diagnostics.FinalHitCount);                                         // 0
Console.WriteLine(diagnostics.GetMissCount(PrismCacheMissReason.DependencyChanged));  // 0
Console.WriteLine(diagnostics.LastDependencyChange);                                  // None
```

The member tables describe what each field is meant to hold. Treat them as the data shape, not as values the renderer reports today.

`LastDependencyChange` is never set to anything other than `PrismDependencyChange.None`. `PrismRendererOptions.EnableDevelopmentDiagnostics` is stored but not read by any renderer code, so turning it on changes nothing here.

`GetMissCount` and `GetEvictionCount` return zero for the `None` reason.

The snapshot type is backend-neutral and owns no texture, render target, UI element, delegate, or lease.

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

Cerneala Prism diagnostic snapshot values.

## See also

- `Cerneala.Drawing.Prism.PrismRendererOptions`
- `Cerneala.Drawing.Prism.PrismCacheMissReason`
- `Cerneala.Drawing.Prism.PrismCacheEvictionReason`
- `Cerneala.Drawing.Prism.PrismDependencyChange`
- `Cerneala.UI.Hosting.Sdl.SdlGpuApplicationBackend`
