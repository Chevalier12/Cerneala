# Button Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Button.cs`

Represents a command-capable content button that can render simple fallback chrome and text when no control template is applied.

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
    Background = DrawColor.White,
    BorderColor = DrawColor.Black,
    BorderThickness = new Thickness(1)
};

LayoutSize desired = button.Measure(new MeasureContext(new LayoutSize(120, 40)));
button.Arrange(new ArrangeContext(new LayoutRect(0, 0, desired.Width, desired.Height)));
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

The button does not declare its own content property. It uses `ContentControl.ContentProperty`, so element content follows the content-control ownership rules. While the classic `Template` property is `null`, `UIElement` content is added as logical and visual content. When the button has no `Template` or `ComponentTemplate`, `UIElement` content is measured and arranged inside `Padding + BorderThickness`; string content is measured by `TextMeasurer` and rendered by `TextRenderer`.

Fallback rendering draws the resolved background, then the border, then non-empty string content. If `Background` has an explicit value, that value is used. Otherwise fallback state colors are used for disabled, pressed, pointer-over, and keyboard-focused states before falling back to the default background.

When a template or component template supplies a template child, layout is delegated to the base template path and `Button` skips its fallback rendering. Template-based buttons commonly bind `Content`, `Background`, `BorderColor`, `BorderThickness`, and `Padding` into the template root or presenter.

`TextMeasurer` and `TextRenderer` default to `TextMeasurer.Default` and `TextRenderer.Default`. Assigning `null` to either property throws `ArgumentNullException`. Changing `TextMeasurer` invalidates measure and render; changing `TextRenderer` invalidates render.

`AutomationPeer.Create(button)` creates a `ButtonAutomationPeer`, which reports `SemanticsRole.Button` and resolves its name from an explicit accessible name or supported button content text.

## Constructors

| Name | Description |
| --- | --- |
| `Button()` | Initializes a new button. The inherited `ButtonBase` constructor makes the control focusable, a tab stop, and assigns the hand cursor. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `TextMeasurer` | `TextMeasurer` | Gets or sets the text measurer used for string fallback content. Defaults to `TextMeasurer.Default`; rejects `null`; changing it invalidates measure and render. |
| `TextRenderer` | `TextRenderer` | Gets or sets the text renderer used for string fallback content. Defaults to `TextRenderer.Default`; rejects `null`; changing it invalidates render. |

## Important Inherited Properties

| Name | Type | Declared by | Description |
| --- | --- | --- | --- |
| `Content` | `object?` | `ContentControl` | Gets or sets the button content. `UIElement` content is owned as logical and visual content while the classic `Template` property is `null`. |
| `Command` | `ICommand?` | `ButtonBase` | Gets or sets the command executed by pointer or keyboard activation. |
| `CommandParameter` | `object?` | `ButtonBase` | Gets or sets the parameter passed to `Command`. |
| `IsPressed` | `bool` | `ButtonBase` | Gets or sets the pressed visual/input state. |
| `Background` | `DrawColor` | `Control` | Gets or sets the background color. An explicit value overrides fallback state colors. |
| `Foreground` | `DrawColor` | `Control` | Gets or sets the foreground color used by fallback text rendering. |
| `BorderColor` | `DrawColor` | `Control` | Gets or sets the fallback border color. |
| `BorderThickness` | `Thickness` | `Control` | Gets or sets the fallback border thickness and contributes to content insets. |
| `Padding` | `Thickness` | `Control` | Gets or sets the padding around fallback content. |
| `FontFamily` | `string` | `Control` | Gets or sets the inherited font family used to create the fallback text aspect. |
| `FontSize` | `float` | `Control` | Gets or sets the inherited font size used to create the fallback text aspect. |
| `Template` | `ControlTemplate?` | `Control` | Gets or sets the classic control template. When present, the template child handles layout and rendering. |
| `ComponentTemplate` | `ComponentTemplate?` | `Control` | Gets or sets the component template. When present, it takes precedence over `Template`. |
| `TemplateInstance` | `TemplateInstance?` | `Control` | Gets the active classic template instance, when one has been applied. |
| `ComponentTemplateInstance` | `ComponentTemplateInstance?` | `Control` | Gets the active component template instance, when one has been applied. |
| `AspectVariants` | `AspectVariantSet` | `Control` | Gets the active aspect variant values used by component templates. |

## Important Inherited Fields

| Name | Type | Declared by | Description |
| --- | --- | --- | --- |
| `ContentProperty` | `UiProperty<object?>` | `ContentControl` | Identifies the inherited `Content` UI property used by `Button`. |
| `CommandProperty` | `UiProperty<ICommand?>` | `ButtonBase` | Identifies the `Command` UI property. |
| `CommandParameterProperty` | `UiProperty<object?>` | `ButtonBase` | Identifies the `CommandParameter` UI property. |
| `IsPressedProperty` | `UiProperty<bool>` | `ButtonBase` | Identifies the `IsPressed` UI property. |
| `BackgroundProperty` | `UiProperty<DrawColor>` | `Control` | Identifies the `Background` UI property. |
| `ForegroundProperty` | `UiProperty<DrawColor>` | `Control` | Identifies the inherited `Foreground` UI property. |
| `BorderColorProperty` | `UiProperty<DrawColor>` | `Control` | Identifies the `BorderColor` UI property. |
| `BorderThicknessProperty` | `UiProperty<Thickness>` | `Control` | Identifies the `BorderThickness` UI property. |
| `PaddingProperty` | `UiProperty<Thickness>` | `Control` | Identifies the `Padding` UI property. |
| `TemplateProperty` | `UiProperty<ControlTemplate?>` | `Control` | Identifies the `Template` UI property. |
| `ComponentTemplateProperty` | `UiProperty<ComponentTemplate?>` | `Control` | Identifies the `ComponentTemplate` UI property. |

## Important Inherited Methods

| Name | Return Type | Declared by | Description |
| --- | --- | --- | --- |
| `CanExecuteCommand(CommandRouter router, ElementInputRouteMap routeMap)` | `bool` | `ButtonBase` | Returns whether the current command can execute for this button and command parameter. |
| `ExecuteCommand(CommandRouter router, ElementInputRouteMap routeMap)` | `bool` | `ButtonBase` | Executes the current command when the button is enabled and the command can run. |
| `RefreshCommandState(CommandRouter router, ElementInputRouteMap routeMap)` | `bool` | `ButtonBase` | Updates `IsEnabled` from command state and returns whether it changed. |
| `ApplyTemplate()` | `void` | `Control` | Creates or refreshes the active template instance for the current template properties. |
| `SetAspectVariant<TControl, TValue>(AspectVariantKey<TControl, TValue> key, TValue value)` | `void` | `Control` | Sets a component-template aspect variant and invalidates aspect/render state when it changes. |

## Applies to

Project: `Cerneala`

## See also

- `UI/Controls/Button.cs`
- `UI/Controls/Primitives/ButtonBase.cs`
- `UI/Controls/ContentControl.cs`
- `UI/Accessibility/ButtonAutomationPeer.cs`
