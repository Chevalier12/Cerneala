# PrismSampling Enum

## Definition
Namespace: `Cerneala.Drawing.Prism.Catalog`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json` (generated)

Specifies the built-in sampling policy used by Prism catalog operations.

```csharp
public enum PrismSampling
```

## Remarks

The generated enum contains the sampling policies supported by the current
built-in catalog. `Linear` is currently the only value and this enum is not a
third-party operation extensibility point.

## Values

| Name | Stable ID | Description |
| --- | ---: | --- |
| `Linear` | `178` | Samples using linear interpolation. |

## Applies to

Cerneala Prism catalog operations that sample images or intermediate surfaces.
