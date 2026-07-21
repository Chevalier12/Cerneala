# PrismColorProfile Enum

## Definition
Namespace: `Cerneala.Drawing.Prism.Catalog`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json` (generated)

Specifies a working or output color profile for Prism processing.

```csharp
public enum PrismColorProfile
```

## Remarks

The generated enum is the closed built-in profile set for the current Prism
contract. `LinearSrgb` is the composition default. The enum is not a registration
point for application-provided color profiles.

## Values

| Name | Stable ID | Description |
| --- | ---: | --- |
| `LinearSrgb` | `173` | Linear-light sRGB; the default Prism working profile. |
| `Srgb` | `174` | Gamma-encoded sRGB. |
| `LinearDisplayP3` | `175` | Linear-light Display P3. |
| `DisplayP3` | `176` | Gamma-encoded Display P3. |
| `ScRgb` | `177` | Extended-range scRGB. |

## Applies to

Cerneala Prism compositions and drawing frame color conversion.
