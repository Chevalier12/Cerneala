# PrismBlendChannels Enum

## Definition
Namespace: `Cerneala.UI.Prism.Runtime`

Assembly/Project: `Cerneala`

Source: `UI/Prism/Runtime/PrismAdvancedBlend.cs`

Specifies the color and alpha channels written by a Prism layer.

```csharp
[Flags]
public enum PrismBlendChannels
```

## Remarks

Values may be combined because the enum has `FlagsAttribute`. Runtime setters
reject bit combinations outside the declared `Rgba` mask.

## Values

| Name | Value | Description |
| --- | ---: | --- |
| `None` | `0` | Writes no channels. |
| `Red` | `1` | Writes red. |
| `Green` | `2` | Writes green. |
| `Blue` | `4` | Writes blue. |
| `Alpha` | `8` | Writes alpha. |
| `Rgb` | `7` | Writes red, green, and blue. |
| `Rgba` | `15` | Writes all channels; the catalog default. |

## Applies to

`PrismLayerState.BlendChannels`.
