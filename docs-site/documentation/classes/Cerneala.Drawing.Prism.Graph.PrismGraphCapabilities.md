# PrismGraphCapabilities Enum

## Definition
Namespace: `Cerneala.Drawing.Prism.Graph`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/Graph/PrismGraphCapabilities.cs`

Identifies backend-neutral Prism operations required by an analyzed scope or frame.

```csharp
[Flags]
public enum PrismGraphCapabilities
```

## Examples

```csharp
using Cerneala.Drawing.Prism.Graph;

PrismGraphCapabilities required =
    PrismGraphCapabilities.ControlCapture |
    PrismGraphCapabilities.FilterProcessing;

bool needsFilters = required.HasFlag(
    PrismGraphCapabilities.FilterProcessing);
```

## Remarks

`PrismFrameAnalyzer` combines these flags for each `PrismAnalyzedScope` and for the entire `PrismFrameAnalysis`. Consumers can test individual requirements with bitwise operations or `HasFlag`.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `None` | `0` | No Prism graph capability is required. |
| `ControlCapture` | `1` | The scope requires capture of retained control content. |
| `FilterProcessing` | `2` | At least one filter operation must be processed. |
| `StyleProcessing` | `4` | At least one style operation must be processed. |
| `MaskProcessing` | `8` | At least one mask must be processed. |
| `GroupProcessing` | `16` | At least one Prism group must be processed. |
| `GroupIsolation` | `32` | At least one group requires isolated composition. |
| `Clipping` | `64` | Layer clipping is required. |
| `AdvancedBlending` | `128` | A blend mode other than the normal or pass-through cases is required. |
| `ColorConversion` | `256` | The composition requires color conversion support. |
| `BackdropInput` | `512` | The scope has an active visible backdrop contribution. |

## Applies to

Cerneala Prism frame analysis and retained composition graph construction.

## See also

- `Cerneala.Drawing.Prism.Graph.PrismAnalyzedScope`
- `Cerneala.Drawing.Prism.Graph.PrismFrameAnalysis`
- `Cerneala.Drawing.Prism.Graph.PrismFrameAnalyzer`
