# PrismBlendMode Enum

## Definition
Namespace: `Cerneala.Drawing.Prism.Catalog`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json` (generated)

Specifies how a Prism contribution is blended with the accumulated result below it.

```csharp
public enum PrismBlendMode
```

## Values

| Name | Stable ID | Category |
| --- | ---: | --- |
| `Normal` | `145` | Normal |
| `Dissolve` | `146` | Normal |
| `Darken` | `147` | Darken |
| `Multiply` | `148` | Darken |
| `ColorBurn` | `149` | Darken |
| `LinearBurn` | `150` | Darken |
| `DarkerColor` | `151` | Darken |
| `Lighten` | `152` | Lighten |
| `Screen` | `153` | Lighten |
| `ColorDodge` | `154` | Lighten |
| `LinearDodge` | `155` | Lighten |
| `LighterColor` | `156` | Lighten |
| `Overlay` | `157` | Contrast |
| `SoftLight` | `158` | Contrast |
| `HardLight` | `159` | Contrast |
| `VividLight` | `160` | Contrast |
| `LinearLight` | `161` | Contrast |
| `PinLight` | `162` | Contrast |
| `HardMix` | `163` | Contrast |
| `Difference` | `164` | Comparative |
| `Exclusion` | `165` | Comparative |
| `Subtract` | `166` | Comparative |
| `Divide` | `167` | Comparative |
| `Hue` | `168` | Component |
| `Saturation` | `169` | Component |
| `Color` | `170` | Component |
| `Luminosity` | `171` | Component |
| `PassThrough` | `172` | Group only |

## Remarks

`PassThrough` is valid only for groups. Layers, backdrops, and individual filters reject it.

## Applies to

Cerneala Prism layer, group, filter, and runtime blending state.
