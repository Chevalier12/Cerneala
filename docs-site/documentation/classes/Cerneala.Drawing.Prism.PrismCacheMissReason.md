# PrismCacheMissReason Enum

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/PrismRendererOptions.cs`

Specifies why a Prism retained-cache candidate was not reused.

```csharp
public enum PrismCacheMissReason
```

## Values

| Name | Description |
| --- | --- |
| `None` | No miss reason has been recorded. |
| `NotFound` | No retained entry matched the complete candidate key. |
| `NotCacheable` | The graph or one of its dependencies did not provide a stable cacheable result. |
| `DependencyChanged` | A pixel-affecting dependency or raster context changed from the retained result. |
| `Invalidated` | The candidate's retained entries were invalidated before lookup. |
| `Disabled` | Reserved for a retained lookup that was bypassed. No current code produces this value. |

## Remarks

The value descriptions name the intended meaning. No current code records a
miss reason: `PrismRendererDiagnostics` has no public constructor and the
renderer never creates it, so `PrismRendererDiagnostics.GetMissCount` and
`PrismRendererDiagnostics.LastMissReason` have no populated instance to
report from.

## Applies to

Cerneala Prism retained-cache diagnostics.

## See also

- `Cerneala.UI.Detective.PrismRendererDiagnostics`
- `Cerneala.Drawing.Prism.PrismDependencyChange`
