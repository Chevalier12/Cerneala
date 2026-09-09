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
    StrokeThickness = 2,
    RadiusX = 12,
    RadiusY = 6
};
```

```xml
<Rectangle Width="120" Height="60" RadiusX="12" RadiusY="6"
           Fill="White" Stroke="Black" StrokeThickness="2" />
```

## Remarks

`Rectangle` fits its arranged bounds. `RadiusX` and `RadiusY` are the horizontal and vertical radii of its elliptical corners. Both default to `0` and must be finite and non-negative. Rendering limits each radius independently to half the corresponding dimension without changing the property value. If either effective radius is zero, the corners are square. Radius changes affect rendering, not natural measurement.

Rounded rectangles use one cached immutable path with four elliptical arcs for both fill and stroke. Moving the control preserves that geometry; a changed size or effective radius rebuilds it. Square rectangles retain the native rectangle drawing path. An explicit inherited `Geometry` takes precedence and is not modified by the radius properties.

Rendering is implemented by `Shape` and supports its fill and stroke brushes. A positive `StrokeThickness` is required for a stroke. Opacity and affine transforms use the shared `UIElement` retained rendering path, including its transform origin. Rotated corners and outlines retain their geometry.

Default measuring uses an empty rectangle because the default geometry is based on `LayoutRect.Empty` during measure. Set `Geometry` when the shape should report a natural desired size; its bounds are used for measuring, with `StrokeThickness` added as padding when `Stroke` is assigned.

## Constructors

| Name | Description |
| --- | --- |
| `Rectangle()` | Initializes a new `Rectangle` instance. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `RadiusXProperty` | `UiProperty<float>` | Identifies the horizontal corner radius; affects render. |
| `RadiusYProperty` | `UiProperty<float>` | Identifies the vertical corner radius; affects render. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `RadiusX` | `float` | `0` | Gets or sets the finite, non-negative horizontal corner radius. |
| `RadiusY` | `float` | `0` | Gets or sets the finite, non-negative vertical corner radius. |

## Relevant Inherited Fields

| Name | Type | Declared By | Description |
| --- | --- | --- | --- |
| `FillProperty` | `UiProperty<Brush?>` | `Shape` | Identifies the `Fill` UI property. Default value is `null`; changes affect render. |
| `StrokeProperty` | `UiProperty<Brush?>` | `Shape` | Identifies the `Stroke` UI property. Default value is `null`; changes affect render. |
| `StrokeThicknessProperty` | `UiProperty<float>` | `Shape` | Identifies the `StrokeThickness` UI property. Default value is `1`; values must be finite and non-negative. Changes affect measure and render. |
| `GeometryProperty` | `UiProperty<Geometry?>` | `Shape` | Identifies the `Geometry` UI property. Default value is `null`; changes affect measure and render. |
| `RenderTransformProperty` | `UiProperty<Transform>` | `Shape` | Compatibility alias for the inherited `UIElement` property. |
| `OpacityProperty` | `UiProperty<float>` | `Shape` | Compatibility alias for the inherited `UIElement` property. |

## Relevant Inherited Properties

| Name | Type | Declared By | Description |
| --- | --- | --- | --- |
| `Fill` | `Brush?` | `Shape` | Gets or sets the brush used to fill the geometry. |
| `Stroke` | `Brush?` | `Shape` | Gets or sets the brush used to stroke the geometry. |
| `StrokeThickness` | `float` | `Shape` | Gets or sets the stroke thickness. The value must be finite and greater than or equal to zero. |
| `Geometry` | `Geometry?` | `Shape` | Gets or sets explicit geometry, overriding the arranged rectangle and radii. |
| `RenderTransform` | `Transform` | `Shape` | Gets or sets the transform applied to rendered shape coordinates. |
| `Opacity` | `float` | `Shape` | Gets or sets the opacity multiplier applied to emitted fill and stroke colors. |

## Relevant Inherited Methods

| Name | Return Type | Declared By | Description |
| --- | --- | --- | --- |
| `Measure(MeasureContext)` | `LayoutSize` | `UIElement` | Measures the rectangle from resolved geometry bounds, plus stroke padding when `Stroke` is assigned. |
| `Arrange(ArrangeContext)` | `LayoutRect` | `UIElement` | Arranges the rectangle. Default rectangle geometry is created from the arranged bounds. |

## Rendering Behavior

| Condition | Result |
| --- | --- |
| `Geometry` is `null` | Fits the arranged bounds, using an elliptical-corner path when both effective radii are positive. |
| `Geometry` is not `null` | Renders the supplied geometry through `Shape.RenderGeometry`. |
| `Fill` is visible and bounds are positive | Fills the rectangle or rounded path. |
| `Stroke` is visible, `StrokeThickness > 0`, and bounds are positive | Strokes the rectangle or rounded path. |
| `Opacity <= 0` | Emits no drawing commands. |

## Applies To

Cerneala retained UI shape controls and rendering infrastructure.

## See Also

- `Cerneala.UI.Controls.Shapes.Shape`
- `Cerneala.UI.Controls.Shapes.Ellipse`
- `Cerneala.UI.Media.RectangleGeometry`
- `Cerneala.UI.Media.SolidColorBrush`
- `Cerneala.UI.Layout.LayoutRect`
