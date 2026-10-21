# Button Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Button.cs`

Represents a command-capable content button that can render simple fallback chrome and text when no `ComponentTemplate` is applied.

```csharp
public class Button : ButtonBase
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Control` -> `ContentControl` -> `ButtonBase` -> `Button`

## Examples

Create a fallback-rendered button with text content:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Layout;

Button button = new()
{
    Content = "Save",
    Padding = new Thickness(8, 4, 8, 4),
    Background = new Cerneala.UI.Media.SolidColorBrush(Color.White),
    BorderBrush = new Cerneala.UI.Media.SolidColorBrush(Color.Black),
    BorderThickness = new Thickness(1)
};

LayoutSize desired = button.Measure(new MeasureContext(new LayoutSize(120, 40)));
button.Arrange(new ArrangeContext(new LayoutRect(0, 0, desired.Width, desired.Height)));
```

Place the content at the top-left corner instead of in the middle, or stretch an element content over the whole inner area:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Layout;

Button topLeft = new()
{
    Width = 300,
    Height = 70,
    Content = "Hold me",
    HorizontalContentAlignment = HorizontalAlignment.Left,
    VerticalContentAlignment = VerticalAlignment.Top
};

Button stretched = new()
{
    Width = 300,
    Height = 70,
    Content = new TextBlock { Text = "Hold me" },
    HorizontalContentAlignment = HorizontalAlignment.Stretch,
    VerticalContentAlignment = VerticalAlignment.Stretch
};
```

Bind a command to the button:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Input;

bool saved = false;

