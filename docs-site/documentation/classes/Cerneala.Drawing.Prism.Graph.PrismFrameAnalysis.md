# PrismFrameAnalysis Class

## Definition
Namespace: `Cerneala.Drawing.Prism.Graph`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/Graph/PrismFrameAnalysis.cs`

Stores the immutable Prism scope analysis produced for one retained draw command list version.

```csharp
public sealed class PrismFrameAnalysis
```

Inheritance:
`object` -> `PrismFrameAnalysis`

## Examples

```csharp
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Graph;

DrawCommandList commands = new();
PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(commands);

analysis.EnsureCurrent(commands);
Console.WriteLine(analysis.Scopes.Length);
```

## Remarks

Instances are created by `PrismFrameAnalyzer`; the constructor is not public. The analysis retains the source command-list identity and snapshots its `Version`.

`EnsureCurrent` rejects a different command-list instance, a changed list version, or a scope whose current structural or value version differs from its `PrismDependencyStamp`. This also detects nested Prism value changes because every analyzed scope is validated.

`Scopes` and backdrop scope indices are immutable. `RequiredCapabilities` and `RequiredSurfaceCount` aggregate the corresponding values from all non-empty scopes in the frame.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `CommandListVersion` | `long` | Gets the source command-list version captured during analysis. |
| `Scopes` | `System.Collections.Immutable.ImmutableArray<PrismAnalyzedScope>` | Gets the analyzed Prism scopes in `BeginPrism` encounter order. |
| `RequiredCapabilities` | `PrismGraphCapabilities` | Gets the combined backend-neutral capabilities required by the frame. |
| `RequiredSurfaceCount` | `int` | Gets the combined estimated intermediate surface count. |
| `BackdropRequirement` | `PrismBackdropRequirement?` | Gets the frame-level backdrop requirement, or `null` when no scope needs backdrop input. |
| `RequiresBackdrop` | `bool` | Gets whether `BackdropRequirement` is present. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `EnsureCurrent(DrawCommandList commands)` | `void` | Verifies that `commands` is the analyzed list at the captured version and that all scope runtime versions are current. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `EnsureCurrent` | `ArgumentNullException` | `commands` is `null`. |
| `EnsureCurrent` | `InvalidOperationException` | The list identity or version differs, or a scope structural or value version is stale. |

## Applies to

Cerneala retained frame validation, drawing submission, and Prism graph construction.

## See also

- `Cerneala.Drawing.DrawingFrameContext`
- `Cerneala.Drawing.Prism.Graph.PrismAnalyzedScope`
- `Cerneala.Drawing.Prism.Graph.PrismBackdropRequirement`
- `Cerneala.Drawing.Prism.Graph.PrismFrameAnalyzer`
