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
| `Disabled` | Retained lookup was bypassed by the internal cache-off conformance mode. |

## Remarks

Reasons are counted per cache candidate. Use
`PrismRendererDiagnostics.GetMissCount` for cumulative counts and
`PrismRendererDiagnostics.LastMissReason` for the most recently recorded
reason. `Disabled` is diagnostic output only; public renderer options do not
expose the internal cache-off mode.

## Applies to

Cerneala Prism retained-cache diagnostics.

## See also

- `Cerneala.Drawing.Prism.PrismRendererDiagnostics`
- `Cerneala.Drawing.Prism.PrismDependencyChange`
