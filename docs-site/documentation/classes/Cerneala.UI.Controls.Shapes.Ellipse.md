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

When arranged with a positive width and height, this emits fill and stroke drawing commands for an ellipse inside the arranged bounds. Because the stroke is drawn inside the bounds, the outer edge of the `3` pixel stroke touches the bounds and nothing is drawn outside them.

## Remarks

`Ellipse` is a concrete `Shape` implementation. Its default geometry is an `EllipseGeometry` created from the current arranged bounds, deflated by half the effective stroke thickness (see Stroke placement). Negative arranged width or height values are clamped to zero before the drawing rectangle is created.

### Stroke placement

When `Geometry` is `null`, `Ellipse` draws its fill and its stroke on the arranged bounds deflated by half the effective stroke thickness, as WPF does. The outer edge of the stroke touches the arranged bounds, and nothing is drawn outside them.

The effective thickness `t` is `0` when `Stroke` is `null` or `StrokeThickness` is `0`. Otherwise `t` is `StrokeThickness`. A non-null `Stroke` that is fully transparent still counts, so it still reserves `t`. The ellipse rectangle is `(X + t/2, Y + t/2, max(0, Width - t), max(0, Height - t))`, where `X`, `Y`, `Width`, and `Height` are the arranged bounds.

For example, `new Ellipse { Width = 12, Height = 12, Stroke = new SolidColorBrush(Color.Black), StrokeThickness = 1 }` arranged at `(0, 0)` draws its ellipse in the rectangle `(0.5, 0.5, 11, 11)`, so the stroke covers `0` to `12` on both axes. With arranged bounds `(1, 2, 30, 20)` and `StrokeThickness = 3`, both the fill command and the stroke command use the rectangle `(2.5, 3.5, 27, 17)`. The fill is deflated by the same amount, so it never reaches under the outer half of the stroke.

When the stroke is thicker than the element, the deflated rectangle is empty and nothing is drawn. For example, `StrokeThickness = 40` on a `30` x `20` ellipse draws neither fill nor stroke.

This rule applies only to the default geometry. A supplied `Geometry` is drawn as it is, and its stroke stays centered on the geometry.

The inherited `Geometry` property overrides the default ellipse geometry. When `Geometry` is not `null`, `Ellipse` renders that supplied geometry instead of creating an `EllipseGeometry` from its bounds. Rendering is then handled by `Shape` according to the runtime geometry type.

Rendering is skipped when `Opacity` is `0`. Visible fill and stroke brushes use the native ellipse drawing commands; a stroke also requires positive `StrokeThickness`. Opacity and transforms use the shared `UIElement` properties and retained drawing state. Rotation and skew transform the ellipse itself, rather than drawing a new axis-aligned ellipse inside its transformed bounding box. The inherited `RenderTransformOrigin` and ancestor transforms apply.

Measurement is inherited from `Shape`. With a custom `Geometry`, desired size is based on the geometry bounds, plus `StrokeThickness` when `Stroke` is set. Without a custom geometry, the ellipse geometry used during measurement is based on empty bounds, so the control relies on arrangement or parent layout constraints for a visible drawing area. The stroke deflation applies to rendering only: an `Ellipse` with a `Stroke` and no explicit size still measures `(StrokeThickness, StrokeThickness)`.

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
| `Geometry` is `null` | Creates an `EllipseGeometry` from the arranged bounds deflated by half the effective stroke thickness and renders it through `Shape`. |
| `Geometry` is not `null` | Uses the supplied geometry instead of the default ellipse geometry. Its stroke stays centered on the geometry. |
| `Fill` is visible and the deflated rectangle has positive width and height | Emits a fill ellipse command for the deflated rectangle. |
| `Stroke` is visible, `StrokeThickness > 0`, and the deflated rectangle has positive width and height | Emits a stroke ellipse command for the deflated rectangle, so the outer edge of the stroke touches the arranged bounds. |
| `StrokeThickness` is larger than the arranged width or height | The deflated rectangle is empty; no fill or stroke command is emitted. |
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
