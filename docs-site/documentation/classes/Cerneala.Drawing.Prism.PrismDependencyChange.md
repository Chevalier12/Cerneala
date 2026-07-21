# PrismDependencyChange Enum

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/PrismRendererOptions.cs`

Identifies pixel-affecting retained-key and raster-context fields that changed.

```csharp
[Flags]
public enum PrismDependencyChange
```

## Values

| Name | Description |
| --- | --- |
| `None` | No dependency change was classified. |
| `Owner` | The retained scope owner identity changed. |
| `Structure` | Stable node identity, structural version, or structural fingerprint changed. |
| `Values` | Prism value version or value fingerprint changed. |
| `Resources` | A referenced resource identity or version changed. |
| `RasterBounds` | Pixel-affecting raster bounds changed. |
| `SurfaceSize` | The required GPU surface width or height changed. |
| `LowerUi` | The lower-UI pixel generation used by a backdrop changed. |
| `PixelScale` | Logical-to-physical pixel scale changed. |
| `Transform` | The pixel-affecting transform changed. |
| `WorkingColorProfile` | The composition working color profile changed. |
| `OutputColorProfile` | The renderer output color profile changed. |
| `SurfaceFormat` | The retained raster surface format changed. |
| `Sampling` | The retained raster sampling policy changed. |
| `Capabilities` | The required Prism capability set changed. |
| `ShaderPackage` | The embedded shader package version changed. |

## Remarks

Values can be combined. Classification is performed only when
`PrismRendererOptions.EnableDevelopmentDiagnostics` is enabled. The renderer
compares immutable numeric and value fields; it does not serialize GPU
resources or store UI references in the diagnostic snapshot.

## Applies to

Cerneala Prism development diagnostics.

## See also

- `Cerneala.Drawing.Prism.PrismRendererOptions`
- `Cerneala.Drawing.Prism.PrismRendererDiagnostics`
- `Cerneala.Drawing.Prism.PrismCacheMissReason`
