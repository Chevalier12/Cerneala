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

Insets use source-image pixel units and must be finite and non-negative. Horizontal and vertical inset pairs must fit the selected source region; otherwise building the nine-slice command throws `ArgumentException`. The `DrawInsets` constructor does not check this, because it does not know the source region. When a destination is smaller than an opposing inset pair, Cerneala scales that pair proportionally so the nine regions remain deterministic and do not cross.

`DrawImageFlip` mirrors the whole nine-slice, borders included. Each source border keeps its own width on the side the flip moves it to. For example, `new DrawInsets(2, 0, 6, 0)` drawn into a 16-pixel-wide destination has column edges at x = 0, 2, 10, 16; with `DrawImageFlip.Horizontal` the 6-pixel right border is drawn in a 6-pixel column on the left, and the edges are x = 0, 6, 14, 16. `DrawImageFlip.Vertical` does the same with `Top` and `Bottom`.

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
