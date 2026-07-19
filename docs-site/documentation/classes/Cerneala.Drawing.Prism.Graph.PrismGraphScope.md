# PrismGraphScope Struct

## Definition
Namespace: `Cerneala.Drawing.Prism.Graph`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/Graph/PrismGraph.cs`

Maps one analyzed Prism scope to its command interval, retained owner, spatial
metadata, and optional graph output.

```csharp
public readonly record struct PrismGraphScope
```

## Examples

```csharp
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;

static void PrintScope(PrismGraph graph)
{
    PrismGraphScope scope = graph.Scopes[0];
    Console.WriteLine(
        $"commands {scope.BeginCommandIndex}..{scope.EndCommandIndex}, " +
        $"depth {scope.Depth}, scale {scope.PixelScale}");
}
```

## Remarks

Instances are emitted by `PrismGraphBuilder` and copied into optimized graphs;
the constructor is not public. `AnalysisScopeIndex` points back to the
corresponding entry in `PrismFrameAnalysis.Scopes`. Command indices identify the
balanced `BeginPrism` and `EndPrism` interval. `ParentScopeIndex` is `null` and
`Depth` is zero for a root scope.

`Bounds` is already transformed by `EffectiveTransform` and clipped by the
active retained clip, in logical drawing coordinates. A backend must not apply
`EffectiveTransform` to `Bounds` again. `PixelScale` converts logical
coordinates to physical surface pixels.

`CompositionSettings` snapshots the current color and shared-light values even
when the scope emits no output. `Output` is `null` when the analyzed scope has
empty bounds or no visible content or backdrop contribution.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `AnalysisScopeIndex` | `int` | Gets the index in the source frame analysis. |
| `BeginCommandIndex` | `int` | Gets the index of the matching `BeginPrism` command. |
| `EndCommandIndex` | `int` | Gets the index of the matching `EndPrism` command. |
| `Depth` | `int` | Gets the zero-based Prism nesting depth. |
| `ParentScopeIndex` | `int?` | Gets the parent analysis scope index, or `null` for a root scope. |
| `CacheOwnerToken` | `PrismCacheOwnerToken` | Gets the retained scope owner token. |
| `CompositionSettings` | `PrismGraphCompositionSettings` | Gets the captured composition-level color and lighting values. |
| `Bounds` | `DrawRect` | Gets transformed and clipped bounds in logical drawing coordinates. |
| `EffectiveTransform` | `System.Numerics.Matrix3x2` | Gets the logical transform already reflected in `Bounds`. |
| `PixelScale` | `float` | Gets the logical-to-physical surface scale. |
| `Output` | `PrismGraphNodeId?` | Gets the final graph output, or `null` when the scope emits none. |

## Applies to

Cerneala retained Prism graph results for analyzed draw scopes.

## See also

- `Cerneala.Drawing.Prism.Graph.PrismAnalyzedScope`
- `Cerneala.Drawing.Prism.Graph.PrismFrameAnalysis`
- `Cerneala.Drawing.Prism.Graph.PrismGraphCompositionSettings`
- `Cerneala.Drawing.Prism.Graph.PrismGraph`
- `Cerneala.Drawing.Prism.Graph.PrismGraphExecutionPlan`
