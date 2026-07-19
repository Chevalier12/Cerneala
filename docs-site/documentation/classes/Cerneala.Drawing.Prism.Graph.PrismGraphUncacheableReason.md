# PrismGraphUncacheableReason Enum

## Definition
Namespace: `Cerneala.Drawing.Prism.Graph`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/Graph/PrismGraphOptimizer.cs`

Identifies one or more reasons an optimized Prism node cannot be retained in a
cross-frame pixel cache.

```csharp
[Flags]
public enum PrismGraphUncacheableReason
```

## Examples

```csharp
using Cerneala.Drawing.Prism.Graph;

static bool DependsOnUncacheableInput(PrismGraphNodePlan node) =>
    node.UncacheableReasons.HasFlag(
        PrismGraphUncacheableReason.UncacheableInput);
```

## Remarks

Values can be combined. Reasons owned by a node are propagated to downstream
plans; `UncacheableInput` records that upstream provenance rather than replacing
the original flags.

## Fields

| Name | Description |
| --- | --- |
| `None` | No uncacheable condition was found. |
| `NonDeterministicOperation` | The catalog marks the operation as non-deterministic. |
| `CatalogDisallowsCaching` | The catalog explicitly disables caching for the operation. |
| `ResourceVersionUnavailable` | A resource affects pixels but no version can participate in the dependency stamp. |
| `FrameBackdrop` | The node depends on the current frame backdrop. |
| `MissingRequiredDependency` | A required pixel-affecting dependency is absent. |
| `UncacheableInput` | At least one upstream input is not cacheable. |

## Applies to

Cerneala retained Prism cache eligibility diagnostics.

## See also

- `Cerneala.Drawing.Prism.Graph.PrismGraphNodePlan`
- `Cerneala.Drawing.Prism.Graph.PrismGraphExecutionPlan`
- `Cerneala.Drawing.Prism.Graph.PrismGraphDependency`
