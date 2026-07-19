# PrismAnalyzedScope Struct

## Definition
Namespace: `Cerneala.Drawing.Prism.Graph`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/Graph/PrismAnalyzedScope.cs`

Describes one validated Prism scope and its position, bounds, dependencies, and resource requirements.

```csharp
public readonly record struct PrismAnalyzedScope
```

## Examples

```csharp
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Graph;

PrismFrameAnalysis analysis =
    new PrismFrameAnalyzer().Analyze(new DrawCommandList());

foreach (PrismAnalyzedScope scope in analysis.Scopes)
{
    Console.WriteLine(
        $"{scope.ScopeIndex}: commands {scope.BeginCommandIndex}..{scope.EndCommandIndex}");
}
```

## Remarks

Instances are created by `PrismFrameAnalyzer`; the constructor is not public. Scope indices follow `BeginPrism` encounter order. `ParentScopeIndex` is `null` for a top-level scope, while `Depth` is zero for top-level scopes and increases for nested scopes.

`Bounds` contains the transformed control bounds intersected with the active retained clip. A fully clipped scope has empty bounds, no required surfaces, and no required capabilities.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `ScopeIndex` | `int` | Gets the zero-based scope index in analysis encounter order. |
| `BeginCommandIndex` | `int` | Gets the index of the matching `BeginPrism` command. |
| `EndCommandIndex` | `int` | Gets the index of the matching `EndPrism` command. |
| `Depth` | `int` | Gets the zero-based Prism nesting depth. |
| `ParentScopeIndex` | `int?` | Gets the parent scope index, or `null` for a top-level scope. |
| `Scope` | `PrismDrawScope` | Gets the typed retained scope payload. |
| `Bounds` | `DrawRect` | Gets the transformed and actively clipped logical bounds. |
| `DependencyStamp` | `PrismDependencyStamp` | Gets the versions and identities captured for invalidation. |
| `RequiredCapabilities` | `PrismGraphCapabilities` | Gets the backend-neutral operations required by this scope. |
| `RequiredSurfaceCount` | `int` | Gets the estimated number of intermediate surfaces required by this scope. |

## Applies to

Cerneala Prism frame inspection and retained composition graph construction.

## See also

- `Cerneala.Drawing.Prism.PrismDrawScope`
- `Cerneala.Drawing.Prism.Graph.PrismDependencyStamp`
- `Cerneala.Drawing.Prism.Graph.PrismFrameAnalysis`
- `Cerneala.Drawing.Prism.Graph.PrismFrameAnalyzer`
