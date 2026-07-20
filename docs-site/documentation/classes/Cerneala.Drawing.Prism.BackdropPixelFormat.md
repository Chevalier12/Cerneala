# BackdropPixelFormat Enum

## Definition
Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/BackdropPixelFormat.cs`

Identifies the backend-neutral pixel format of a borrowed backdrop frame.

```csharp
public enum BackdropPixelFormat
```

## Values

| Name | Description |
| --- | --- |
| `Rgba8Unorm` | Four normalized 8-bit channels in RGBA order. |
| `Bgra8Unorm` | Four normalized 8-bit channels in BGRA order. |
| `Rgba16Float` | Four 16-bit floating-point channels in RGBA order. |

## Remarks

The enum carries raster metadata only. Backend-specific texture objects and ownership APIs are intentionally absent from the generic backdrop contract.

## Applies to

Cerneala backdrop frame acquisition and Prism color conversion.

## See also

- `Cerneala.Drawing.Prism.BackdropAlphaMode`
- `Cerneala.Drawing.Prism.BackdropFrameMetadata`
