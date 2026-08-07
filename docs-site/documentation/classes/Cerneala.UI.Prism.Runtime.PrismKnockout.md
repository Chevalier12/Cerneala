# PrismKnockout Enum

## Definition
Namespace: `Cerneala.UI.Prism.Runtime`

Assembly/Project: `Cerneala`

Source: `UI/Prism/Runtime/PrismAdvancedBlend.cs`

Specifies Photoshop-style knockout behavior for a Prism layer.

```csharp
public enum PrismKnockout
```

## Remarks

Knockout is a layer-only setting and defaults to `None`. The corresponding
runtime state setter rejects values outside this enum.

For `Shallow` and `Deep`, Prism uses a clean-room implementation of the PDF 1.7
knockout-group recurrence. The compositor keeps the current backdrop, the
original backdrop selected by the knockout scope, the source shape before
layer opacity, and the source alpha as separate values. The layer blend mode is
evaluated against the original backdrop; knockout does not silently replace the
selected blend mode with `Normal`.

`Shallow` selects the nearest pass-through or isolated group boundary.
`Deep` selects the deepest backdrop available to the composition. At the root
level both modes can therefore resolve to the same backdrop. Disabled blend
channels are restored from the current backdrop after the recurrence.

The CPU reference is implemented by
`PrismBlendMath.CompositeKnockout`; the GPU implementation is in
`Drawing/MonoGame/Prism/Shaders/Blends/AdvancedBlending.fx`. Both operate on
premultiplied color and use the same shape/alpha recurrence.

## Examples

```csharp
PrismLayerState layer = instance.GetLayerState(layerId);
layer.BlendMode = PrismBlendMode.Multiply;
layer.Knockout = PrismKnockout.Deep;
```

## Values

| Name | Description |
| --- | --- |
| `None` | Performs no knockout. |
| `Shallow` | Knocks out earlier group content to the nearest group backdrop. |
| `Deep` | Knocks out through nested groups to the deepest composition backdrop. |

## Applies to

`PrismLayerState.Knockout`.
