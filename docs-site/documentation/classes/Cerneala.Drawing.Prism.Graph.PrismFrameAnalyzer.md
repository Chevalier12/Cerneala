# PrismFrameAnalyzer Class

## Definition
Namespace: `Cerneala.Drawing.Prism.Graph`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/Graph/PrismFrameAnalyzer.cs`

Validates and analyzes the retained command stream for Prism scopes in a single command-list pass.

```csharp
public sealed class PrismFrameAnalyzer
```

Inheritance:
`object` -> `PrismFrameAnalyzer`

## Examples

```csharp
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Graph;

DrawCommandList commands = new();
PrismFrameAnalyzer analyzer = new();

PrismFrameAnalysis analysis = analyzer.Analyze(commands);
Console.WriteLine(analysis.RequiredSurfaceCount);
```

## Remarks

`Analyze` walks the command list once. It validates Prism and clip nesting, records matching command indices and parent relationships, transforms each scope's control bounds, intersects them with the active clip, and snapshots dependency versions.

Capability and surface requirements are estimated from the immutable Prism definition. Fully clipped scopes contribute neither. A visible, positive-opacity runtime backdrop on a non-empty scope contributes its index to one frame-level `PrismBackdropRequirement`.

Nested scope dependencies are folded into their parent's `PrismDependencyStamp.DescendantVersion`. The completed analysis is validated against the source list before it is returned.

## Constructors

| Name | Description |
| --- | --- |
| `PrismFrameAnalyzer()` | Initializes a Prism frame analyzer. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Analyze(DrawCommandList commands)` | `PrismFrameAnalysis` | Validates and analyzes `commands`, returning an immutable frame analysis tied to that list and version. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Analyze` | `ArgumentNullException` | `commands` is `null`. |
| `Analyze` | `InvalidOperationException` | Prism or clip commands are malformed, a `BeginPrism` has no payload, the command list changes during analysis, or a captured scope changes before analysis completes. |

Nesting diagnostics include the relevant command index.

## Applies to

Cerneala retained Prism frame analysis before backend submission or graph construction.

## See also

- `Cerneala.Drawing.DrawCommandList`
- `Cerneala.Drawing.DrawingFrameContext`
- `Cerneala.Drawing.Prism.Graph.PrismFrameAnalysis`
- `Cerneala.Drawing.Prism.Graph.PrismAnalyzedScope`
