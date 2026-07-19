# PrismGraphNodeKind Enum

## Definition
Namespace: `Cerneala.Drawing.Prism.Graph`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/Graph/PrismGraph.cs`

Identifies the operation represented by a retained Prism graph node.

```csharp
public enum PrismGraphNodeKind
```

## Examples

```csharp
using Cerneala.Drawing.Prism.Graph;

static bool IsCapture(PrismGraphNode node) =>
    node.Kind == PrismGraphNodeKind.ControlCapture;
```

## Remarks

`PrismGraphBuilder` emits nodes in deterministic evaluation order. Input nodes identify retained control or backdrop pixels, operation nodes transform inputs, and composite nodes combine branches.

`PassThroughComposite` is distinct from an ordinary `Composite`: it closes a non-isolating group over the original incoming background while retaining the group's local alpha for downstream `ClipBaseAlpha` edges.

## Fields

| Name | Description |
| --- | --- |
| `ControlCapture` | Captures retained control content for one non-empty analyzed scope. |
| `BackdropInput` | Represents the separate backdrop input branch. |
| `ColorConversion` | Converts an input to the composition working color profile. |
| `Layer` | Selects a visible Prism layer from captured control content. |
| `Group` | Represents an assembled group child stack. |
| `Filter` | Applies a catalog filter operation. |
| `Style` | Applies a catalog style operation. |
| `Mask` | Supplies a mask resource. |
| `Fill` | Applies a layer fill amount. |
| `Opacity` | Applies layer, group, or backdrop opacity. |
| `ClipToBelow` | Clips a layer against the alpha of its non-clipping base. |
| `Composite` | Combines graph branches or completes a masking operation. |
| `PassThroughComposite` | Closes a pass-through group over its original background and exposes local contribution alpha for clipping. |

## Applies to

Cerneala retained Prism graph inspection and backend execution.

## See also

- `Cerneala.Drawing.Prism.Graph.PrismGraphNode`
- `Cerneala.Drawing.Prism.Graph.PrismGraphEdgeKind`
- `Cerneala.Drawing.Prism.Graph.PrismGraphBuilder`
