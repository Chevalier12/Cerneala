# PrismGraphLayerSettings Struct

## Definition
Namespace: `Cerneala.Drawing.Prism.Graph`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/Graph/PrismGraph.cs`

Stores the immutable advanced-blending values captured for one Prism layer graph node.

```csharp
public readonly record struct PrismGraphLayerSettings(
    PrismBlendChannels BlendChannels,
    PrismKnockout Knockout,
    bool BlendInteriorStylesAsGroup,
    bool BlendClippedLayersAsGroup,
    bool TransparencyShapesLayer,
    bool LayerMaskHidesStyles,
    bool VectorMaskHidesStyles,
    PrismBlendIfChannel BlendIfChannel,
    PrismBlendRange ThisLayerRange,
    PrismBlendRange UnderlyingRange,
    int DissolveSeed);
```

## Examples

```csharp
using Cerneala.Drawing.Prism.Graph;

static PrismGraphLayerSettings? GetLayerSettings(
    PrismGraphNode node)
{
    return node.Kind == PrismGraphNodeKind.Layer
        ? node.LayerSettings
        : null;
}
```

## Remarks

`PrismGraphBuilder` snapshots these values from the current `PrismLayerState` and stores them only on `Layer` nodes. The snapshot belongs to the graph and does not retain mutable runtime state.

The builder rejects unknown blend-channel bits, undefined knockout or Blend If values, and a negative dissolve seed. The record constructor itself performs no validation.

## Constructors

| Name | Description |
| --- | --- |
| `PrismGraphLayerSettings(PrismBlendChannels blendChannels, PrismKnockout knockout, bool blendInteriorStylesAsGroup, bool blendClippedLayersAsGroup, bool transparencyShapesLayer, bool layerMaskHidesStyles, bool vectorMaskHidesStyles, PrismBlendIfChannel blendIfChannel, PrismBlendRange thisLayerRange, PrismBlendRange underlyingRange, int dissolveSeed)` | Creates an immutable advanced layer-settings snapshot. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `BlendChannels` | `PrismBlendChannels` | Gets the color and alpha channels written by the layer. |
| `Knockout` | `PrismKnockout` | Gets the layer knockout behavior. |
| `BlendInteriorStylesAsGroup` | `bool` | Gets whether inner styles blend with content before the layer blend. |
| `BlendClippedLayersAsGroup` | `bool` | Gets whether the clipping chain is blended as a group. |
| `TransparencyShapesLayer` | `bool` | Gets whether transparency shapes layer effects. |
| `LayerMaskHidesStyles` | `bool` | Gets whether the layer mask hides generated styles. |
| `VectorMaskHidesStyles` | `bool` | Gets whether a vector mask hides generated styles. |
| `BlendIfChannel` | `PrismBlendIfChannel` | Gets the channel evaluated by Blend If. |
| `ThisLayerRange` | `PrismBlendRange` | Gets the source-layer Blend If thresholds. |
| `UnderlyingRange` | `PrismBlendRange` | Gets the underlying-result Blend If thresholds. |
| `DissolveSeed` | `int` | Gets the stable non-negative Dissolve seed. |

## Applies to

Cerneala retained Prism layer graph nodes and backend blending.

## See also

- `Cerneala.Drawing.Prism.Graph.PrismGraphNode`
- `Cerneala.Drawing.Prism.Graph.PrismGraphBuilder`
- `Cerneala.UI.Prism.Runtime.PrismLayerState`
- `Cerneala.UI.Prism.Runtime.PrismBlendRange`
