# PrismLayerState Class

## Definition
Namespace: `Cerneala.UI.Prism.Runtime`

Assembly/Project: `Cerneala`

Source: `UI/Prism/Runtime/PrismStates.cs`

Exposes mutable typed values for one layer in a `PrismInstance`.

```csharp
public sealed class PrismLayerState : PrismNodeState
```

## Examples

```csharp
PrismLayerState layer = instance.GetLayerState(new PrismNodeId(1));
layer.Opacity = 0.75f;
layer.BlendIfChannel = PrismBlendIfChannel.Gray;
layer.UnderlyingRange = new PrismBlendRange(0f, 0.1f, 0.9f, 1f);
```

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Filters` | `IReadOnlyList<PrismFilterState>` | Gets filter occurrences in declared order. |
| `Styles` | `IReadOnlyList<PrismStyleState>` | Gets style occurrences in declared order. |
| `Mask` | `PrismMaskState?` | Gets the optional mask state. |
| `Visible` | `bool` | Gets or sets whether the complete layer participates. |
| `Opacity` | `float` | Gets or sets opacity for content and styles together. |
| `Fill` | `float` | Gets or sets prepared-content opacity before styles. |
| `BlendMode` | `PrismBlendMode` | Gets or sets the layer blend mode. |
| `ClipToBelow` | `bool` | Gets or sets clipping-chain participation. |
| `BlendChannels` | `PrismBlendChannels` | Gets or sets channels written by the layer. |
| `Knockout` | `PrismKnockout` | Gets or sets knockout behavior. |
| `BlendInteriorStylesAsGroup` | `bool` | Gets or sets whether inner styles blend with content before the layer blend. |
| `BlendClippedLayersAsGroup` | `bool` | Gets or sets grouped blending for the clipping chain. |
| `TransparencyShapesLayer` | `bool` | Gets or sets whether transparency shapes layer effects. |
| `LayerMaskHidesStyles` | `bool` | Gets or sets whether the layer mask hides generated styles. |
| `VectorMaskHidesStyles` | `bool` | Gets or sets whether a vector mask hides generated styles. |
| `BlendIfChannel` | `PrismBlendIfChannel` | Gets or sets the Blend If channel. |
| `ThisLayerRange` | `PrismBlendRange` | Gets or sets source thresholds. |
| `UnderlyingRange` | `PrismBlendRange` | Gets or sets underlying-result thresholds. |
| `DissolveSeed` | `int` | Gets or sets the stable nonnegative Dissolve seed. |

## Remarks

All defaults originate in the canonical catalog. Setters write directly to dense typed storage and increment `PrismInstance.ValueVersion` only when the effective value changes.

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Opacity`, `Fill` | `ArgumentOutOfRangeException` | The value is non-finite or outside zero through one. |
| `BlendMode` | `ArgumentException` | The value is `PassThrough`. |
| `BlendChannels`, `Knockout`, `BlendIfChannel` | `ArgumentOutOfRangeException` | The enum value is unsupported. |
| `DissolveSeed` | `ArgumentOutOfRangeException` | The value is negative. |

## Applies to

Per-element normal Prism layer state.
