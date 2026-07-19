# PrismGraphNodePlan Struct

## Definition
Namespace: `Cerneala.Drawing.Prism.Graph`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/Graph/PrismGraphOptimizer.cs`

Describes cacheability and output bounds for one optimized Prism graph node.

```csharp
public readonly record struct PrismGraphNodePlan
```

## Examples

```csharp
using Cerneala.Drawing.Prism.Graph;

static IEnumerable<PrismGraphNodeId> CacheableNodes(
    PrismGraphExecutionPlan plan) =>
    plan.NodePlans
        .Where(node => node.IsCacheable)
        .Select(node => node.NodeId);
```

## Remarks

Instances are produced by `PrismGraphOptimizer`; the constructor is not public.
The default value has `BoundsStatus` set to `Unknown` and is not cacheable.

`CacheDependencies` is the deterministic transitive closure of captured
dependencies. It includes dependencies inherited through input nodes and
through proven no-op nodes that were removed by aliasing. The closure is
complete for a cache key only when `IsCacheable` is `true`.

`Bounds` uses transformed logical drawing coordinates. An `Unknown` status
means the rectangle is not safe for strict clipping or allocation;
`Conservative` is a safe overestimate.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `NodeId` | `PrismGraphNodeId` | Gets the optimized graph node identifier. |
| `Bounds` | `DrawRect` | Gets the planned output rectangle in transformed logical coordinates. |
| `BoundsStatus` | `PrismGraphBoundsStatus` | Gets the confidence and safety classification for `Bounds`. |
| `CacheDependencies` | `ImmutableArray<PrismGraphDependency>` | Gets the deterministic transitive closure of captured dependencies. |
| `UncacheableReasons` | `PrismGraphUncacheableReason` | Gets flags explaining why the node must not be cached. |
| `IsCacheable` | `bool` | Gets whether the node has a valid identity and no uncacheable reason. |

## Applies to

Cerneala retained Prism node caching and surface planning.

## See also

- `Cerneala.Drawing.Prism.Graph.PrismGraphExecutionPlan`
- `Cerneala.Drawing.Prism.Graph.PrismGraphBoundsStatus`
- `Cerneala.Drawing.Prism.Graph.PrismGraphUncacheableReason`
- `Cerneala.Drawing.Prism.Graph.PrismGraphDependency`
