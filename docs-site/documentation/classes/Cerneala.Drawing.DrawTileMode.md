# DrawTileMode Enum

## Definition
Namespace: `Cerneala.Drawing`  
Assembly/Project: `Cerneala`  
Source: `Drawing/IDrawBrush.cs`

Controls repetition and mirrored repetition of tile content.

```csharp
public enum DrawTileMode
```

## Examples
```csharp
var brush = new ImageBrush(image, tileMode: DrawTileMode.FlipXY);
```

## Members
| Name | Description |
| --- | --- |
| `None` | Draws one fitted tile. |
| `Tile` | Repeats the tile. |
| `FlipX` | Alternates horizontal mirroring. |
| `FlipY` | Alternates vertical mirroring. |
| `FlipXY` | Alternates both axes. |

## Applies to
`TileBrush` derivatives.
