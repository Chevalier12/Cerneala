# VisualBrush Class

## Definition
Namespace: `Cerneala.UI.Media`  
Assembly/Project: `Cerneala`  
Source: `UI/Media/VisualBrush.cs`

Captures an existing `UIElement` as visual-only brush content.

```csharp
public sealed record VisualBrush : TileBrush
```

## Examples
```csharp
VisualBrush brush = new(existingElement, opacity: 0.6f);
targetShape.Fill = brush;
```

## Remarks
The source visual is not made interactive and does not receive input or focus through the brush. Capture uses a thread-local active-source set and throws a controlled `InvalidOperationException` when a visual brush graph cycles back to an active source. Source-generator markup keeps this type runtime-only because it needs a live element resolver.

## Constructors
| Name | Description |
| --- | --- |
| `VisualBrush(UIElement, DrawBrushStretch, DrawBrushAlignmentX, DrawBrushAlignmentY, DrawRect?, DrawRect?, DrawTileMode, float)` | Captures the supplied existing visual source. |

## Properties
| Name | Description |
| --- | --- |
| `Visual` | Existing source element. |
| `Kind` | Always `DrawBrushKind.Visual`. |
| `Stretch`, `AlignmentX`, `AlignmentY`, `Viewport`, `Viewbox`, `TileMode`, `Opacity` | Inherited brush settings. |

## Applies to
Retained UI rendering with a brush-capable backend.
