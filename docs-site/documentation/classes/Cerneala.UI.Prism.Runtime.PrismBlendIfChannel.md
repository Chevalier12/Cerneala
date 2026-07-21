# PrismBlendIfChannel Enum

## Definition
Namespace: `Cerneala.UI.Prism.Runtime`

Assembly/Project: `Cerneala`

Source: `UI/Prism/Runtime/PrismAdvancedBlend.cs`

Specifies the composite or component channel evaluated by Blend If.

```csharp
public enum PrismBlendIfChannel
```

## Remarks

The selected channel is evaluated against the source and underlying Blend If
ranges after mask and fill preparation and before final opacity and blending.

## Values

| Name | Description |
| --- | --- |
| `Gray` | Uses composite luminance; the catalog default. |
| `Red` | Uses the red channel. |
| `Green` | Uses the green channel. |
| `Blue` | Uses the blue channel. |

## Applies to

`PrismLayerState.BlendIfChannel`.
