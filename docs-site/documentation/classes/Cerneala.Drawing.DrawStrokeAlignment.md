# DrawStrokeAlignment Enum

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawStrokeStyle.cs`

Specifies where a stroke lies relative to a closed contour.

```csharp
public enum DrawStrokeAlignment
```

## Examples

```csharp
DrawPen outsidePen = new(
    borderBrush,
    thickness: 2,
    new DrawStrokeStyle(alignment: DrawStrokeAlignment.Outside));
```

## Remarks

The tessellator derives the interior side from the closed contour winding. Open paths do not have an interior and therefore treat all three values as `Center`.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `Inside` | `0` | Places the full stroke thickness inside a closed contour. |
| `Center` | `1` | Splits the stroke thickness equally across the contour. |
| `Outside` | `2` | Places the full stroke thickness outside a closed contour. |

## Applies To

`DrawStrokeStyle.Alignment` on closed typed paths, rectangles, and ellipses.

## See Also

- `DrawStrokeStyle`
- `DrawPen`
