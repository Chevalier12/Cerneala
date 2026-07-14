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
using Cerneala.UI.Media;

Control control = new()
{
    Background = new SolidColorBrush(Color.White),
    Foreground = new SolidColorBrush(Color.Black),
    BorderBrush = new SolidColorBrush(Color.Black),
    BorderThickness = new Thickness(1),
    Padding = new Thickness(8),
    FontFamily = "Default",
    FontSize = 16
};
```

Apply a control template:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;

Control control = new()
{
    ComponentTemplate = new ComponentTemplate<Control>("Control.Default", context =>
    {
        Border border = new()
        {
            Padding = new Thickness(4),
            BorderThickness = new Thickness(1)
        };

        context.Bind(Control.BackgroundProperty, border, Control.BackgroundProperty);
        context.Bind(Control.BorderBrushProperty, border, Control.BorderBrushProperty);

        return border;
    }),
    Background = new SolidColorBrush(Color.White),
    BorderBrush = new SolidColorBrush(Color.Black)
};

control.ApplyTemplate();
ComponentTemplateInstance? instance = control.ComponentTemplateInstance;
```

## Remarks

`Control` is the common base for controls that need shared chrome, text styling, template support, and aspect variants. It does not render fallback chrome by itself; derived controls or template roots use properties such as `Background`, `BorderBrush`, `BorderThickness`, `Padding`, `Foreground`, `FontFamily`, and `FontSize`.

`ComponentTemplate` stores the control's only template type. `ApplyTemplate()` creates and attaches the matching `ComponentTemplateInstance`, or detaches the current instance when the property is cleared. Reapplying the same template keeps the existing instance and generated root. Template replacement first notifies the derived control to unsubscribe from its old parts, disposes the old instance, attaches and validates the new parts, and only then publishes the new instance. A failed part hook disposes the new instance and leaves no partially attached template root.

Derived templated controls override `OnTemplateApplied(ComponentTemplateInstance?)` to release old part subscriptions and resolve the new parts. `GetRequiredTemplatePart<TElement>` fails immediately when a named part is absent or has the wrong type. `GetOptionalTemplatePart<TElement>` permits an absent part but still rejects a registered part of the wrong type. Both error forms identify the part name and expected type.

Layout is delegated to the active template child. During measure and arrange, `Control` calls `ApplyTemplate()` and then measures or arranges `ComponentTemplateInstance.Root`; if no template child exists, measurement returns `LayoutSize.Zero`.

Component-template changes affect measure, arrange, render, hit testing, and input visuals. `Background` and `BorderBrush` affect render and input visuals. `Foreground` accepts the same solid, gradient, image, drawing, and visual brushes, is inherited, and affects render. A `null` foreground suppresses text drawing. `BorderThickness`, `Padding`, `FontFamily`, and `FontSize` affect measurement and rendering; `FontFamily`, `FontSize`, and `Foreground` inherit through the UI property system.

`Padding` and `BorderThickness` reject negative, `NaN`, and infinite side values. `FontFamily` rejects `null`, empty, and whitespace-only values. `FontSize` must be finite and greater than zero.

`SetAspectVariant<TControl, TValue>` updates `AspectVariants`. When the value actually changes, the control invalidates aspect and render state.

## Constructors

