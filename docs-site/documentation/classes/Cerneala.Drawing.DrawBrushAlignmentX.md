# DrawBrushAlignmentX Enum

## Definition
Namespace: `Cerneala.Drawing`  
Assembly/Project: `Cerneala`  
Source: `Drawing/IDrawBrush.cs`

Controls horizontal placement of content that does not fill its tile.

```csharp
public enum DrawBrushAlignmentX
```

## Examples
```csharp
var brush = new ImageBrush(image, alignmentX: DrawBrushAlignmentX.Right);
```

## Members
| Name | Description |
| --- | --- |
| `Left` | Aligns to the left edge. |
| `Center` | Centers horizontally. |
| `Right` | Aligns to the right edge. |

## Applies to
`TileBrush` derivatives.
