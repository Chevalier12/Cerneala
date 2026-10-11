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

The value descriptions name the intended meaning. No current code records an
eviction reason: `PrismRendererDiagnostics` has no public constructor and the
renderer never creates it, so `PrismRendererDiagnostics.GetEvictionCount` has no
populated instance to report from. In particular, nothing produces
`InvalidSurface`, `DeviceReset`, or `ExplicitRemoval`.

In the SDL_GPU retained cache, an entry is pinned while a draw lease or a
submitted command buffer uses it. Budget eviction skips pinned entries; it does
not queue them for later removal. An invalidated pinned entry is marked
invalid and released when both its draw-lease count and its command-buffer
count reach zero.

## Applies to

Cerneala Prism retained-cache diagnostics.

## See also

- `Cerneala.UI.Detective.PrismRendererDiagnostics`
- `Cerneala.Drawing.Prism.PrismRendererOptions`
