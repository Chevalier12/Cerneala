# Pen Class

## Definition
Namespace: `Cerneala.UI.Media`

Assembly/Project: `Cerneala`

Source: `UI/Media/Pen.cs`

Represents the brush and thickness used to stroke outlines in retained UI drawing APIs.

```csharp
public sealed record Pen
```

Inheritance:
`object` -> `Pen`

## Examples

Create a pen for drawing a shape outline:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls.Shapes;
using Cerneala.UI.Media;

Pen outline = new(
    new SolidColorBrush(DrawColor.Black),
    thickness: 2);

Rectangle rectangle = new()
{
    Stroke = outline.Brush,
    StrokeThickness = outline.Thickness
};
```

## Remarks

`Pen` stores a non-null `Brush` and a finite, non-negative stroke thickness. The constructor throws `ArgumentNullException` when `brush` is `null`, and throws `ArgumentOutOfRangeException` when `thickness` is negative, infinite, or `NaN`.

The type is immutable after construction. Consumers can pass the stored brush and thickness to shape, geometry, or rendering APIs that expose stroke settings separately.

## Constructors

| Signature | Description |
| --- | --- |
| `Pen(Brush brush, float thickness)` | Initializes a pen with the specified brush and finite, non-negative thickness. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Brush` | `Brush` | Gets the brush used for the stroke. |
| `Thickness` | `float` | Gets the stroke thickness. |

## Applies To

Cerneala retained UI media and rendering APIs.

## See Also

- `Cerneala.UI.Media.Brush`
- `Cerneala.UI.Media.SolidColorBrush`
- `Cerneala.UI.Controls.Shapes.Shape`
