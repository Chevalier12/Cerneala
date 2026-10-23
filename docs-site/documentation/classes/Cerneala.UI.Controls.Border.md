# Border Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Border.cs`

Draws a background and a border, with optional rounded corners, around a single child element.

```csharp
public class Border : Decorator
```

Inheritance:
`Object` -> `UiObject` -> `UIElement` -> `Control` -> `Decorator` -> `Border`

## Examples

Create a bordered panel with padding and a text child:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Layout;

Border panel = new()
{
    Background = new Cerneala.UI.Media.SolidColorBrush(Color.White),
    BorderBrush = new Cerneala.UI.Media.SolidColorBrush(Color.Black),
    BorderThickness = new Thickness(2),
    Padding = new Thickness(8),
    Child = new TextBlock { Text = "Ready" }
};
```

The child is measured and arranged inside the combined `Padding` and `BorderThickness` insets.

Draw only a 2 pixel line at the bottom, and round only the left corners of a card:

```csharp
Border underline = new()
{
    BorderBrush = new Cerneala.UI.Media.SolidColorBrush(Color.Black),
    BorderThickness = new Thickness(0, 0, 0, 2)
};

Border card = new()
{
    Background = new Cerneala.UI.Media.SolidColorBrush(Color.White),
    BorderBrush = new Cerneala.UI.Media.SolidColorBrush(Color.Black),
    BorderThickness = new Thickness(1),
    CornerRadius = new CornerRadius(8, 0, 0, 8)
};
```

The same card in markup:

```xml
<Border Background="White" BorderBrush="Black" BorderThickness="1" CornerRadius="8,0,0,8" />
```

## Remarks

`Border` is a retained UI control for framing one child. Layout behavior comes from `Decorator`: `Child` is measured with the available size deflated by `Padding + BorderThickness`, then arranged inside the same insets. `CornerRadius` does not change layout.

During rendering, `Border` fills its arranged bounds with `Background` when the brush is not `null`, then paints the border with `BorderBrush` on top of it. The border lies inside the arranged bounds, in the same area that layout reserves for `BorderThickness`: each side is painted with its own thickness, and a side with thickness `0` is not painted. For example, a `Border` arranged at `(0, 0, 100, 50)` with `BorderThickness = new Thickness(0, 0, 0, 2)` paints only the rows `y = 48` and `y = 49`; nothing is painted outside `(0, 0, 100, 50)`.

`CornerRadius` rounds both the background and the border. A radius is given per corner, in the order top-left, top-right, bottom-right, bottom-left. The inner edge of the border is rounded by the outer radius minus the thickness of the two sides that meet at that corner, so a corner whose radius is not larger than the border is square inside. When the radii of two corners on one side add up to more than that side's length, all four radii are scaled down by the same factor until they fit. The child is not clipped to the rounded shape.

The background is painted under the whole control, also under the border. With a semi-transparent `BorderBrush`, the border shows the background through it.

`Background`, `BorderBrush`, `BorderThickness`, and `Padding` are inherited from `Control`. `CornerRadius` is declared by `Border`.

## Constructors

| Name | Description |
| --- | --- |
| `Border()` | Initializes a new `Border` instance. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `CornerRadiusProperty` | `UiProperty<CornerRadius>` | Identifies the `CornerRadius` UI property. Defaults to `CornerRadius.Zero`; affects render; each corner must be finite and non-negative. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `CornerRadius` | `CornerRadius` | Gets or sets the radius of each corner of the background and the border. Default is `CornerRadius.Zero` (square corners). Setting a negative, `NaN`, or infinite corner throws. In markup it accepts one value for all corners (`CornerRadius="8"`) or four comma-separated values (`CornerRadius="8,0,0,8"`); any other count is an error. |

## Relevant Inherited Properties

| Name | Type | Declared By | Description |
| --- | --- | --- | --- |
| `Child` | `UIElement?` | `Decorator` | Gets or sets the single logical and visual child. Changing it invalidates measure and render. |
| `Background` | `Brush?` | `Control` | Brush for the fill of the arranged bounds, rounded by `CornerRadius`. Default is `null`; changes affect render and input visual invalidation. |
| `BorderBrush` | `Brush?` | `Control` | Brush for the border. Default is `null`; changes affect render and input visual invalidation. |
| `BorderThickness` | `Thickness` | `Control` | Border inset used for layout, and the painted thickness of each side. Default is `Thickness.Zero`; values must be finite and non-negative. |
| `Padding` | `Thickness` | `Control` | Extra inset between the border and child. Default is `Thickness.Zero`; values must be finite and non-negative. |

## Relevant Inherited Methods

| Name | Return Type | Declared By | Description |
| --- | --- | --- | --- |
| `ApplyTemplate()` | `void` | `Control` | Applies the current control or component template. `Border` itself renders directly unless a template path changes inherited behavior. |

## Rendering Behavior

| Condition | Result |
| --- | --- |
| `Background != null`, arranged width/height are positive, every corner radius is `0` | Emits a `FillRectangle` command for the arranged bounds. |
| `Background != null`, arranged width/height are positive, a corner radius is positive | Emits a `FillRoundedRectangle` command for the arranged bounds with the fitted corner radii. |
| `BorderBrush != null`, every side of `BorderThickness` is the same value `t > 0`, every corner radius is `0` | Emits a `DrawRectangle` command of thickness `t` on the arranged bounds deflated by `t / 2`, so the stroke covers exactly the `t` pixels inside each edge. For example, bounds `(1, 2, 30, 20)` and `t = 2` give a stroke at `(2, 3, 28, 18)`. When `2 * t` reaches the width or the height, emits a `FillRectangle` of the whole bounds instead. |
| `BorderBrush != null`, the sides of `BorderThickness` differ or a corner radius is positive | Emits one `FillPath` command with `DrawFillRule.EvenOdd`: the outer shape of the bounds minus the inner shape deflated by each side's thickness. |
| `BorderBrush == null` or every side of `BorderThickness` is `0` | Emits no border command. |
| Arranged width or height is zero or negative | Clamps drawing size to zero and emits no background or border command. |

## Layout Behavior

| Input | Effect |
| --- | --- |
| `Padding` | Adds to the child inset on each side. |
| `BorderThickness` | Adds to the child inset on each side. |
| `CornerRadius` | No effect on layout. |
| `Child` is `null` | Measures as the combined inset size and arranges no child. |

## Applies To

Cerneala retained UI controls and layout/rendering infrastructure.

## See Also

- `Cerneala.UI.Controls.Decorator`
- `Cerneala.UI.Controls.Control`
- `Cerneala.UI.Controls.TextBlock`
- `Cerneala.UI.Layout.Thickness`
- `Cerneala.UI.Layout.CornerRadius`
- `Cerneala.Drawing.Color`
