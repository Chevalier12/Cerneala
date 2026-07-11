# Border Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Border.cs`

Draws a rectangular background and border around a single child element.

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
    Background = Color.White,
    BorderBrush = Color.Black,
    BorderThickness = new Thickness(2),
    Padding = new Thickness(8),
    Child = new TextBlock { Text = "Ready" }
};
```

The child is measured and arranged inside the combined `Padding` and `BorderThickness` insets.

## Remarks

`Border` is a retained UI control for framing one child. Layout behavior comes from `Decorator`: `Child` is measured with the available size deflated by `Padding + BorderThickness`, then arranged inside the same insets.

During rendering, `Border` fills its arranged bounds with `Background` when the color is not transparent, then draws a rectangle stroke with `BorderBrush` when the color is not transparent and the effective stroke thickness is greater than zero. The rendered stroke thickness is the maximum of `BorderThickness.Left`, `Top`, `Right`, and `Bottom`; side-specific border thickness affects layout insets, but rendering uses one uniform stroke width.

`Background`, `BorderBrush`, `BorderThickness`, and `Padding` are inherited from `Control`. `Border` declares no additional public properties of its own.

## Constructors

| Name | Description |
| --- | --- |
| `Border()` | Initializes a new `Border` instance. |

## Relevant Inherited Properties

| Name | Type | Declared By | Description |
| --- | --- | --- | --- |
| `Child` | `UIElement?` | `Decorator` | Gets or sets the single logical and visual child. Changing it invalidates measure and render. |
| `Background` | `Color` | `Control` | Fill color for the arranged bounds. Default is `Color.Transparent`; changes affect render and input visual invalidation. |
| `BorderBrush` | `Color` | `Control` | Stroke color for the rectangle border. Default is `Color.Transparent`; changes affect render and input visual invalidation. |
| `BorderThickness` | `Thickness` | `Control` | Border inset used for layout and stroke width source used for rendering. Default is `Thickness.Zero`; values must be finite and non-negative. |
| `Padding` | `Thickness` | `Control` | Extra inset between the border and child. Default is `Thickness.Zero`; values must be finite and non-negative. |

## Relevant Inherited Methods

| Name | Return Type | Declared By | Description |
| --- | --- | --- | --- |
| `ApplyTemplate()` | `void` | `Control` | Applies the current control or component template. `Border` itself renders directly unless a template path changes inherited behavior. |

## Rendering Behavior

| Condition | Result |
| --- | --- |
| `Background.A != 0` and arranged width/height are positive | Emits a filled rectangle command for the arranged bounds. |
| `BorderBrush.A != 0`, effective thickness is positive, and arranged width/height are positive | Emits a rectangle stroke command for the arranged bounds. |
| Arranged width or height is zero or negative | Clamps drawing size to zero and emits no background or stroke command. |

## Layout Behavior

| Input | Effect |
| --- | --- |
| `Padding` | Adds to the child inset on each side. |
| `BorderThickness` | Adds to the child inset on each side. |
| `Child` is `null` | Measures as the combined inset size and arranges no child. |

## Applies To

Cerneala retained UI controls and layout/rendering infrastructure.

## See Also

- `Cerneala.UI.Controls.Decorator`
- `Cerneala.UI.Controls.Control`
- `Cerneala.UI.Controls.TextBlock`
- `Cerneala.UI.Layout.Thickness`
- `Cerneala.Drawing.Color`