Button button = new()
{
    Content = "Save",
    Command = new ActionCommand(_ => saved = true)
};
```

## Remarks

`Button` builds on `ButtonBase`, so it is focusable, participates in tab navigation, uses a hand cursor, exposes pressed state, and can execute `ICommand` or `RoutedCommand` instances through the retained input pipeline.

The button does not declare its own content property. It uses `ContentControl.ContentProperty`, so element content follows the content-control ownership rules. While both `ComponentTemplate` and `ComponentTemplateKey` are `null`, `UIElement` content is added as logical and visual content. When no template child is present, `UIElement` content is measured inside `Padding + BorderThickness`; string content is measured by `TextMeasurer` and rendered by `TextRenderer`.

As in WPF's default `Button`, the content is centered inside `Padding + BorderThickness`: the constructor sets framework defaults of `HorizontalAlignment.Center` for `HorizontalContentAlignment` and `VerticalAlignment.Center` for `VerticalContentAlignment`, and an Aspect or local value replaces them. Without a template, `Button` arranges `UIElement` content in a slot of the content's `DesiredSize` placed by these two properties, or across the whole inner width or height for `Stretch`, and draws string content at the same kind of slot of the measured text size; `Stretch` places text like `Left` and `Top`. For example, a 300 x 70 `Button` in a `UIRoot` gets `Padding` 8 and `BorderThickness` 1 from the default Aspect package, so its inner area is 282 x 52; a `TextBlock` content of desired size 61 x 22 is arranged in the middle of that area at its desired size, and the string `"Hold me"` (60.56 px wide) starts 110.72 px right of the inner left edge. The templates `DefaultTheme.CreateButtonTemplate()` and `ButtonTemplates.Modern` bind the two properties to their `ContentPresenter`'s `HorizontalAlignment` and `VerticalAlignment`, so a templated button places its content the same way. `ToggleButton` and `RepeatButton` keep the `Center` defaults; `CheckBox` and `RadioButton` set them back to `Left` and `Top`, as their WPF styles do.

Fallback rendering draws the resolved background brush, then the border, then non-empty string content. If `Background` has an explicit value, that brush is used. Otherwise fallback state colors are wrapped in solid brushes for disabled, pressed, pointer-over, and keyboard-focused states before falling back to the default background.

When `ComponentTemplate` supplies a template child, layout is delegated to the base template path and `Button` skips its fallback rendering. Template-based buttons commonly bind `Content`, `Background`, `BorderBrush`, `BorderThickness`, and `Padding` into the template root or presenter.

The fallback text aspect of string content is built from `FontFamily`, `FontSize`, `FontWeight`, `FontStyle`, `FontStretch`, and `Foreground`. For example, a `Button` with `FontWeight = FontWeight.Bold` and the string content `"OK"` measures and draws `"OK"` with the bold face of its font family. The string content is still a single run of text measured by `TextMeasurer.Measure`; it does not use the inline or paragraph options of `TextBlock`.

`TextMeasurer` and `TextRenderer` default to `TextMeasurer.Default` and `TextRenderer.Default`. Assigning `null` to either property throws `ArgumentNullException`. Changing `TextMeasurer` invalidates measure and render; changing `TextRenderer` invalidates render.

`AutomationPeer.Create(button)` creates a `ButtonAutomationPeer`, which reports `SemanticsRole.Button` and resolves its name from an explicit accessible name or supported button content text. `RepeatButton` gets the same peer. `ToggleButton`, `CheckBox`, and `RadioButton` derive from `Button` but get the derived `ToggleButtonAutomationPeer`.

## Constructors

| Name | Description |
| --- | --- |
| `Button()` | Initializes a new button whose `HorizontalContentAlignment` and `VerticalContentAlignment` default to `Center`. The inherited `ButtonBase` constructor makes the control focusable, a tab stop, and assigns the hand cursor. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `TextMeasurer` | `TextMeasurer` | Gets or sets the text measurer used for string fallback content. Defaults to `TextMeasurer.Default`; rejects `null`; changing it invalidates measure and render. |
| `TextRenderer` | `TextRenderer` | Gets or sets the text renderer used for string fallback content. Defaults to `TextRenderer.Default`; rejects `null`; changing it invalidates render. |

## Important Inherited Properties

| Name | Type | Declared by | Description |
| --- | --- | --- | --- |
| `Content` | `object?` | `ContentControl` | Gets or sets the button content. `UIElement` content is owned as logical and visual content while both `ComponentTemplate` and `ComponentTemplateKey` are `null`. |
| `Command` | `ICommand?` | `ButtonBase` | Gets or sets the command executed by pointer or keyboard activation. |
| `CommandParameter` | `object?` | `ButtonBase` | Gets or sets the parameter passed to `Command`. |
| `IsPressed` | `bool` | `ButtonBase` | Gets or sets the pressed visual/input state. |
| `Background` | `Brush?` | `Control` | Gets or sets the background brush. An explicit value overrides fallback state brushes. |
| `Foreground` | `Brush?` | `Control` | Gets or sets the foreground brush used by fallback text rendering. |
| `BorderBrush` | `Brush?` | `Control` | Gets or sets the fallback border brush. |
| `BorderThickness` | `Thickness` | `Control` | Gets or sets the fallback border thickness and contributes to content insets. |
| `Padding` | `Thickness` | `Control` | Gets or sets the padding around fallback content. |
| `FontFamily` | `string` | `Control` | Gets or sets the inherited font family used to create the fallback text aspect. |
| `FontSize` | `float` | `Control` | Gets or sets the inherited font size used to create the fallback text aspect. |
| `FontWeight` | `FontWeight` | `Control` | Gets or sets the inherited font weight used to create the fallback text aspect. |
| `FontStyle` | `FontStyle` | `Control` | Gets or sets the inherited font style used to create the fallback text aspect. |
| `FontStretch` | `FontStretch` | `Control` | Gets or sets the inherited font stretch used to create the fallback text aspect. |
| `HorizontalContentAlignment` | `HorizontalAlignment` | `Control` | Gets or sets the horizontal placement of the content inside `Padding + BorderThickness`. Defaults to `Center` on `Button`. |
| `VerticalContentAlignment` | `VerticalAlignment` | `Control` | Gets or sets the vertical placement of the content inside `Padding + BorderThickness`. Defaults to `Center` on `Button`. |
| `ComponentTemplate` | `ComponentTemplate?` | `Control` | Gets or sets the component template. When present, the template child handles layout and rendering. |
| `ComponentTemplateInstance` | `ComponentTemplateInstance?` | `Control` | Gets the active component-template instance, when one has been applied. |
| `AspectVariants` | `AspectVariantSet` | `Control` | Gets the active aspect variant values used by component templates. |

## Important Inherited Events

| Name | Declared by | Description |
| --- | --- | --- |
| `Click` | `ButtonBase` | Occurs when pointer or keyboard activation invokes the button. |

## Important Inherited Fields

| Name | Type | Declared by | Description |
| --- | --- | --- | --- |
| `ContentProperty` | `UiProperty<object?>` | `ContentControl` | Identifies the inherited `Content` UI property used by `Button`. |
| `CommandProperty` | `UiProperty<ICommand?>` | `ButtonBase` | Identifies the `Command` UI property. |
| `CommandParameterProperty` | `UiProperty<object?>` | `ButtonBase` | Identifies the `CommandParameter` UI property. |
| `IsPressedProperty` | `UiProperty<bool>` | `ButtonBase` | Identifies the `IsPressed` UI property. |
| `BackgroundProperty` | `UiProperty<Brush?>` | `Control` | Identifies the `Background` UI property. |
| `ForegroundProperty` | `UiProperty<Brush?>` | `Control` | Identifies the inherited `Foreground` UI property. |
| `BorderBrushProperty` | `UiProperty<Brush?>` | `Control` | Identifies the `BorderBrush` UI property. |
| `BorderThicknessProperty` | `UiProperty<Thickness>` | `Control` | Identifies the `BorderThickness` UI property. |
| `PaddingProperty` | `UiProperty<Thickness>` | `Control` | Identifies the `Padding` UI property. |
| `FontFamilyProperty` | `UiProperty<string>` | `Control` | Identifies the inherited `FontFamily` UI property. |
| `FontSizeProperty` | `UiProperty<float>` | `Control` | Identifies the inherited `FontSize` UI property. |
| `FontWeightProperty` | `UiProperty<FontWeight>` | `Control` | Identifies the inherited `FontWeight` UI property. |
| `FontStyleProperty` | `UiProperty<FontStyle>` | `Control` | Identifies the inherited `FontStyle` UI property. |
| `FontStretchProperty` | `UiProperty<FontStretch>` | `Control` | Identifies the inherited `FontStretch` UI property. |
| `HorizontalContentAlignmentProperty` | `UiProperty<HorizontalAlignment>` | `Control` | Identifies the `HorizontalContentAlignment` UI property. |
| `VerticalContentAlignmentProperty` | `UiProperty<VerticalAlignment>` | `Control` | Identifies the `VerticalContentAlignment` UI property. |
| `ComponentTemplateProperty` | `UiProperty<ComponentTemplate?>` | `Control` | Identifies the `ComponentTemplate` UI property. |

## Important Inherited Methods

| Name | Return Type | Declared by | Description |
| --- | --- | --- | --- |
| `CanExecuteCommand(CommandRouter router, ElementInputRouteMap routeMap)` | `bool` | `ButtonBase` | Returns whether the current command can execute for this button and command parameter. |
| `ExecuteCommand(CommandRouter router, ElementInputRouteMap routeMap)` | `bool` | `ButtonBase` | Executes the current command when the button is enabled and the command can run. |
| `RefreshCommandState(CommandRouter router, ElementInputRouteMap routeMap)` | `bool` | `ButtonBase` | Updates `IsEnabled` from command state and returns whether it changed. |
| `ApplyTemplate()` | `void` | `Control` | Creates or refreshes the active component-template instance for the current `ComponentTemplate`. |
| `SetAspectVariant<TControl, TValue>(AspectVariantKey<TControl, TValue> key, TValue value)` | `void` | `Control` | Sets a component-template aspect variant and invalidates aspect/render state when it changes. |

## Applies to

Project: `Cerneala`

## See also

- `UI/Controls/Button.cs`
- `UI/Controls/Primitives/ButtonBase.cs`
- `UI/Controls/ContentControl.cs`
- `UI/Accessibility/ButtonAutomationPeer.cs`
