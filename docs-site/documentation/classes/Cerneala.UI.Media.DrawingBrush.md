# DrawingBrush Class

## Definition
Namespace: `Cerneala.UI.Media`  
Assembly/Project: `Cerneala`  
Source: `UI/Media/DrawingBrush.cs`

Uses an immutable `DrawCommand` snapshot as repeatable brush content.

```csharp
public sealed record DrawingBrush : TileBrush
```

## Examples
```csharp
DrawingBrush brush = new(
    [DrawCommand.FillRectangle(new DrawRect(0, 0, 20, 20), Color.White)],
    new DrawRect(0, 0, 20, 20),
    tileMode: DrawTileMode.Tile);
```

## Remarks
The constructor copies the command sequence, validates positive finite content bounds, and uses structural equality. Content cannot access `UIElement` or layout services. The MonoGame backend rasterizes the command snapshot to a device-owned render target.

## Constructors
| Name | Description |
| --- | --- |
| `DrawingBrush(IEnumerable<DrawCommand>, DrawRect, DrawBrushStretch, DrawBrushAlignmentX, DrawBrushAlignmentY, DrawRect?, DrawRect?, DrawTileMode, float)` | Creates a brush from immutable command content and content bounds. |

## Properties
| Name | Description |
| --- | --- |
| `Commands` | Read-only copied command list. |
| `ContentBounds` | Source coordinate bounds. |
| `Kind` | Always `DrawBrushKind.Drawing`. |
| `Stretch`, `AlignmentX`, `AlignmentY`, `Viewport`, `Viewbox`, `TileMode`, `Opacity` | Inherited brush settings. |

## Applies to
`DrawingContext` and drawing backends with brush support.
