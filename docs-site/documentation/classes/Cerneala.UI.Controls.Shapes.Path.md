# Path Class

## Definition
Namespace: `Cerneala.UI.Controls.Shapes`
Assembly/Project: `Cerneala`
Source: `UI/Controls/Shapes/Path.cs`

Represents a shape control that renders a `PathGeometry` from its `Data` property unless the inherited `Geometry` property is set.

```csharp
public sealed class Path : Shape
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Control` -> `Shape` -> `Path`

## Examples

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls.Shapes;
using Cerneala.UI.Media;
using PathShape = Cerneala.UI.Controls.Shapes.Path;

PathShape path = new()
{
    Data = new PathGeometry(
    [
        new DrawPoint(0, 0),
        new DrawPoint(40, 0),
        new DrawPoint(40, 24)
    ]),
    Stroke = new SolidColorBrush(Color.Black),
    StrokeThickness = 2
};
```

## Remarks

`Path` resolves its render geometry by returning the inherited `Geometry` value first, then falling back to `Data`. This means `Shape.Geometry` takes precedence over `Path.Data` when both are assigned.

`Data` is a `UiProperty<PathGeometry?>` with a default value of `null`. Changing it affects both measure and render. When no geometry is resolved, measuring returns `LayoutSize.Zero` through the inherited `Shape` measurement behavior, and rendering emits no drawing commands.

The inherited `Shape` renderer draws `PathGeometry` instances as connected line segments between consecutive points. It uses `Stroke`, `StrokeThickness`, `Opacity`, and `RenderTransform`; the inherited `Fill` brush is not used for `PathGeometry` rendering. A visible stroke color and a positive stroke thickness are required for line commands to be emitted.

`PathGeometry` requires at least one point and exposes immutable point data. Its bounds are calculated from the point set and are used by shape measurement, with stroke thickness added when the stroke has a solid color.

## Constructors

| Name | Description |
| --- | --- |
| `Path()` | Initializes a new path control with `Data` set to `null` and inherited `Shape` defaults. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `DataProperty` | `UiProperty<PathGeometry?>` | Identifies the `Data` UI property. The property defaults to `null` and has `AffectsMeasure` and `AffectsRender` metadata. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Data` | `PathGeometry?` | `null` | Gets or sets the path geometry used when the inherited `Geometry` property is `null`. |

## Relevant Inherited Shape Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Geometry` | `Geometry?` | `null` | Overrides `Data` as the resolved geometry when assigned. |
| `Stroke` | `Brush?` | `null` | Provides the stroke brush used to draw `PathGeometry` line segments. |
| `StrokeThickness` | `float` | `1` | Controls rendered line thickness and contributes to measured size when the stroke has a solid color. |
| `Fill` | `Brush?` | `null` | Inherited from `Shape`, but not used when rendering a `PathGeometry`. |
| `RenderTransform` | `Transform` | `Transform.Identity` | Applies to rendered path segment endpoints. |
| `Opacity` | `float` | `1` | Multiplies rendered stroke alpha; rendering is skipped when opacity is less than or equal to `0`. |
| `Shadow` | `ShadowEffect?` | `null` | Inherited shape effect property. |

## Methods

`Path` does not declare public methods. It overrides the protected `ResolveGeometry(LayoutRect arrangedBounds)` hook from `Shape`.

## Applies To

Cerneala retained UI shape controls in the `Cerneala` project.

## See Also

- `UI/Controls/Shapes/Path.cs`
- `UI/Controls/Shapes/Shape.cs`
- `UI/Media/PathGeometry.cs`
