# PrismCacheEvictionReason Enum

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/PrismRendererOptions.cs`

Specifies why a retained Prism GPU surface left the cache.

```csharp
public enum PrismCacheEvictionReason
```

## Values

| Name | Description |
| --- | --- |
| `None` | No eviction reason has been recorded. |
| `Capacity` | The retained byte or entry budget required deterministic LRU eviction. |
| `Invalidation` | Dependency, owner, lifecycle, or raster invalidation removed the entry. |
| `TransientPressure` | Transient frame work reclaimed retained memory before allocation failure. |
| `Replacement` | A newly completed result replaced an unpinned entry with the same key. |
| `InvalidSurface` | The cached GPU surface was already invalid or disposed. |
| `DeviceReset` | Graphics-device reset or loss invalidated the surface. |
| `Disposal` | Cache or backend disposal released the entry. |
| `ExplicitRemoval` | An explicit cache removal or clear operation released the entry. |

## Remarks

Pinned entries cannot be disposed until their final draw lease is released. If
an eviction is requested while an entry is pinned, the cache preserves the
original reason and records it when deferred removal completes. Use
`PrismRendererDiagnostics.GetEvictionCount` for cumulative reason counts.

## Applies to

Cerneala Prism retained-cache diagnostics.

## See also

- `Cerneala.Drawing.Prism.PrismRendererDiagnostics`
- `Cerneala.Drawing.Prism.PrismRendererOptions`
