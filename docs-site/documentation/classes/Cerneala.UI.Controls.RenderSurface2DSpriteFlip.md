# RenderSurface2DSpriteFlip Enum

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `Drawing/MonoGame/RenderSurface2DFrame.cs`

Specifies sprite-axis mirroring for `RenderSurface2DFrame.DrawSprite`.

```csharp
[Flags]
public enum RenderSurface2DSpriteFlip
```

## Fields
| Name | Value | Description |
| --- | --- | --- |
| `None` | `0` | Draws the sprite without mirroring. |
| `Horizontal` | `1` | Mirrors the sprite horizontally. |
| `Vertical` | `2` | Mirrors the sprite vertically. |

## Remarks
Combine `Horizontal` and `Vertical` with the bitwise OR operator to mirror both axes.

## Applies To
Project: `Cerneala`

Backend: MonoGame/WindowsDX retained rendering.

## See Also
- `RenderSurface2DFrame`
- `RenderSurface2DFrame.DrawSprite`
