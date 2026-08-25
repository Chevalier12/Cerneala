# DrawLineSegment2D Struct

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawBatches.cs`

Describes one colored line segment within a `DrawLineBatch`.

```csharp
public readonly record struct DrawLineSegment2D
```

## Examples

```csharp
DrawLineSegment2D segment = new(
    new DrawPoint(0, 0),
    new DrawPoint(80, 24),
    Color.CornflowerBlue,
    thickness: 2);
```

## Remarks

Thickness must be positive and finite. A line batch expands each segment to shared triangle-mesh geometry once when the immutable batch is constructed.

## Constructors

| Name | Description |
| --- | --- |
| `DrawLineSegment2D(DrawPoint start, DrawPoint end, Color color, float thickness = 1)` | Creates a colored segment with validated thickness. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Start` | `DrawPoint` | Gets the start point. |
| `End` | `DrawPoint` | Gets the end point. |
| `Color` | `Color` | Gets the segment color. |
| `Thickness` | `float` | Gets the positive segment thickness. |

## Applies To

`DrawLineBatch`.
