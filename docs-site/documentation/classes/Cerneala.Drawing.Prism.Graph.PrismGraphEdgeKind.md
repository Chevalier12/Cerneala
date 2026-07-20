# PrismGraphEdgeKind Enum

## Definition
Namespace: `Cerneala.Drawing.Prism.Graph`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/Graph/PrismGraph.cs`

Identifies the role of a directed dependency between two Prism graph nodes.

```csharp
public enum PrismGraphEdgeKind
```

## Examples

```csharp
using Cerneala.Drawing.Prism.Graph;

static IEnumerable<PrismGraphEdge> GetMaskInputs(PrismGraph graph) =>
    graph.Edges.Where(
        edge => edge.Kind == PrismGraphEdgeKind.MaskAlpha);
```

## Remarks

An edge runs from `Source` to `Target`. Specialized kinds keep control, prepared style source, backdrop, mask, clipping, group, and foreground/background inputs explicit instead of overloading a generic content edge.

`StyleSource` preserves the prepared layer alpha before `Fill` is applied. A style node therefore receives its current composited `Content` separately from the source geometry used to produce shadows, glows, bevels, overlays, satin, and strokes.

When `ClipBaseAlpha` is sourced from a `PassThroughComposite` node, it selects that boundary's local contribution alpha rather than alpha from the original incoming background.

## Fields

| Name | Description |
| --- | --- |
| `Content` | Connects the ordinary output of one operation to the next. |
| `StyleSource` | Supplies prepared, pre-`Fill` layer content to a layer-style operation. |
| `Control` | Connects converted retained control content to a layer. |
| `Backdrop` | Connects a backdrop input to its color-conversion branch. |
| `GroupContent` | Connects an assembled child stack to its group node. |
| `MaskAlpha` | Supplies mask alpha to a masking composite. |
| `ClipBaseAlpha` | Supplies the non-clipping base alpha to a clipping node. |
| `CompositeBackground` | Supplies the accumulated background of a stack composite. |
| `CompositeForeground` | Supplies the foreground contribution of a stack composite. |

## Applies to

Cerneala retained Prism graph traversal and execution.

## See also

- `Cerneala.Drawing.Prism.Graph.PrismGraphEdge`
- `Cerneala.Drawing.Prism.Graph.PrismGraphNodeKind`
- `Cerneala.Drawing.Prism.Graph.PrismGraph`
