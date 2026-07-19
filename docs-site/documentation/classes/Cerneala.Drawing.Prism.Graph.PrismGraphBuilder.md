# PrismGraphBuilder Class

## Definition
Namespace: `Cerneala.Drawing.Prism.Graph`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/Graph/PrismGraphBuilder.cs`

Builds a deterministic retained composition graph from one current Prism frame analysis.

```csharp
public sealed class PrismGraphBuilder
```

Inheritance:
`object` -> `PrismGraphBuilder`

## Examples

```csharp
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Graph;

DrawCommandList commands = new();
PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(commands);

PrismGraphBuilder builder = new();
PrismGraph graph = builder.Build(analysis);
```

## Remarks

`Build` consumes `PrismFrameAnalysis` directly and does not rescan the draw command list. It verifies that the analysis is current both before and after construction.

Each non-empty analyzed scope receives one control-capture branch and a working-color conversion. Visible content is processed bottom-up; hidden or zero-opacity contributions are omitted. Composition-level settings are captured on every graph scope, and advanced layer settings are captured on each emitted layer node. Non-pass-through groups are marked as isolation boundaries. Masks, clipping-base alpha, and backdrop input use distinct graph branches and edge kinds.

A pass-through group is built over the incoming stack background rather than an isolated transparent surface. Its explicit `PassThroughComposite` boundary receives the original background and the group's local result, then consumes that background so the parent stack does not composite it a second time. When the boundary becomes a clipping base, `ClipBaseAlpha` selects the group's local alpha instead of alpha already present in the original background.

Node IDs are derived from the retained scope owner, definition node ID, operation kind, and ordinal, so value-only changes update snapshots without changing graph identity.

## Constructors

| Name | Description |
| --- | --- |
| `PrismGraphBuilder()` | Initializes a graph builder. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Build(PrismFrameAnalysis analysis)` | `PrismGraph` | Validates `analysis` and builds immutable nodes, edges, and scope outputs. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Build` | `ArgumentNullException` | `analysis` is `null`. |
| `Build` | `PrismGraphBuildException` | The analysis is stale or a composition/runtime value cannot be represented as a valid graph. |

## Applies to

Cerneala retained Prism graph construction between frame analysis and backend execution.

## See also

- `Cerneala.Drawing.Prism.Graph.PrismFrameAnalyzer`
- `Cerneala.Drawing.Prism.Graph.PrismFrameAnalysis`
- `Cerneala.Drawing.Prism.Graph.PrismGraph`
- `Cerneala.Drawing.Prism.Graph.PrismGraphCompositionSettings`
- `Cerneala.Drawing.Prism.Graph.PrismGraphLayerSettings`
- `Cerneala.Drawing.Prism.Graph.PrismGraphBuildException`
