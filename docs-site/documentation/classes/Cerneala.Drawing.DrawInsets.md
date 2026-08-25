# DrawInsets Struct

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawImageOptions.cs`

Stores the four source-image border widths used by nine-slice drawing.

```csharp
public readonly record struct DrawInsets
```

## Examples

```csharp
DrawInsets borders = new(left: 6, top: 8, right: 6, bottom: 8);
drawing.DrawNineSlice(image, destination, borders);
```

## Remarks

Insets use source-image pixel units and must be finite and non-negative. Horizontal and vertical inset pairs must fit the selected source region. When a destination is smaller than an opposing inset pair, Cerneala scales that pair proportionally so the nine regions remain deterministic and do not cross.

## Constructors

| Name | Description |
| --- | --- |
| `DrawInsets(float uniform)` | Uses one inset on every side. |
| `DrawInsets(float left, float top, float right, float bottom)` | Uses independent left, top, right, and bottom source borders. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Left` | `float` | Gets the left inset. |
| `Top` | `float` | Gets the top inset. |
| `Right` | `float` | Gets the right inset. |
| `Bottom` | `float` | Gets the bottom inset. |

## Applies To

`DrawCommand.DrawNineSlice`, `DrawingContext.DrawNineSlice`, and `RenderSurface2DFrame.DrawNineSlice`.
