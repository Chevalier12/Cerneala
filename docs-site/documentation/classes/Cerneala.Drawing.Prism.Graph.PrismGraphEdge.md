# PrismGraphEdge Struct

## Definition
Namespace: `Cerneala.Drawing.Prism.Graph`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/Graph/PrismGraph.cs`

Represents a directed, typed connection between two Prism graph nodes.

```csharp
public readonly record struct PrismGraphEdge(
    PrismGraphNodeId Source,
    PrismGraphNodeId Target,
    PrismGraphEdgeKind Kind);
```

## Examples

```csharp
using Cerneala.Drawing.Prism.Graph;

static void PrintEdges(PrismGraph graph)
{
    foreach (PrismGraphEdge edge in graph.Edges)
    {
        PrismGraphNode source = graph.GetNode(edge.Source);
        PrismGraphNode target = graph.GetNode(edge.Target);
        Console.WriteLine($"{source.Kind} -> {target.Kind}");
    }
}
```

## Remarks

Data flows from `Source` to `Target`. The edge kind identifies which target input receives the source output, including distinct mask alpha, clipping base alpha, and composite background or foreground inputs.

## Constructors

| Name | Description |
| --- | --- |
| `PrismGraphEdge(PrismGraphNodeId source, PrismGraphNodeId target, PrismGraphEdgeKind kind)` | Creates an immutable directed edge. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Source` | `PrismGraphNodeId` | Gets the source node identifier. |
| `Target` | `PrismGraphNodeId` | Gets the target node identifier. |
| `Kind` | `PrismGraphEdgeKind` | Gets the role of the connection. |

## Applies to

Cerneala retained Prism graph traversal and backend execution.

## See also

- `Cerneala.Drawing.Prism.Graph.PrismGraphEdgeKind`
- `Cerneala.Drawing.Prism.Graph.PrismGraphNodeId`
- `Cerneala.Drawing.Prism.Graph.PrismGraph`
