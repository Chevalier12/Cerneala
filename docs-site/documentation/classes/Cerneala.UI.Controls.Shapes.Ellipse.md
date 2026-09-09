# Ellipse Class

## Definition
Namespace: `Cerneala.UI.Controls.Shapes`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Shapes/Ellipse.cs`

Draws an ellipse shape using the arranged bounds unless a custom `Geometry` is supplied.

```csharp
public sealed class Ellipse : Shape
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Control` -> `Shape` -> `Ellipse`

## Examples

Create an ellipse with a fill and stroke:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls.Shapes;
using Cerneala.UI.Media;

Ellipse ellipse = new()
{
    Fill = new SolidColorBrush(Color.White),
    Stroke = new SolidColorBrush(Color.Black),
    StrokeThickness = 3
};
```

When arranged with a positive width and height, this emits fill and stroke drawing commands for an ellipse inside the arranged bounds.

## Remarks

`Ellipse` is a concrete `Shape` implementation. Its default geometry is an `EllipseGeometry` created from the current arranged bounds. Negative arranged width or height values are clamped to zero before the drawing rectangle is created.

The inherited `Geometry` property overrides the default ellipse geometry. When `Geometry` is not `null`, `Ellipse` renders that supplied geometry instead of creating an `EllipseGeometry` from its bounds. Rendering is then handled by `Shape` according to the runtime geometry type.

Rendering is skipped when `Opacity` is `0`. Visible fill and stroke brushes use the native ellipse drawing commands; a stroke also requires positive `StrokeThickness`. Opacity and transforms use the shared `UIElement` properties and retained drawing state. Rotation and skew transform the ellipse itself, rather than drawing a new axis-aligned ellipse inside its transformed bounding box. The inherited `RenderTransformOrigin` and ancestor transforms apply.

Measurement is inherited from `Shape`. With a custom `Geometry`, desired size is based on the geometry bounds, plus `StrokeThickness` when `Stroke` is set. Without a custom geometry, the ellipse geometry used during measurement is based on empty bounds, so the control relies on arrangement or parent layout constraints for a visible drawing area.

`Ellipse` declares no additional public properties or methods of its own. Its public shape API is inherited from `Shape`.

## Constructors

| Name | Description |
| --- | --- |
| `Ellipse()` | Initializes a new `Ellipse` instance. |

## Inherited Fields

| Name | Type | Declared By | Description |
| --- | --- | --- | --- |
| `FillProperty` | `UiProperty<Brush?>` | `Shape` | Identifies the `Fill` UI property. Defaults to `null` and affects render. |
| `StrokeProperty` | `UiProperty<Brush?>` | `Shape` | Identifies the `Stroke` UI property. Defaults to `null` and affects render. |
| `StrokeThicknessProperty` | `UiProperty<float>` | `Shape` | Identifies the `StrokeThickness` UI property. Defaults to `1`, affects measure and render, and accepts only finite non-negative values. |
| `GeometryProperty` | `UiProperty<Geometry?>` | `Shape` | Identifies the `Geometry` UI property. Defaults to `null` and affects measure and render. |
| `RenderTransformProperty` | `UiProperty<Transform>` | `Shape` | Compatibility alias for `UIElement.RenderTransformProperty`. |
| `OpacityProperty` | `UiProperty<float>` | `Shape` | Compatibility alias for `UIElement.OpacityProperty`. |

## Inherited Properties

| Name | Type | Declared By | Default | Description |
| --- | --- | --- | --- | --- |
| `Fill` | `Brush?` | `Shape` | `null` | Gets or sets the fill brush used for the ellipse interior. |
| `Stroke` | `Brush?` | `Shape` | `null` | Gets or sets the stroke brush used for the ellipse outline. |
| `StrokeThickness` | `float` | `Shape` | `1` | Gets or sets the stroke thickness. Values must be finite and non-negative. |
| `Geometry` | `Geometry?` | `Shape` | `null` | Gets or sets an optional custom geometry. When set, it replaces the default bounds-based ellipse geometry. |
| `RenderTransform` | `Transform` | `Shape` | `Transform.Identity` | Gets or sets the inherited affine rendering transform. |
| `Opacity` | `float` | `Shape` | `1` | Gets or sets the opacity multiplier applied to fill and stroke alpha. Values must be finite and between `0` and `1`. |

## Methods

`Ellipse` does not declare public methods. It overrides the protected `ResolveGeometry(LayoutRect arrangedBounds)` hook from `Shape`.

## Rendering Behavior

| Condition | Result |
| --- | --- |
| `Geometry` is `null` | Creates an `EllipseGeometry` from the arranged bounds and renders it through `Shape`. |
| `Geometry` is not `null` | Uses the supplied geometry instead of the default ellipse geometry. |
| `Fill` is visible and bounds are positive | Emits a fill ellipse command for an `EllipseGeometry`. |
| `Stroke` is visible, `StrokeThickness > 0`, and bounds are positive | Emits a stroke ellipse command for an `EllipseGeometry`. |
| `Opacity <= 0` | Emits no drawing commands. |

## Layout Behavior

| Input | Effect |
| --- | --- |
| Custom `Geometry` | Desired size uses the custom geometry bounds, plus stroke padding when `Stroke` is present. |
| No custom `Geometry` | Measurement uses an empty-bounds ellipse geometry; visible size comes from arrangement by the parent layout. |
| `StrokeThickness` | Adds to measured width and height when `Stroke` is set. |

## Applies To

Cerneala retained UI controls in the `Cerneala` project.

## See Also

- `UI/Controls/Shapes/Ellipse.cs`
- `UI/Controls/Shapes/Shape.cs`
- `UI/Controls/Shapes/Rectangle.cs`
- `UI/Controls/Shapes/Path.cs`
- `UI/Media/EllipseGeometry.cs`
