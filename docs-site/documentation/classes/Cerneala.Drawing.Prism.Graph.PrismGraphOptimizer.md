# PrismGraphOptimizer Class

## Definition
Namespace: `Cerneala.Drawing.Prism.Graph`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/Graph/PrismGraphOptimizer.cs`

Optimizes a retained Prism graph and creates backend-neutral cache, bounds, and
surface lifetime metadata.

```csharp
public sealed class PrismGraphOptimizer
```

Inheritance:
`object` -> `PrismGraphOptimizer`

## Examples

```csharp
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Graph;

PrismFrameAnalysis analysis =
    new PrismFrameAnalyzer().Analyze(new DrawCommandList());
PrismGraph rawGraph = new PrismGraphBuilder().Build(analysis);

PrismGraphOptimizer optimizer = new();
PrismGraphExecutionPlan plan = optimizer.Optimize(rawGraph);
```

## Remarks

`Optimize` is deterministic and does not mutate the source graph, Prism
definitions, or runtime instances. Re-optimizing the resulting graph preserves
the effective plan.

The optimizer removes only proven no-op operations, invisible contributions,
and redundant color conversions. It does not fuse catalog operations unless
their equivalence is declared. Dependencies from aliased nodes remain in the
transitive cache dependency closure.

The result classifies cacheability conservatively, expands known spatial
bounds, and computes explicit surface lifetimes. The optimizer does not execute
effects or allocate GPU resources.

## Constructors

| Name | Description |
| --- | --- |
| `PrismGraphOptimizer()` | Initializes a graph optimizer. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Optimize(PrismGraph graph)` | `PrismGraphExecutionPlan` | Returns a separate optimized graph and its execution metadata. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Optimize` | `ArgumentNullException` | `graph` is `null`. |
| `Optimize` | `InvalidOperationException` | Scope metadata or a required catalog parameter is inconsistent. |

## Applies to

Cerneala retained Prism composition planning before backend execution.

## See also

- `Cerneala.Drawing.Prism.Graph.PrismGraphBuilder`
- `Cerneala.Drawing.Prism.Graph.PrismGraph`
- `Cerneala.Drawing.Prism.Graph.PrismGraphExecutionPlan`
