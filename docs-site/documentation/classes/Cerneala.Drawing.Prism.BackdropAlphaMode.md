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
| `Opaque` | The source is fully opaque and its alpha channel is ignored. |
| `Premultiplied` | Color channels are already multiplied by alpha. |
| `Straight` | Color channels are independent of alpha. |

## Remarks

Prism uses this value with `BackdropPixelFormat` and `PrismColorProfile` to select one observable conversion policy for the backdrop input. The value describes borrowed pixels; it does not transfer ownership of the source.

## Applies to

Cerneala backdrop frame acquisition and Prism composition.

## See also

- `Cerneala.Drawing.Prism.BackdropFrameMetadata`
- `Cerneala.Drawing.Prism.BackdropPixelFormat`
