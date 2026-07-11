# Rectangle Class

## Definition
Namespace: `Cerneala.UI.Controls.Shapes`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Shapes/Rectangle.cs`

Draws a rectangular retained UI shape using the arranged bounds or an explicitly supplied geometry.

```csharp
public sealed class Rectangle : Shape
```

Inheritance:
`Object` -> `UiObject` -> `UIElement` -> `Control` -> `Shape` -> `Rectangle`

## Examples

Create a rectangle with a solid fill and stroke:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls.Shapes;
using Cerneala.UI.Media;

Rectangle rectangle = new()
{
    Fill = new SolidColorBrush(Color.White),
    Stroke = new SolidColorBrush(Color.Black),
    StrokeThickness = 2
};
```

When arranged, the rectangle renders over its arranged bounds. For example, arranging the control at `(1, 2, 30, 20)` produces a fill rectangle for that same draw rectangle, followed by a rectangle stroke when `Stroke` is visible and `StrokeThickness` is greater than zero.

## Remarks

`Rectangle` is the rectangular specialization of `Shape`. It does not declare additional public properties. Its core behavior is `ResolveGeometry(LayoutRect arrangedBounds)`: if `Geometry` is `null`, the control creates a `RectangleGeometry` from the arranged bounds; otherwise, it renders the explicit `Geometry` inherited from `Shape`.

Rendering is implemented by `Shape`. A visible solid `Fill` emits a fill rectangle command for rectangular geometry. A visible solid `Stroke` with positive `StrokeThickness` emits a rectangle stroke command. `Opacity` is applied to emitted colors, and `Opacity <= 0` skips rendering. `RenderTransform` is applied to the rendered geometry bounds.

Default measuring uses an empty rectangle because the default geometry is based on `LayoutRect.Empty` during measure. Set `Geometry` when the shape should report a natural desired size; its bounds are used for measuring, with solid stroke thickness added as padding.

## Constructors

| Name | Description |
| --- | --- |
| `Rectangle()` | Initializes a new `Rectangle` instance. |

## Relevant Inherited Fields

| Name | Type | Declared By | Description |
| --- | --- | --- | --- |
| `FillProperty` | `UiProperty<Brush?>` | `Shape` | Identifies the `Fill` UI property. Default value is `null`; changes affect render. |
| `StrokeProperty` | `UiProperty<Brush?>` | `Shape` | Identifies the `Stroke` UI property. Default value is `null`; changes affect render. |
| `StrokeThicknessProperty` | `UiProperty<float>` | `Shape` | Identifies the `StrokeThickness` UI property. Default value is `1`; values must be finite and non-negative. Changes affect measure and render. |
| `GeometryProperty` | `UiProperty<Geometry?>` | `Shape` | Identifies the `Geometry` UI property. Default value is `null`; changes affect measure and render. |
| `RenderTransformProperty` | `UiProperty<Transform>` | `Shape` | Identifies the shape-specific `RenderTransform` UI property. Default value is `Transform.Identity`; values cannot be `null`. Changes affect render. |
| `OpacityProperty` | `UiProperty<float>` | `Shape` | Identifies the shape-specific `Opacity` UI property. Default value is `1`; values must be finite and between `0` and `1`. Changes affect render. |
| `ShadowProperty` | `UiProperty<ShadowEffect?>` | `Shape` | Identifies the `Shadow` UI property. Default value is `null`; changes affect render. |

## Relevant Inherited Properties

| Name | Type | Declared By | Description |
| --- | --- | --- | --- |
| `Fill` | `Brush?` | `Shape` | Gets or sets the brush used to fill the geometry. Rendering uses the brush's `SolidColor` value when present. |
| `Stroke` | `Brush?` | `Shape` | Gets or sets the brush used to stroke the geometry. Rendering uses the brush's `SolidColor` value when present. |
| `StrokeThickness` | `float` | `Shape` | Gets or sets the stroke thickness. The value must be finite and greater than or equal to zero. |
| `Geometry` | `Geometry?` | `Shape` | Gets or sets an explicit geometry. When `null`, `Rectangle` creates a `RectangleGeometry` from its arranged bounds. |
| `RenderTransform` | `Transform` | `Shape` | Gets or sets the transform applied to rendered shape coordinates. |
| `Opacity` | `float` | `Shape` | Gets or sets the opacity multiplier applied to emitted fill and stroke colors. |
| `Shadow` | `ShadowEffect?` | `Shape` | Gets or sets the shadow effect value stored on the shape. |

## Relevant Inherited Methods

| Name | Return Type | Declared By | Description |
| --- | --- | --- | --- |
| `Measure(MeasureContext)` | `LayoutSize` | `UIElement` | Measures the rectangle. Shape measurement is based on resolved geometry bounds and solid stroke padding. |
| `Arrange(ArrangeContext)` | `LayoutRect` | `UIElement` | Arranges the rectangle. Default rectangle geometry is created from the arranged bounds. |

## Rendering Behavior

| Condition | Result |
| --- | --- |
| `Geometry` is `null` | Uses a `RectangleGeometry` created from the arranged bounds. Width and height are clamped to zero or greater. |
| `Geometry` is not `null` | Renders the supplied geometry through `Shape.RenderGeometry`. |
| `Fill.SolidColor` has visible alpha and bounds are positive | Emits a filled rectangle command for rectangular geometry. |
| `Stroke.SolidColor` has visible alpha, `StrokeThickness > 0`, and bounds are positive | Emits a rectangle stroke command for rectangular geometry. |
| `Opacity <= 0` | Emits no drawing commands. |

## Applies To

Cerneala retained UI shape controls and rendering infrastructure.

## See Also

- `Cerneala.UI.Controls.Shapes.Shape`
- `Cerneala.UI.Controls.Shapes.Ellipse`
- `Cerneala.UI.Media.RectangleGeometry`
- `Cerneala.UI.Media.SolidColorBrush`
- `Cerneala.UI.Layout.LayoutRect`
