# Shape Class

## Definition
Namespace: `Cerneala.UI.Controls.Shapes`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Shapes/Shape.cs`

Provides the abstract base class for retained UI controls that render geometry with fill, stroke, opacity, and transform settings.

```csharp
public abstract class Shape : Control
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Control` -> `Shape`

Derived:
`Ellipse`, `Line`, `Path`, `Polygon`, `Polyline`, `Rectangle`, `SvgPath`

## Examples

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

## Remarks

`Shape` centralizes the rendering behavior for geometry-backed controls. Derived classes provide geometry by implementing `ResolveGeometry`; the base class measures from geometry bounds and renders rectangle, ellipse, and path geometry through the retained drawing context.

`Fill`, `RenderTransform`, and `Opacity` affect rendering. `Stroke`, `StrokeThickness`, and `Geometry` affect both measure and rendering. Every `Stroke` change invalidates measure, like WPF's `Shape.Stroke`; only assigning or clearing `Stroke` changes the measured size, because it adds or removes the `StrokeThickness` padding. `StrokeThickness` must be finite and greater than or equal to zero. `Opacity` must be finite and between `0` and `1`. `RenderTransform` cannot be `null`.

The shape's `OpacityProperty` and `RenderTransformProperty` fields are compatibility aliases for the corresponding `UIElement` properties. Their CLR accessors remain available on `Shape`, but share the same values and retained rendering behavior as `UIElement`. Transforms use the inherited `RenderTransformOrigin` and compose with ancestor transforms; a rotated rectangle remains a rotated rectangle. Changing render-scope state does not require rebuilding the shape's local geometry commands.

Retained composition omits a shape whose effective opacity is zero. Local geometry recording is independent of opacity, so a shape initially at zero opacity can become visible without rebuilding its local commands. No geometry is recorded when the resolved geometry is `null`. Rectangle and ellipse geometry can render both fill and native `DrawPen` strokes. `Shape` strokes are centered on the geometry it renders, with one exception made by the derived classes: `Rectangle` and `Ellipse`, when `Geometry` is `null`, resolve their default geometry from the arranged bounds deflated by half the effective stroke thickness, so the outer edge of their stroke touches the arranged bounds (see `Rectangle` and `Ellipse`). An explicit `Geometry`, `Line`, `Polyline`, `Polygon`, `Path`, and `SvgPath` keep the stroke centered on the geometry. Path geometry emits a native fill and/or one native stroke using the same immutable path, preserving curves, arcs, contour boundaries, caps, and joins. A path with zero width or height skips filling but can still stroke.

Geometry is in the shape's own coordinate space: `(0, 0)` is the shape's arranged top-left, like a WPF `Shape`. `RenderGeometry` translates `RectangleGeometry`, `EllipseGeometry`, and `PathGeometry` coordinates by the arranged position; `SvgGeometry` retains its view-box-to-arranged-bounds mapping. For example, a `Rectangle` with `Geometry = new RectangleGeometry(new DrawRect(0, 0, 30, 20))` measures 30x20; when its parent arranges it at `(100, 50, 30, 20)`, it records `FillRectangle (100, 50, 30, 20)`. A geometry `(5, 6, 10, 8)` on a shape arranged at `(1, 2)` is drawn at `(6, 8, 10, 8)`. `ResolveGeometry` implementations therefore return geometry relative to the arranged top-left, not window coordinates.

`FillRule` defaults to `DrawFillRule.NonZero`. `EvenOdd` is also supported. It affects path and SVG filling, not strokes; undefined enum values are rejected. Changing the fill rule invalidates rendering.

## Fields

| Name | Description |
| --- | --- |
| `FillProperty` | Identifies the `Fill` UI property. |
| `StrokeProperty` | Identifies the `Stroke` UI property. |
| `StrokeThicknessProperty` | Identifies the `StrokeThickness` UI property. |
| `GeometryProperty` | Identifies the `Geometry` UI property. |
| `FillRuleProperty` | Identifies the `FillRule` UI property. |
| `RenderTransformProperty` | Compatibility alias for `UIElement.RenderTransformProperty`. |
| `OpacityProperty` | Compatibility alias for `UIElement.OpacityProperty`. |

## Properties

| Name | Description |
| --- | --- |
| `Fill` | Gets or sets the brush used to fill the shape interior. |
| `Stroke` | Gets or sets the brush used to draw the shape outline. Strokes are centered on the geometry, except for the default geometry of `Rectangle` and `Ellipse`, which is deflated by half the effective stroke thickness. |
| `StrokeThickness` | Gets or sets the outline thickness. The value must be finite and non-negative. |
| `Geometry` | Gets or sets explicit geometry used for measuring and rendering when a derived class resolves it. Its coordinates are relative to the shape's arranged top-left. |
| `FillRule` | Gets or sets the `DrawFillRule` used to fill path and SVG geometry. |
| `RenderTransform` | Gets or sets the inherited affine rendering transform. |
| `Opacity` | Gets or sets the alpha multiplier applied to rendered fill and stroke colors. |

## Methods

| Name | Description |
| --- | --- |
| `MeasureCore(MeasureContext)` | Measures the shape from the resolved geometry bounds plus stroke padding when `Stroke` is assigned. |
| `OnRender(RenderContext)` | Resolves and records geometry independently of retained opacity state. |
| `ResolveGeometry(LayoutRect)` | When implemented by a derived class, returns the geometry to measure or render for the supplied arranged bounds. The returned geometry is relative to the arranged top-left. |
| `RenderGeometry(RenderContext, Geometry)` | Renders supported geometry types using the current fill, stroke, thickness, opacity, and transform settings. |
| `ToDrawRect(LayoutRect)` | Converts a layout rectangle to a non-negative drawing rectangle. |

## Applies to

Cerneala retained UI shape controls.

## See also

- `Cerneala.UI.Controls.Shapes.Rectangle`
- `Cerneala.UI.Controls.Shapes.Ellipse`
- `Cerneala.UI.Controls.Shapes.Path`
- `Cerneala.UI.Media.Geometry`
