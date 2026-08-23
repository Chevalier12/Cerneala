# PrismImage Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/PrismImage.cs`

Represents an `IDrawImage` whose source is processed lazily by a Prism pipeline when drawn.

```csharp
public sealed class PrismImage : IDrawImage
```

## Remarks

`PrismImage` owns no caller-visible GPU resource and does not require disposal. It keeps the original source and the pipeline description, synchronizes a native `PrismInstance` only when needed, and uses a stable cache-owner token for repeated draws.

Operation and pipeline mutations increment the recorded visual content version. Retained `RenderSurface2D` sessions therefore redraw the affected Prism scope instead of treating a changed effect as an identical frame.

Nested `PrismImage` values are supported and compose as nested native Prism scopes.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Source` | `IDrawImage` | Gets the unprocessed source image. |
| `Pipeline` | `PrismPipeline` | Gets the live pipeline used for subsequent draws. |
| `Width` | `int` | Gets the source image width. |
| `Height` | `int` | Gets the source image height. |

## See Also

- `IDrawImage`
- `Prism`
- `PrismPipeline`
- `RenderSurface2DFrame.DrawImage`
