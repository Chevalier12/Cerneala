# DrawImageFlip Enum

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawImageFlip.cs`

Specifies image-axis mirroring for `DrawImage` commands.

```csharp
[Flags]
public enum DrawImageFlip
```

## Fields

| Name | Value | Description |
| --- | --- | --- |
| `None` | `0` | Draws the image without mirroring. |
| `Horizontal` | `1` | Mirrors the image horizontally. |
| `Vertical` | `2` | Mirrors the image vertically. |

## Remarks

Combine `Horizontal` and `Vertical` with the bitwise OR operator to mirror both axes. The advanced `DrawImage` overloads on `DrawCommand`, `DrawingContext`, and `RenderSurface2DFrame` use this backend-neutral enum.

## Applies To

Cerneala drawing command recording and rendering paths.

## See Also

- `DrawCommand`
- `DrawingContext`
- `RenderSurface2DFrame`
