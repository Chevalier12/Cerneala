# Shape Class

## Definition
Namespace: `Cerneala.UI.Controls.Shapes`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Shapes/Shape.cs`

Provides the abstract base class for retained UI controls that render geometry with fill, stroke, opacity, transform, and shadow settings.

```csharp
public abstract class Shape : Control
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Control` -> `Shape`

Derived:
`Ellipse`, `Path`, `Rectangle`

## Examples

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls.Shapes;
using Cerneala.UI.Media;

Rectangle rectangle = new()
{
    Fill = new SolidColorBrush(DrawColor.White),
    Stroke = new SolidColorBrush(DrawColor.Black),
    StrokeThickness = 2
};
```

## Remarks

`Shape` centralizes the rendering behavior for geometry-backed controls. Derived classes provide geometry by implementing `ResolveGeometry`; the base class measures from geometry bounds and renders rectangle, ellipse, and path geometry through the retained drawing context.

`Fill`, `Stroke`, `RenderTransform`, `Opacity`, and `Shadow` affect rendering. `StrokeThickness` and `Geometry` affect both measure and rendering. `StrokeThickness` must be finite and greater than or equal to zero. `Opacity` must be finite and between `0` and `1`. `RenderTransform` cannot be `null`.

Rendering exits early when `Opacity` is `0` or less, or when the resolved geometry is `null`. Rectangle and ellipse geometry can render both fill and stroke; path geometry renders connected line segments when a visible stroke is present.

## Fields

| Name | Description |
| --- | --- |
| `FillProperty` | Identifies the `Fill` UI property. |
| `StrokeProperty` | Identifies the `Stroke` UI property. |
| `StrokeThicknessProperty` | Identifies the `StrokeThickness` UI property. |
| `GeometryProperty` | Identifies the `Geometry` UI property. |
| `RenderTransformProperty` | Identifies the shape-specific `RenderTransform` UI property. |
| `OpacityProperty` | Identifies the shape-specific `Opacity` UI property. |
| `ShadowProperty` | Identifies the `Shadow` UI property. |

## Properties

| Name | Description |
| --- | --- |
| `Fill` | Gets or sets the brush used to fill the shape interior. |
| `Stroke` | Gets or sets the brush used to draw the shape outline. |
| `StrokeThickness` | Gets or sets the outline thickness. The value must be finite and non-negative. |
| `Geometry` | Gets or sets explicit geometry used for measuring and rendering when a derived class resolves it. |
| `RenderTransform` | Gets or sets the transform applied to rendered shape bounds or path points. |
| `Opacity` | Gets or sets the alpha multiplier applied to rendered fill and stroke colors. |
| `Shadow` | Gets or sets the shadow effect associated with the shape. |

## Methods

| Name | Description |
| --- | --- |
| `MeasureCore(MeasureContext)` | Measures the shape from the resolved geometry bounds plus stroke padding when a stroke is visible. |
| `OnRender(RenderContext)` | Resolves geometry and renders it when opacity and geometry permit drawing. |
| `ResolveGeometry(LayoutRect)` | When implemented by a derived class, returns the geometry to measure or render for the supplied arranged bounds. |
| `RenderGeometry(RenderContext, Geometry)` | Renders supported geometry types using the current fill, stroke, thickness, opacity, and transform settings. |
| `ToDrawRect(LayoutRect)` | Converts a layout rectangle to a non-negative drawing rectangle. |

## Applies to

Cerneala retained UI shape controls.

## See also

- `Cerneala.UI.Controls.Shapes.Rectangle`
- `Cerneala.UI.Controls.Shapes.Ellipse`
- `Cerneala.UI.Controls.Shapes.Path`
- `Cerneala.UI.Media.Geometry`
