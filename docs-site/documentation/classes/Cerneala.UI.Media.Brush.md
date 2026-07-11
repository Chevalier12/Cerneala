# Brush Class

## Definition
Namespace: `Cerneala.UI.Media`

Assembly/Project: `Cerneala`

Source: `UI/Media/Brush.cs`

Provides the abstract base record for media brushes used by retained UI drawing APIs.

```csharp
public abstract record Brush
```

Inheritance:
`object` -> `Brush`

Derived:
`SolidColorBrush`, `LinearGradientBrush`, `RadialGradientBrush`

## Examples

Use a `Brush` value through a shape fill:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls.Shapes;
using Cerneala.UI.Media;

Brush fill = new SolidColorBrush(new Color(64, 128, 255));

Rectangle rectangle = new()
{
    Fill = fill,
    Stroke = new SolidColorBrush(Color.Black),
    StrokeThickness = 2
};

Color? solidColor = fill.SolidColor;
```

## Remarks

`Brush` is the shared base type for solid and gradient brush records. It exposes `SolidColor` as an optional solid-color representation. The base implementation returns `null`; `SolidColorBrush` overrides it to return its `Color`.

Consumers can use `SolidColor` when they need a concrete `Color`. For example, shape rendering and measuring paths that inspect `Fill` or `Stroke` through `SolidColor` treat brushes that return `null` as having no solid color. Gradient brush types inherit the base `null` value unless they provide their own override.

`Brush` does not define validation, color interpolation, or rendering behavior by itself. Those behaviors belong to concrete brush types and to the rendering code that consumes them.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `SolidColor` | `Color?` | Gets the brush as a solid color when available; the base implementation returns `null`. |

## Applies To

Cerneala retained UI media and rendering APIs.

## See Also

- `Cerneala.UI.Media.SolidColorBrush`
- `Cerneala.UI.Media.LinearGradientBrush`
- `Cerneala.UI.Media.RadialGradientBrush`
- `Cerneala.UI.Controls.Shapes.Shape`
- `Cerneala.Drawing.Color`
