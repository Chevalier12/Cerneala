# PrismGraphExecutionPlan Class

## Definition
Namespace: `Cerneala.Drawing.Prism.Graph`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/Graph/PrismGraphOptimizer.cs`

Stores an immutable optimized Prism graph and its backend-neutral execution
metadata.

```csharp
public sealed class PrismGraphExecutionPlan
```

Inheritance:
`object` -> `PrismGraphExecutionPlan`

## Examples

```csharp
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Graph;

PrismFrameAnalysis analysis =
    new PrismFrameAnalyzer().Analyze(new DrawCommandList());
PrismGraph rawGraph = new PrismGraphBuilder().Build(analysis);
PrismGraphExecutionPlan plan =
    new PrismGraphOptimizer().Optimize(rawGraph);

Console.WriteLine(plan.PeakLiveSurfaces);
```

## Remarks

Instances are produced by `PrismGraphOptimizer`; the constructor is not public.
`OptimizedGraph` is a separate graph and the input graph is not modified.

`ExecutionOrder` is deterministic and topological. `ExecutionOrder`,
`NodePlans`, and `SurfaceLifetimes` have the same length and align by index.
Lifetime endpoints are inclusive indices into `ExecutionOrder`.

`PeakLiveSurfaces` is derived from overlap among the explicit lifetime
intervals, including captures that remain live across nested scopes. It is
planning metadata and does not allocate or own GPU resources.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `OptimizedGraph` | `PrismGraph` | Gets the optimized graph without nodes proven unnecessary. |
| `ExecutionOrder` | `ImmutableArray<PrismGraphNodeId>` | Gets the deterministic topological node order. |
| `NodePlans` | `ImmutableArray<PrismGraphNodePlan>` | Gets cacheability and bounds metadata aligned with `ExecutionOrder`. |
| `SurfaceLifetimes` | `ImmutableArray<PrismGraphSurfaceLifetime>` | Gets inclusive surface lifetime intervals aligned with `ExecutionOrder`. |
| `RemovedNodeIds` | `ImmutableArray<PrismGraphNodeId>` | Gets structural IDs omitted from the optimized graph. |
| `PeakLiveSurfaces` | `int` | Gets the largest number of simultaneously live planned surfaces. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `GetNodePlan(PrismGraphNodeId nodeId)` | `PrismGraphNodePlan` | Resolves the metadata for an optimized node. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `GetNodePlan` | `KeyNotFoundException` | `nodeId` is not present in the optimized execution plan. |

## Applies to

Cerneala backend-neutral Prism optimization and executor planning.

## See also

- `Cerneala.Drawing.Prism.Graph.PrismGraphOptimizer`
- `Cerneala.Drawing.Prism.Graph.PrismGraph`
- `Cerneala.Drawing.Prism.Graph.PrismGraphNodePlan`
- `Cerneala.Drawing.Prism.Graph.PrismGraphSurfaceLifetime`
