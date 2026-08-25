# DrawCornerRadius Struct

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawCornerRadius.cs`

Stores one circular radius for each corner of a rounded rectangle.

```csharp
public readonly record struct DrawCornerRadius
```

## Examples

```csharp
DrawCornerRadius radii = new(
    topLeft: 12,
    topRight: 4,
    bottomRight: 16,
    bottomLeft: 0);

drawing.FillRoundedRectangle(
    new DrawRect(8, 8, 120, 48),
    radii,
    Color.CornflowerBlue);
```

## Remarks

Each radius must be finite and non-negative. `Normalize` applies one proportional scale to all four values when adjacent radii would exceed an edge. This preserves their ratios and guarantees that the normalized top, bottom, left, and right pairs fit the supplied bounds.

## Constructors

| Name | Description |
| --- | --- |
| `DrawCornerRadius(float uniformRadius)` | Uses the same radius for all corners. |
| `DrawCornerRadius(float topLeft, float topRight, float bottomRight, float bottomLeft)` | Uses independent clockwise radii beginning at the top-left corner. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `TopLeft` | `float` | Gets the top-left radius. |
| `TopRight` | `float` | Gets the top-right radius. |
| `BottomRight` | `float` | Gets the bottom-right radius. |
| `BottomLeft` | `float` | Gets the bottom-left radius. |

## Methods

| Name | Returns | Description |
| --- | --- | --- |
| `Normalize(DrawRect)` | `DrawCornerRadius` | Returns proportionally scaled radii that fit every edge of the supplied bounds. |

## Applies To

Rounded-rectangle commands and reusable rounded-rectangle paths.

## See Also

- `DrawingContext.FillRoundedRectangle`
- `DrawingContext.DrawRoundedRectangle`
- `DrawPathFactory.RoundedRectangle`
