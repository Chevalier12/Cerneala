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
    Data = PathGeometry.Parse("M0 0 Q20 0 40 24 L0 24 Z"),
    Fill = new SolidColorBrush(Color.White),
    Stroke = new SolidColorBrush(Color.Black),
    StrokeThickness = 2
};
```

```xml
<Path Data="M0 0 Q20 0 40 24 L0 24 Z" Fill="White" Stroke="Black"
      StrokeThickness="2" FillRule="EvenOdd" />
```

For C# construction, pass an immutable `DrawPath` built with `DrawPathBuilder` to `PathGeometry.FromPath`.

## Remarks

`Path` resolves its render geometry by returning the inherited `Geometry` value first, then falling back to `Data`. This means `Shape.Geometry` takes precedence over `Path.Data` when both are assigned.

`Data` is a `UiProperty<PathGeometry?>` with a default value of `null`. Changing it affects both measure and render. When no geometry is resolved, measuring returns `LayoutSize.Zero` through the inherited `Shape` measurement behavior, and rendering emits no drawing commands.

The inherited `Shape` renderer supports lines, quadratic and cubic Bézier curves, elliptical arcs, multiple contours, and explicit open/closed contour state. Fill and stroke reuse the complete immutable native path. `FillRule` selects `NonZero` or `EvenOdd`; filling an open contour implicitly closes it without closing its stroke.

Path coordinates are local to the arranged position and are not automatically stretched. The inherited `UIElement` transform and opacity apply through retained drawing state. Explicit `SvgGeometry` retains its view-box-to-arranged-bounds mapping.

The existing point-sequence `PathGeometry` constructor remains supported. It snapshots an open polyline and requires at least one point; a single point measures but has no drawable path. Rich paths use the existing `Drawing` builder or SVG parser. Their conservative bounds are used by shape measurement, with stroke padding when `Stroke` is set.

In generated `.crn`, a `Data` string is lowered to `PathGeometry.Parse`. SVG syntax is validated by the shared runtime parser when the factory creates the control. Typed `PathGeometry` property bindings are also supported; the public `Data` type has not changed.

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
| `Stroke` | `Brush?` | `null` | Provides the brush for the complete native path stroke. |
| `StrokeThickness` | `float` | `1` | Controls stroke thickness and contributes to measured size when a stroke is set. |
| `Fill` | `Brush?` | `null` | Provides the brush for filling the path interior. |
| `FillRule` | `DrawFillRule` | `NonZero` | Selects non-zero winding or even-odd filling. |
| `RenderTransform` | `Transform` | `Transform.Identity` | Applies an affine transform to the complete path through retained drawing state. |
| `Opacity` | `float` | `1` | Multiplies fill and stroke paint opacity; rendering is skipped when opacity is `0`. |

## Methods

`Path` does not declare public methods. It overrides the protected `ResolveGeometry(LayoutRect arrangedBounds)` hook from `Shape`.

## Applies To

Cerneala retained UI shape controls in the `Cerneala` project.

## See Also

- `UI/Controls/Shapes/Path.cs`
- `UI/Controls/Shapes/Shape.cs`
- `UI/Media/PathGeometry.cs`