| Name | Description |
| --- | --- |
| `Control()` | Initializes a new control with the default UI property values. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `ComponentTemplateProperty` | `UiProperty<ComponentTemplate?>` | Identifies the `ComponentTemplate` UI property. Defaults to `null`; affects measure, arrange, render, hit testing, and input visuals. |
| `BackgroundProperty` | `UiProperty<Brush?>` | Identifies the `Background` UI property. Defaults to `null`; affects render and input visuals. |
| `ForegroundProperty` | `UiProperty<Brush?>` | Identifies the inherited `Foreground` UI property. Defaults to `new SolidColorBrush(Color.Black)`; affects render. |
| `BorderBrushProperty` | `UiProperty<Brush?>` | Identifies the `BorderBrush` UI property. Defaults to `null`; affects render and input visuals. |
| `BorderThicknessProperty` | `UiProperty<Thickness>` | Identifies the `BorderThickness` UI property. Defaults to `Thickness.Zero`; affects measure and render; each side must be finite and non-negative. |
| `PaddingProperty` | `UiProperty<Thickness>` | Identifies the `Padding` UI property. Defaults to `Thickness.Zero`; affects measure and render; each side must be finite and non-negative. |
| `FontFamilyProperty` | `UiProperty<string>` | Identifies the inherited `FontFamily` UI property. Defaults to `"Default"`; affects measure and render; rejects empty or whitespace-only values. |
| `FontSizeProperty` | `UiProperty<float>` | Identifies the inherited `FontSize` UI property. Defaults to `16`; affects measure and render; must be finite and greater than zero. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Background` | `Brush?` | Gets or sets the control background brush. `null` draws no background. |
| `Foreground` | `Brush?` | Gets or sets the inherited foreground brush used by text and derived controls. `null` suppresses foreground drawing. |
| `BorderBrush` | `Brush?` | Gets or sets the control border brush. `null` draws no border. |
| `BorderThickness` | `Thickness` | Gets or sets the border thickness. Values must be finite and non-negative. |
| `Padding` | `Thickness` | Gets or sets the padding inside the border. Values must be finite and non-negative. |
| `FontFamily` | `string` | Gets or sets the inherited font family. The value cannot be empty or whitespace-only. |
| `FontSize` | `float` | Gets or sets the inherited font size. The value must be finite and greater than zero. |
| `ComponentTemplate` | `ComponentTemplate?` | Gets or sets the component template. |
| `ComponentTemplateInstance` | `ComponentTemplateInstance?` | Gets the active component template instance after `ApplyTemplate()` creates one. |
| `AspectVariants` | `AspectVariantSet` | Gets the current aspect variant values passed to component templates. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `ApplyTemplate()` | `void` | Creates, reuses, or detaches the component template instance so it matches `ComponentTemplate`. |
| `SetAspectVariant<TControl, TValue>(AspectVariantKey<TControl, TValue> key, TValue value)` | `void` | Sets an aspect variant value and invalidates aspect/render state when the variant set changes. |

## Protected Properties

| Name | Type | Description |
| --- | --- | --- |
| `TemplateChild` | `UIElement?` | Gets the root element from the active component template instance or template instance. |
| `Insets` | `Thickness` | Gets the combined `Padding` and `BorderThickness` values for each side. |

## Protected Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `MeasureCore(MeasureContext context)` | `LayoutSize` | Applies the current template and measures the active template child, or returns `LayoutSize.Zero` when no template child exists. |
| `ArrangeCore(ArrangeContext context)` | `LayoutRect` | Applies the current template, arranges the active template child, and returns `context.FinalRect`. |
| `OnTemplateApplied(ComponentTemplateInstance? instance)` | `void` | Releases old template-part state when `instance` is `null`, or connects validated parts from the newly attached instance. |
| `GetRequiredTemplatePart<TElement>(string name)` | `TElement` | Returns a required named part and throws `InvalidOperationException` when it is absent or has the wrong type. |
| `GetOptionalTemplatePart<TElement>(string name)` | `TElement?` | Returns an optional named part, `null` when absent, and throws `InvalidOperationException` when present with the wrong type. |
| `OnPropertyChanged(UiPropertyChangedEventArgs args)` | `void` | Applies the template when `ComponentTemplate` changes, after the base property-change handling runs. |

## Migration

`Background`, `BorderBrush`, and `Foreground` no longer accept `Color`. There are no `BorderColor` or `ForegroundColor` compatibility aliases. Wrap solid colors explicitly:

```csharp
// Before
control.Background = Color.White;
control.BorderColor = Color.Red;
control.Foreground = Color.Black;

// After
control.Background = new SolidColorBrush(Color.White);
control.BorderBrush = new SolidColorBrush(Color.Red);
control.Foreground = new SolidColorBrush(Color.Black);
```

## Applies to

Project: `Cerneala`

## See also

- `UI/Controls/Control.cs`
- `UI/Controls/Templates/ComponentTemplate.cs`
- `UI/Elements/UIElement.cs`
