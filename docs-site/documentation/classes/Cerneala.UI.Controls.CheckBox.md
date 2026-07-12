# CheckBox Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/CheckBox.cs`

Represents a toggleable checkbox control composed by a component template with an optional content value.

```csharp
public class CheckBox : ToggleButton
```

Attributes:
`TemplatePart("PART_CheckMark", typeof(Cerneala.UI.Controls.Shapes.Path))`

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
    Foreground = new SolidColorBrush(Color.Black)
};
```

## Remarks

`CheckBox` derives its toggle behavior from `ToggleButton`. A left mouse button release with a click count greater than zero toggles `IsChecked`.

The constructor installs the default component template, a one-pixel border thickness, and a default border brush at the `AspectBase` value source. Markup and local values can override these defaults.

The default template contains a bordered indicator, the required `PART_CheckMark` path, and a `ContentPresenter`. The presenter is separated from the indicator by a six-pixel margin. `Background` colors the complete checkbox surface, while `BorderBrush` and `BorderThickness` style the indicator box. `Padding`, `Foreground`, `FontFamily`, `FontSize`, and `Content` are bound into the content portion of the template.

`PART_CheckMark` must be a `Cerneala.UI.Controls.Shapes.Path`. The control changes its visibility to `Visible` when `IsChecked` is `true` and to `Hidden` otherwise. During arrangement, its geometry is scaled uniformly to fit its arranged bounds with a 1.5-pixel inset and is centered on both axes. The default indicator measures to a square and is vertically centered beside the content.

The default path uses a one-pixel black stroke and a compact three-point `PathGeometry`. Its stroke is owned by the template and is not bound to `Foreground`, so a custom template can choose the check-mark brush independently from the text color.

A custom component template must provide `PART_CheckMark` with the required type. Named elements declared inside generated `@template` markup are registered as template parts.

## Constructors

| Name | Description |
| --- | --- |
| `CheckBox()` | Initializes a checkbox with its default component template, border brush, and one-pixel indicator border. |

## Template Parts

| Name | Type | Description |
| --- | --- | --- |
| `PART_CheckMark` | `Cerneala.UI.Controls.Shapes.Path` | Required path whose visibility reflects `IsChecked` and whose stroke represents the check mark. |

## Key Inherited Members

| Name | Declared By | Description |
| --- | --- | --- |
| `IsChecked` | `ToggleButton` | Gets or sets whether the checkbox is checked. Defaults to `false`; affects render and input visual state. |
| `IsCheckedProperty` | `ToggleButton` | Identifies the `IsChecked` UI property. |
| `Content` | `ContentControl` | Gets or sets the content presented by the default template. |
| `ComponentTemplate` | `Control` | Gets or sets the component template. The default template provides `PART_CheckMark`. |
| `Foreground` | `Control` | Gets or sets the brush used by the presented text. It does not change `PART_CheckMark`. |
| `Background` | `Control` | Gets or sets the background brush for the complete checkbox surface. |
| `BorderBrush` | `Control` | Gets or sets the indicator border brush. |
| `FontFamily` | `Control` | Gets or sets the font family used for string content. |
| `FontSize` | `Control` | Gets or sets the font size used for string content and string measurement. |
| `Command` | `ButtonBase` | Gets or sets the command associated with the inherited button behavior. |
| `CommandParameter` | `ButtonBase` | Gets or sets the parameter passed to `Command`. |

## Methods

| Name | Description |
| --- | --- |
| `MeasureCore(MeasureContext)` | Applies and measures the component template. |
| `ArrangeCore(ArrangeContext)` | Arranges the template, uniformly scales `PART_CheckMark`, and centers its geometry. |
| `OnPropertyChanged(UiPropertyChangedEventArgs)` | Synchronizes `PART_CheckMark` when `IsChecked` changes. |

## Applies To

`Cerneala` UI controls targeting `net8.0`.

## See Also

- `Cerneala.UI.Controls.Primitives.ToggleButton`
- `Cerneala.UI.Controls.Button`
- `Cerneala.UI.Controls.ContentControl`
