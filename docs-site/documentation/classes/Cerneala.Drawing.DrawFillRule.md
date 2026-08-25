# DrawFillRule Enum

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawFillRule.cs`

Specifies how overlapping path contours contribute to a filled region.

```csharp
public enum DrawFillRule
```

## Examples

```csharp
drawing.FillPath(path, brush, DrawFillRule.EvenOdd);
```

## Remarks

The fill rule is retained as part of the draw command and tessellation-cache identity. `EvenOdd` alternates filled and unfilled regions at every contour crossing. `NonZero` uses signed winding, so contour direction affects holes.

## Fields

| Name | Description |
| --- | --- |
| `NonZero` | Fills points whose signed winding count is not zero. |
| `EvenOdd` | Fills points crossed by an odd number of contour edges. |

## Applies To

Typed and SVG path filling.

## See Also

- `DrawPath`
- `DrawingContext.FillPath`
