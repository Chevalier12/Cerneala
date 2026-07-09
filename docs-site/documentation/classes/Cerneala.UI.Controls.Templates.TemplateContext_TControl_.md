# TemplateContext<TControl> Class

## Definition

Namespace: `Cerneala.UI.Controls.Templates`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Templates/TemplateContext.cs`

Provides a strongly typed build-time context for classic control template factories.

```csharp
public sealed class TemplateContext<TControl> : TemplateContext
    where TControl : Control
```

Inheritance:
`object` -> `TemplateContext` -> `TemplateContext<TControl>`

Type parameters:

| Name | Constraints | Description |
| --- | --- | --- |
| `TControl` | `Control` | The concrete owner control type exposed by the strongly typed `Owner` property. |

## Examples

Create a typed button template and read the owner as `Button` without casting:

```csharp
using Cerneala.UI.Controls;

Button button = new()
{
    Content = "Save"
};

button.Template = new ControlTemplate<Button>(context =>
{
    return new ContentPresenter
    {
        Content = context.Owner.Content
    };
});

button.ApplyTemplate();
```

Record a template binding while building a retained template root:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;

Button button = new()
{
    Background = DrawColor.White
};

button.Template = new ControlTemplate<Button>(context =>
{
    Border border = new();
    context.Bind(Control.BackgroundProperty, border, Control.BackgroundProperty);
    return border;
});

button.ApplyTemplate();
```

## Remarks

`TemplateContext<TControl>` is the generic counterpart of `TemplateContext`. It keeps the same binding collection and `Bind` helpers from the base class, but hides `TemplateContext.Owner` with a strongly typed `Owner` property.

`ControlTemplate<TControl>` creates this context when building a `TemplateInstance`. The template factory receives the context, returns a root `UIElement`, and any bindings recorded through `Bind` are passed to the `TemplateInstance`.

The context records bindings; it does not attach them itself. A template binding is attached when the template instance is attached to the owner and detached when that template instance is detached.

## Constructors

| Name | Description |
| --- | --- |
| `TemplateContext(TControl owner)` | Initializes a context for `owner`. Throws `ArgumentNullException` when `owner` is `null`. |

## Properties

| Name | Type | Declared by | Description |
| --- | --- | --- | --- |
| `Owner` | `TControl` | `TemplateContext<TControl>` | Gets the owner control using the specific template owner type. |
| `Owner` | `Control` | `TemplateContext` | Gets the owner control as the base `Control` type. Hidden by the generic `Owner` property. |
| `Bindings` | `IReadOnlyList<TemplateBinding>` | `TemplateContext` | Gets a read-only view of bindings recorded through `Bind`. |

## Methods

| Name | Return Type | Declared by | Description |
| --- | --- | --- | --- |
| `Bind<T>(UiProperty<T> sourceProperty, UIElement target, UiProperty<T> targetProperty)` | `TemplateBinding<T>` | `TemplateContext` | Creates a typed binding from an owner property to a target element property, records it in `Bindings`, and returns it. |
| `Bind(UiProperty sourceProperty, UIElement target, UiProperty targetProperty)` | `TemplateBinding` | `TemplateContext` | Creates a binding from non-generic property metadata, records it in `Bindings`, and returns it. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `TemplateContext(TControl owner)` | `ArgumentNullException` | `owner` is `null`. |
| `Bind<T>(UiProperty<T>, UIElement, UiProperty<T>)` | `ArgumentNullException` | `sourceProperty`, `target`, or `targetProperty` is `null`. |
| `Bind<T>(UiProperty<T>, UIElement, UiProperty<T>)` | `ArgumentException` | Source and target property value types do not match, or the target property is read-only. |
| `Bind(UiProperty, UIElement, UiProperty)` | `ArgumentNullException` | `sourceProperty`, `target`, or `targetProperty` is `null`. |
| `Bind(UiProperty, UIElement, UiProperty)` | `ArgumentException` | Source and target property value types do not match, or the target property is read-only. |

## Applies to

Project: `Cerneala`

UI area: retained controls and classic control templating.

## See also

- `UI/Controls/Templates/TemplateContext.cs`
- `UI/Controls/Templates/ControlTemplate{TControl}.cs`
- `UI/Controls/Templates/TemplateBinding{T}.cs`
- `UI/Controls/Templates/TemplateInstance.cs`
