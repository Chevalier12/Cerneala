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
    BorderThickness = new Thickness(-2),
    Padding = new Thickness(8),
    Child = new TextBlock { Text = "Ready" }
};
```

`BorderThickness` sides are signed. `-2` is a band of 2 pixels inside the arranged bounds, so the child is measured and arranged inside `Padding` (8) plus 2 = 10 pixels on each side. With `new Thickness(2)` (positive) the band lies outside the arranged bounds instead, and the child is inset by `Padding` only (8 pixels).

Draw only a 2 pixel line inside the bottom edge, and round only the left corners of a card whose 1 pixel border lies outside its bounds:

```csharp
Border underline = new()
{
    BorderBrush = new Cerneala.UI.Media.SolidColorBrush(Color.Black),
    BorderThickness = new Thickness(0, 0, 0, -2)
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

`Border` is a retained UI control for framing one child. Layout behavior comes from `Decorator`: `Child` is measured with the available size deflated by `Padding` plus the inward part of `BorderThickness`, then arranged inside the same insets. `CornerRadius` does not change layout.

Each side of `BorderThickness` is signed, and the sign says on which side of the arranged bounds the band is painted:

| Side value | Painted band | Room taken from the child |
| --- | --- | --- |
| `-t` (negative) | `t` pixels inside the arranged bounds | `t` pixels |
| `+t` (positive) | `t` pixels outside the arranged bounds | none |
| `0` | nothing | none |

For example, a `Border` arranged at `(10, 10, 40, 30)` with `BorderThickness = new Thickness(0, 0, 0, -2)` paints only the rows `y = 38` and `y = 39`, inside the bounds, and its child loses 2 pixels at the bottom. With `new Thickness(0, 0, 0, 2)` it paints the rows `y = 40` and `y = 41`, below the bounds, and the child loses nothing. A positive band is outside the arranged bounds, so it is not part of the control's hit-test area, and it can be cut off by a parent that clips. The default value of `Padding` and `BorderThickness` on a plain `Border` is `Thickness.Zero`, which paints nothing. The built-in controls (`Button`, `ScrollBar`, `Thumb`, and so on) take a positive default from the Aspect package or their constructor, so their border is painted outside their bounds.

During rendering, `Border` fills its arranged bounds with `Background` when the brush is not `null`, then paints the border with `BorderBrush` on top of it. `Background` always covers the arranged bounds, whatever the sign of `BorderThickness`.

`CornerRadius` rounds the background and the border. A radius is given per corner, in the order top-left, top-right, bottom-right, bottom-left, and it belongs to the arranged bounds. The border is the ring between an outer shape and an inner shape:

- The outer shape is the arranged bounds grown by each positive side. A corner whose radius is `0` stays square. Otherwise it is an ellipse quarter with the radius plus the positive thickness of the two sides that meet there. For example, a radius of 8 with `BorderThickness = new Thickness(2)` gives an outer corner of radius 10 around the unchanged background corner of radius 8.
- The inner shape is the arranged bounds shrunk by each negative side. Its corner radius is the radius minus the negative thickness of the two sides that meet there, and never below `0`, so a corner whose radius is not larger than the inward band is square inside. For example, a radius of 8 with `BorderThickness = new Thickness(-2)` gives an inner corner of radius 6.

When the radii of two corners on one side add up to more than that side's length, all four radii are scaled down by the same factor until they fit. The child is not clipped to the rounded shape.

The background is painted under the whole control, also under an inward border. With a semi-transparent `BorderBrush`, an inward border shows the background through it.

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
| `BorderThickness` | `Thickness` | `Control` | Signed thickness of each side: `-t` paints a band of `t` pixels inside the arranged bounds and takes `t` pixels from the child, `+t` paints a band of `t` pixels outside the bounds and takes nothing, `0` paints nothing. Default is `Thickness.Zero`; each side must be finite, and may be negative. |
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
| `BorderBrush != null`, every side of `BorderThickness` is the same positive value `t`, every corner radius is `0` | Emits a `DrawRectangle` command of thickness `t` on the arranged bounds inflated by `t / 2`, so the stroke covers exactly the `t` pixels outside each edge. For example, bounds `(1, 2, 30, 20)` and `t = 2` give a stroke at `(0, 1, 32, 22)`. |
| `BorderBrush != null`, every side of `BorderThickness` is the same negative value `-t`, every corner radius is `0` | Emits a `DrawRectangle` command of thickness `t` on the arranged bounds deflated by `t / 2`, so the stroke covers exactly the `t` pixels inside each edge. For example, bounds `(1, 2, 30, 20)` and `-t = -2` give a stroke at `(2, 3, 28, 18)`. When `2 * t` reaches the width or the height, emits a `FillRectangle` of the whole bounds instead. |
| `BorderBrush != null`, the sides of `BorderThickness` differ (also in sign) or a corner radius is positive | Emits one `FillPath` command with `DrawFillRule.EvenOdd`: the outer shape (the bounds grown by each positive side) minus the inner shape (the bounds shrunk by each negative side). For example, `new Thickness(0, 0, 0, -2)` on bounds `(10, 10, 40, 30)` fills the rectangle `(10, 38, 40, 2)`. |
| `BorderBrush == null` or every side of `BorderThickness` is `0` | Emits no border command. |
| Arranged width or height is zero or negative | Clamps drawing size to zero and emits no background or border command. |

## Layout Behavior

| Input | Effect |
| --- | --- |
| `Padding` | Adds to the child inset on each side. |
| `BorderThickness` | A negative side `-t` adds `t` to the child inset on that side; a positive or zero side adds nothing. |
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
