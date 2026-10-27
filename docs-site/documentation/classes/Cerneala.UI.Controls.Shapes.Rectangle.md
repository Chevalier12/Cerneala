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

`Rectangle` fits its arranged bounds. `RadiusX` and `RadiusY` are the horizontal and vertical radii of its elliptical corners. Both default to `0` and must be finite and non-negative. Rendering limits each radius independently to half the corresponding dimension of the stroke-deflated rectangle (see Stroke placement) without changing the property value. If either effective radius is zero, the corners are square. Radius changes affect rendering, not natural measurement.

Rounded rectangles use one cached immutable path with four elliptical arcs for both fill and stroke. Moving the control preserves that geometry; a changed size or effective radius rebuilds it. Square rectangles retain the native rectangle drawing path. An explicit inherited `Geometry` takes precedence and is not modified by the radius properties.

Rendering is implemented by `Shape` and supports its fill and stroke brushes. A positive `StrokeThickness` is required for a stroke. Opacity and affine transforms use the shared `UIElement` retained rendering path, including its transform origin. Rotated corners and outlines retain their geometry.

### Stroke placement

When `Geometry` is `null`, `Rectangle` draws its fill and its stroke on the arranged bounds deflated by half the effective stroke thickness, as WPF does. The outer edge of the stroke touches the arranged bounds, and nothing is drawn outside them.

The effective thickness `t` is `0` when `Stroke` is `null` or `StrokeThickness` is `0`. Otherwise `t` is `StrokeThickness`. A non-null `Stroke` that is fully transparent still counts, so it still reserves `t`. The drawn rectangle is `(X + t/2, Y + t/2, max(0, Width - t), max(0, Height - t))`, where `X`, `Y`, `Width`, and `Height` are the arranged bounds.

For example, arranged bounds `(1, 2, 30, 20)` with `StrokeThickness = 3` draw the fill and the stroke in the rectangle `(2.5, 3.5, 27, 17)`. The stroke covers `1` to `31` horizontally and `2` to `22` vertically.

`RadiusX` and `RadiusY` keep their values and apply to the deflated rectangle. Each effective radius is limited to half the width and half the height of the deflated rectangle, not of the arranged bounds. For example, `RadiusX = 12` on arranged bounds `30` x `20` with `StrokeThickness = 2` uses the deflated rectangle `28` x `18`, so the effective `RadiusX` is `12` and the effective `RadiusY` for `RadiusY = 12` is `9`.

When the stroke is thicker than the element, the deflated rectangle is empty and nothing is drawn. For example, `StrokeThickness = 40` on a `30` x `20` rectangle draws neither fill nor stroke.

This rule applies only to the default geometry. A supplied `Geometry` is drawn as it is, and its stroke stays centered on the geometry.

Default measuring uses an empty rectangle because the default geometry is based on `LayoutRect.Empty` during measure. Set `Geometry` when the shape should report a natural desired size; its bounds are used for measuring, with `StrokeThickness` added as padding when `Stroke` is assigned. The stroke deflation applies to rendering only: a `Rectangle` with a `Stroke` and no explicit size still measures `(StrokeThickness, StrokeThickness)`.

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
| `StrokeProperty` | `UiProperty<Brush?>` | `Shape` | Identifies the `Stroke` UI property. Default value is `null`; changes affect measure and render. |
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
| `Geometry` is `null` | Fits the arranged bounds deflated by half the effective stroke thickness, using an elliptical-corner path when both effective radii are positive. |
| `Geometry` is not `null` | Renders the supplied geometry through `Shape.RenderGeometry`. Its stroke stays centered on the geometry. |
| `Fill` is visible and the deflated rectangle has positive width and height | Fills the deflated rectangle or rounded path. |
| `Stroke` is visible, `StrokeThickness > 0`, and the deflated rectangle has positive width and height | Strokes the deflated rectangle or rounded path, so the outer edge of the stroke touches the arranged bounds. |
| `StrokeThickness` is larger than the arranged width or height | The deflated rectangle is empty; no fill or stroke command is emitted. |
| `Opacity <= 0` | Emits no drawing commands. |

## Applies To

Cerneala retained UI shape controls and rendering infrastructure.

## See Also

- `Cerneala.UI.Controls.Shapes.Shape`
- `Cerneala.UI.Controls.Shapes.Ellipse`
- `Cerneala.UI.Media.RectangleGeometry`
- `Cerneala.UI.Media.SolidColorBrush`
- `Cerneala.UI.Layout.LayoutRect`
