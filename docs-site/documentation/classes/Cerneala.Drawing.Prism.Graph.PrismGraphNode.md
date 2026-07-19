# PrismGraphNode Class

## Definition
Namespace: `Cerneala.Drawing.Prism.Graph`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/Graph/PrismGraph.cs`

Describes one immutable operation and its captured inputs in a retained Prism graph.

```csharp
public sealed class PrismGraphNode
```

Inheritance:
`object` -> `PrismGraphNode`

## Examples

```csharp
using Cerneala.Drawing.Prism.Graph;

static void PrintNodes(PrismGraph graph)
{
    foreach (PrismGraphNode node in graph.Nodes)
    {
        Console.WriteLine($"{node.Id}: {node.Kind}");
    }
}
```

## Remarks

Instances are emitted by `PrismGraphBuilder`; the constructor is not public. `Id` is structural and remains stable across value-only frames, while `Dependencies`, `Parameters`, and operation-specific properties snapshot the inputs of the current analysis.

Only properties meaningful for `Kind` are populated. For example, layer nodes expose `LayerSettings`, filter nodes expose `Filter`, style nodes expose `Style`, color-conversion nodes expose `ColorProfile`, and mask nodes expose resource and mask settings.

`PassThroughComposite` nodes are explicit non-isolating group boundaries. Their normal output retains the original incoming background and the group's local contribution, while a `ClipBaseAlpha` edge sourced from the boundary selects the local contribution alpha rather than accumulated background alpha.

`DefinitionOrder` is deterministic definition-tree order. Synthetic scope nodes use `-1`, while definition-backed nodes use a zero-based preorder index.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Id` | `PrismGraphNodeId` | Gets the stable structural node identifier. |
| `Kind` | `PrismGraphNodeKind` | Gets the operation kind. |
| `AnalysisScopeIndex` | `int` | Gets the source frame-analysis scope index. |
| `DefinitionNodeId` | `PrismNodeId?` | Gets the originating definition node ID, or `null` for a scope-level synthetic node. |
| `DefinitionOrder` | `int` | Gets deterministic definition-tree order, or `-1` for a synthetic node. |
| `DiagnosticName` | `string` | Gets the deterministic composition path and operation suffix. |
| `Dependencies` | `ImmutableArray<PrismGraphDependency>` | Gets immutable versioned input dependencies. |
| `Parameters` | `ImmutableArray<PrismGraphParameter>` | Gets immutable catalog parameter snapshots. |
| `IsIsolationBoundary` | `bool` | Gets whether a group must be composed in isolation. |
| `BlendMode` | `PrismBlendMode?` | Gets the blend mode when the operation uses one. |
| `Amount` | `float?` | Gets the fill, opacity, or filter amount when applicable. |
| `Filter` | `PrismFilterId?` | Gets the catalog filter ID for a filter node. |
| `Style` | `PrismStyleId?` | Gets the catalog style ID for a style node. |
| `Resource` | `PrismResourceId?` | Gets the image resource for a mask node. |
| `ColorProfile` | `PrismColorProfile?` | Gets the destination working profile for a color-conversion node. |
| `MaskChannel` | `PrismMaskChannel?` | Gets the selected mask channel for a mask node. |
| `Feather` | `float?` | Gets mask feathering when applicable. |
| `Density` | `float?` | Gets mask density when applicable. |
| `Invert` | `bool?` | Gets whether mask alpha is inverted when applicable. |
| `LayerSettings` | `PrismGraphLayerSettings?` | Gets captured advanced blending values for a layer node, or `null` for other node kinds. |

## Applies to

Cerneala retained Prism graph inspection, diagnostics, caching, and backend execution.

## See also

- `Cerneala.Drawing.Prism.Graph.PrismGraph`
- `Cerneala.Drawing.Prism.Graph.PrismGraphNodeId`
- `Cerneala.Drawing.Prism.Graph.PrismGraphNodeKind`
- `Cerneala.Drawing.Prism.Graph.PrismGraphLayerSettings`
- `Cerneala.Drawing.Prism.Graph.PrismGraphBuilder`
