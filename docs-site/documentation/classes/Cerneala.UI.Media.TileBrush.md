# TileBrush Class

## Definition
Namespace: `Cerneala.UI.Media`  
Assembly/Project: `Cerneala`  
Source: `UI/Media/TileBrush.cs`

Base class for brushes that fit, align, crop, and repeat source content.

```csharp
public abstract record TileBrush : Brush
```

## Examples
```csharp
var brush = new ImageBrush(
    image,
    stretch: DrawBrushStretch.Uniform,
    tileMode: DrawTileMode.Tile);
```

## Remarks
All tile rectangles must be finite and strictly positive. Defaults are `Fill`, centered alignment, no viewport/viewbox, `None` tile mode, and opacity `1`.

## Constructors
| Name | Description |
| --- | --- |
| `TileBrush(DrawBrushStretch, DrawBrushAlignmentX, DrawBrushAlignmentY, DrawRect?, DrawRect?, DrawTileMode, float)` | Protected constructor for tile brush implementations. |

## Properties
| Name | Description |
| --- | --- |
| `Stretch` | Source fitting policy. |
| `AlignmentX` | Horizontal alignment. |
| `AlignmentY` | Vertical alignment. |
| `Viewport` | Optional destination tile rectangle. |
| `Viewbox` | Optional source rectangle. |
| `TileMode` | Repetition and mirroring policy. |
| `Opacity` | Inherited brush opacity. |

## Applies to
`ImageBrush`, `DrawingBrush`, and `VisualBrush`.

## See also
- `Cerneala.Drawing.DrawBrushStretch`
- `Cerneala.Drawing.DrawTileMode`
