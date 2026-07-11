# DrawBrushAlignmentY Enum

## Definition
Namespace: `Cerneala.Drawing`  
Assembly/Project: `Cerneala`  
Source: `Drawing/IDrawBrush.cs`

Controls vertical placement of content that does not fill its tile.

```csharp
public enum DrawBrushAlignmentY
```

## Examples
```csharp
var brush = new ImageBrush(image, alignmentY: DrawBrushAlignmentY.Bottom);
```

## Members
| Name | Description |
| --- | --- |
| `Top` | Aligns to the top edge. |
| `Center` | Centers vertically. |
| `Bottom` | Aligns to the bottom edge. |

## Applies to
`TileBrush` derivatives.
