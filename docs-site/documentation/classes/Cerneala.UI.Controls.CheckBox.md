# CheckBox Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/CheckBox.cs`

Represents a toggleable checkbox control with a fixed check box glyph and optional content.

```csharp
public class CheckBox : ToggleButton
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Control` -> `ContentControl` -> `ButtonBase` -> `Button` -> `ToggleButton` -> `CheckBox`

## Examples

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;

CheckBox checkBox = new()
{
    Content = "Agree",
    IsChecked = true,
    FontSize = 10,
    Foreground = Color.Black
};
```

## Remarks

`CheckBox` derives its toggle behavior from `ToggleButton`. A left mouse button release with a click count greater than zero toggles `IsChecked`.

The constructor sets checkbox-specific colors: `BorderColor` to `new Color(100, 110, 125)`, `Foreground` to `new Color(35, 45, 60)`, and `Background` to `Color.Transparent`.

During measurement, the control always reserves a 14 by 14 layout box for the checkbox mark. Non-empty string content is measured as `text.Length * FontSize * 0.5f` wide by `FontSize` high, with a 6 pixel gap between the box and text. Padding and border thickness are included through the inherited `Insets` calculation.

During rendering, the checkbox box is drawn at the left side of the arranged bounds and vertically centered inside the inset area. When `IsChecked` is `true`, the box fill uses `Foreground` and an inner white mark is drawn. When `IsChecked` is `false`, the box fill uses `Background`. Non-empty string content is drawn to the right of the box using `FontFamily`, `FontSize`, and `Foreground`.

## Constructors

| Name | Description |
| --- | --- |
| `CheckBox()` | Initializes a checkbox with default border, foreground, and transparent background colors. |

## Key Inherited Members

| Name | Declared By | Description |
| --- | --- | --- |
| `IsChecked` | `ToggleButton` | Gets or sets whether the checkbox is checked. Defaults to `false`; affects render and input visual state. |
| `IsCheckedProperty` | `ToggleButton` | Identifies the `IsChecked` UI property. |
| `Content` | `ContentControl` | Gets or sets the checkbox content. Non-empty string content is measured and rendered directly by `CheckBox`. |
| `Foreground` | `Control` | Gets or sets the color used for checked box fill and text rendering. |
| `Background` | `Control` | Gets or sets the unchecked box fill color. |
| `BorderColor` | `Control` | Gets or sets the checkbox outline color. |
| `FontFamily` | `Control` | Gets or sets the font family used for string content. |
| `FontSize` | `Control` | Gets or sets the font size used for string content and string measurement. |
| `Command` | `ButtonBase` | Gets or sets the command associated with the inherited button behavior. |
| `CommandParameter` | `ButtonBase` | Gets or sets the parameter passed to `Command`. |

## Methods

| Name | Description |
| --- | --- |
| `MeasureCore(MeasureContext)` | Measures the checkbox box, optional content, and inherited insets. |
| `OnRender(RenderContext)` | Draws the checkbox box, checked mark, border, and non-empty string content. |

## Applies To

`Cerneala` UI controls targeting `net8.0`.

## See Also

- `Cerneala.UI.Controls.Primitives.ToggleButton`
- `Cerneala.UI.Controls.Button`
- `Cerneala.UI.Controls.ContentControl`
