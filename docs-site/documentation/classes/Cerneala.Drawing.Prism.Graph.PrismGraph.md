# PrismGraph Class

## Definition
Namespace: `Cerneala.Drawing.Prism.Graph`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/Graph/PrismGraph.cs`

Stores an immutable retained Prism composition graph.

```csharp
public sealed class PrismGraph
```

Inheritance:
`object` -> `PrismGraph`

## Examples

```csharp
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Graph;

DrawCommandList commands = new();
PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(commands);
PrismGraph graph = new PrismGraphBuilder().Build(analysis);

Console.WriteLine(graph.ToDiagnosticString());
```

## Remarks

Instances are created by `PrismGraphBuilder` and `PrismGraphOptimizer`; the
constructor is not public. The builder produces the raw graph from one current
frame analysis. The optimizer returns a separate optimized graph through
`PrismGraphExecutionPlan` without modifying the raw graph.

`Nodes` are deterministic, `Edges` are directed from producer to consumer, and
`Scopes` preserve frame-analysis scope order. Nodes in an optimized graph follow
the plan's topological `ExecutionOrder`. Graph construction validates that all
edge endpoints exist and rejects cycles, so every accepted graph is a DAG.

`ControlCaptureCount` and `BackdropInputCount` report explicit input branches. A non-empty analyzed scope has at most one control-capture node, while backdrop input remains a separate branch. Each metadata-backed backdrop input exposes the shared frame through `BackdropFrame`, then an explicit `BackdropCrop` identifies the source-pixel rectangle before color and alpha normalization.

Pass-through groups use an explicit `PassThroughComposite` boundary. Its ordinary output combines the local group result with the original incoming background, but a `ClipBaseAlpha` edge sourced from that boundary addresses only the group's local alpha. Backends must preserve both semantics instead of treating the boundary as an ordinary flattened composite.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Nodes` | `ImmutableArray<PrismGraphNode>` | Gets all operation nodes in deterministic graph evaluation order. |
| `Edges` | `ImmutableArray<PrismGraphEdge>` | Gets all directed typed connections. |
| `Scopes` | `ImmutableArray<PrismGraphScope>` | Gets scope-to-output mappings in analysis order. |
| `ControlCaptureCount` | `int` | Gets the number of control-capture input nodes. |
| `BackdropInputCount` | `int` | Gets the number of backdrop input nodes. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `GetNode(PrismGraphNodeId id)` | `PrismGraphNode` | Resolves a graph node by its structural identifier. |
| `ToDiagnosticString()` | `string` | Returns a deterministic multiline snapshot of scopes, nodes, dependencies, and edges. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `GetNode(PrismGraphNodeId)` | `KeyNotFoundException` | The graph does not contain `id`. |

## Applies to

Cerneala retained Prism composition planning and backend execution.

## See also

- `Cerneala.Drawing.Prism.Graph.PrismGraphBuilder`
- `Cerneala.Drawing.Prism.Graph.PrismGraphOptimizer`
- `Cerneala.Drawing.Prism.Graph.PrismGraphExecutionPlan`
- `Cerneala.Drawing.Prism.Graph.PrismGraphNode`
- `Cerneala.Drawing.Prism.Graph.PrismGraphEdge`
- `Cerneala.Drawing.Prism.Graph.PrismGraphScope`
- `Cerneala.Drawing.Prism.Graph.PrismGraphCompositionSettings`
- `Cerneala.Drawing.Prism.Graph.PrismGraphLayerSettings`
