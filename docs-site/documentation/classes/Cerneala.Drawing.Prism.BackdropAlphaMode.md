# BackdropAlphaMode Enum

## Definition
Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/BackdropAlphaMode.cs`

Specifies how alpha is encoded in a borrowed backdrop frame.

```csharp
public enum BackdropAlphaMode
```

## Values

| Name | Description |
| --- | --- |
| `Opaque` | Declares that the source is fully opaque. Current code does not ignore or overwrite the alpha channel for this value. |
| `Premultiplied` | Color channels are already multiplied by alpha. |
| `Straight` | Declares that color channels are independent of alpha. Current code does not convert straight alpha to premultiplied alpha for this value. |

## Remarks

`BackdropFrameMetadata` rejects an undefined value, but no current Prism code selects a different conversion by `AlphaMode` or by `BackdropPixelFormat`. The value describes borrowed pixels; it does not transfer ownership of the source.

## Applies to

Cerneala backdrop frame acquisition and Prism composition.

## See also

- `Cerneala.Drawing.Prism.BackdropFrameMetadata`
- `Cerneala.Drawing.Prism.BackdropPixelFormat`
