# Control Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Control.cs`

Defines the base class for retained UI controls that expose common visual properties, font properties, and template application.

```csharp
public class Control : UIElement
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Control`

## Examples

Create a control and configure its common visual properties:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Layout;

Control control = new()
{
    Background = DrawColor.White,
    Foreground = DrawColor.Black,
    BorderColor = DrawColor.Black,
    BorderThickness = new Thickness(1),
    Padding = new Thickness(8),
    FontFamily = "Default",
    FontSize = 16
};
```

Apply a classic control template:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Layout;

Control control = new()
{
    Template = new ControlTemplate<Control>(context =>
    {
        Border border = new()
        {
            Padding = new Thickness(4),
            BorderThickness = new Thickness(1)
        };

        context.Bind(Control.BackgroundProperty, border, Control.BackgroundProperty);
        context.Bind(Control.BorderColorProperty, border, Control.BorderColorProperty);

        return border;
    }),
    Background = DrawColor.White,
    BorderColor = DrawColor.Black
};

control.ApplyTemplate();
TemplateInstance? instance = control.TemplateInstance;
```

## Remarks

`Control` is the common base for controls that need shared chrome, text styling, template support, and aspect variants. It does not render fallback chrome by itself; derived controls or template roots use properties such as `Background`, `BorderColor`, `BorderThickness`, `Padding`, `Foreground`, `FontFamily`, and `FontSize`.

`Template` stores a classic `ControlTemplate`. `ComponentTemplate` stores the newer component template type and takes precedence when both template properties are set. `ApplyTemplate()` creates the matching template instance, attaches it to the control, and detaches the previous classic or component template instance when the active template changes. If `ComponentTemplate` is not `null`, the classic `Template` path is skipped.

Layout is delegated to the active template child. During measure and arrange, `Control` calls `ApplyTemplate()` and then measures or arranges `ComponentTemplateInstance.Root` or `TemplateInstance.Root`; if no template child exists, measurement returns `LayoutSize.Zero`.

Template and component-template changes affect measure, arrange, render, hit testing, and input visuals. `Background` and `BorderColor` affect render and input visuals. `Foreground` is inherited and affects render. `BorderThickness`, `Padding`, `FontFamily`, and `FontSize` affect measurement and rendering; `FontFamily`, `FontSize`, and `Foreground` inherit through the UI property system.

`Padding` and `BorderThickness` reject negative, `NaN`, and infinite side values. `FontFamily` rejects `null`, empty, and whitespace-only values. `FontSize` must be finite and greater than zero.

`SetAspectVariant<TControl, TValue>` updates `AspectVariants`. When the value actually changes, the control invalidates aspect and render state.

## Constructors

| Name | Description |
| --- | --- |
| `Control()` | Initializes a new control with the default UI property values. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `TemplateProperty` | `UiProperty<ControlTemplate?>` | Identifies the `Template` UI property. Defaults to `null`; affects measure, arrange, render, hit testing, and input visuals. |
| `ComponentTemplateProperty` | `UiProperty<ComponentTemplate?>` | Identifies the `ComponentTemplate` UI property. Defaults to `null`; affects measure, arrange, render, hit testing, and input visuals. |
| `BackgroundProperty` | `UiProperty<DrawColor>` | Identifies the `Background` UI property. Defaults to `DrawColor.Transparent`; affects render and input visuals. |
| `ForegroundProperty` | `UiProperty<DrawColor>` | Identifies the inherited `Foreground` UI property. Defaults to `DrawColor.Black`; affects render. |
| `BorderColorProperty` | `UiProperty<DrawColor>` | Identifies the `BorderColor` UI property. Defaults to `DrawColor.Transparent`; affects render and input visuals. |
| `BorderThicknessProperty` | `UiProperty<Thickness>` | Identifies the `BorderThickness` UI property. Defaults to `Thickness.Zero`; affects measure and render; each side must be finite and non-negative. |
| `PaddingProperty` | `UiProperty<Thickness>` | Identifies the `Padding` UI property. Defaults to `Thickness.Zero`; affects measure and render; each side must be finite and non-negative. |
| `FontFamilyProperty` | `UiProperty<string>` | Identifies the inherited `FontFamily` UI property. Defaults to `"Default"`; affects measure and render; rejects empty or whitespace-only values. |
| `FontSizeProperty` | `UiProperty<float>` | Identifies the inherited `FontSize` UI property. Defaults to `16`; affects measure and render; must be finite and greater than zero. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Background` | `DrawColor` | Gets or sets the control background color. |
| `Foreground` | `DrawColor` | Gets or sets the inherited foreground color used by derived controls or templates. |
| `BorderColor` | `DrawColor` | Gets or sets the control border color. |
| `BorderThickness` | `Thickness` | Gets or sets the border thickness. Values must be finite and non-negative. |
| `Padding` | `Thickness` | Gets or sets the padding inside the border. Values must be finite and non-negative. |
| `FontFamily` | `string` | Gets or sets the inherited font family. The value cannot be empty or whitespace-only. |
| `FontSize` | `float` | Gets or sets the inherited font size. The value must be finite and greater than zero. |
| `Template` | `ControlTemplate?` | Gets or sets the classic control template. Used when `ComponentTemplate` is `null`. |
| `TemplateInstance` | `TemplateInstance?` | Gets the active classic template instance after `ApplyTemplate()` creates one. |
| `ComponentTemplate` | `ComponentTemplate?` | Gets or sets the component template. Takes precedence over `Template`. |
| `ComponentTemplateInstance` | `ComponentTemplateInstance?` | Gets the active component template instance after `ApplyTemplate()` creates one. |
| `AspectVariants` | `AspectVariantSet` | Gets the current aspect variant values passed to component templates. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `ApplyTemplate()` | `void` | Creates, reuses, or detaches template instances so the active instance matches `ComponentTemplate` or `Template`. |
| `SetAspectVariant<TControl, TValue>(AspectVariantKey<TControl, TValue> key, TValue value)` | `void` | Sets an aspect variant value and invalidates aspect/render state when the variant set changes. |

## Protected Properties

| Name | Type | Description |
| --- | --- | --- |
| `TemplateChild` | `UIElement?` | Gets the root element from the active component template instance or classic template instance. |
| `Insets` | `Thickness` | Gets the combined `Padding` and `BorderThickness` values for each side. |

## Protected Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `MeasureCore(MeasureContext context)` | `LayoutSize` | Applies the current template and measures the active template child, or returns `LayoutSize.Zero` when no template child exists. |
| `ArrangeCore(ArrangeContext context)` | `LayoutRect` | Applies the current template, arranges the active template child, and returns `context.FinalRect`. |
| `OnPropertyChanged(UiPropertyChangedEventArgs args)` | `void` | Applies templates when `Template` or `ComponentTemplate` changes, after the base property-change handling runs. |

## Applies to

Project: `Cerneala`

## See also

- `UI/Controls/Control.cs`
- `UI/Controls/Templates/ControlTemplate.cs`
- `UI/Controls/Templates/ControlTemplate{TControl}.cs`
- `UI/Controls/Templates/ComponentTemplate.cs`
- `UI/Elements/UIElement.cs`
