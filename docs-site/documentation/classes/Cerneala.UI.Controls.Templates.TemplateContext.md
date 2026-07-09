# TemplateContext Class

## Definition
Namespace: `Cerneala.UI.Controls.Templates`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Templates/TemplateContext.cs`

Collects the owner control and template bindings while a classic control template factory builds its retained template root.

```csharp
public class TemplateContext
```

Inheritance:
`object` -> `TemplateContext`

Derived:
`TemplateContext<TControl>`

## Examples
Bind a generated child property to the template owner while creating a `ControlTemplate<Button>`:

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

Border root = (Border)button.TemplateInstance!.Root!;
button.Background = DrawColor.Black;
```

Create a context directly and inspect the recorded binding:

```csharp
using Cerneala.UI.Controls;

Button owner = new();
Border target = new();
TemplateContext context = new(owner);

TemplateBinding binding = context.Bind(
    Control.FontSizeProperty,
    target,
    Control.FontSizeProperty);

TemplateBinding sameBinding = context.Bindings[0];
```

## Remarks
`TemplateContext` is the mutable build-time context used by the classic `ControlTemplate<TControl>` pipeline. The template creates a context for the owner, invokes the template factory, then passes `context.Bindings` to the resulting `TemplateInstance`.

The context does not attach bindings by itself. Calling `Bind` records a `TemplateBinding`; the returned `TemplateInstance` attaches those bindings when the instance is attached to the template owner and detaches them when the instance is detached or disposed.

Use `TemplateContext<TControl>` when a template factory needs a strongly typed `Owner`. The non-generic base class exposes the same binding collection and binding helpers for code that only needs the owner as `Control`.

Bindings require source and target properties with the same value type, and the target property must be writable. The default binding source used by `TemplateBinding.Create` is `UiPropertyValueSource.TemplateBinding`.

## Constructors
| Name | Description |
| --- | --- |
| `TemplateContext(Control owner)` | Initializes a context for `owner`. Throws `ArgumentNullException` when `owner` is `null`. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `Owner` | `Control` | Gets the control that owns the template being built. |
| `Bindings` | `IReadOnlyList<TemplateBinding>` | Gets a read-only view of the template bindings recorded through `Bind`. |

## Methods
| Name | Return Type | Description |
| --- | --- | --- |
| `Bind<T>(UiProperty<T> sourceProperty, UIElement target, UiProperty<T> targetProperty)` | `TemplateBinding<T>` | Creates a typed template binding from an owner property to a target element property, records it in `Bindings`, and returns it. |
| `Bind(UiProperty sourceProperty, UIElement target, UiProperty targetProperty)` | `TemplateBinding` | Creates a template binding for matching non-generic property metadata, records it in `Bindings`, and returns it. |

## Exceptions
| Member | Exception | Condition |
| --- | --- | --- |
| `TemplateContext(Control owner)` | `ArgumentNullException` | `owner` is `null`. |
| `Bind<T>(UiProperty<T>, UIElement, UiProperty<T>)` | `ArgumentNullException` | `sourceProperty`, `target`, or `targetProperty` is `null`. |
| `Bind<T>(UiProperty<T>, UIElement, UiProperty<T>)` | `ArgumentException` | Source and target property value types do not match, or the target property is read-only. |
| `Bind(UiProperty, UIElement, UiProperty)` | `ArgumentNullException` | `sourceProperty`, `target`, or `targetProperty` is `null`. |
| `Bind(UiProperty, UIElement, UiProperty)` | `ArgumentException` | Source and target property value types do not match, or the target property is read-only. |

## Applies To
Project: `Cerneala`

UI area: retained controls and classic control templating.

## See Also
- `UI/Controls/Templates/TemplateContext.cs`
- `UI/Controls/Templates/TemplateBinding{T}.cs`
- `UI/Controls/Templates/TemplateInstance.cs`
- `UI/Controls/Templates/ControlTemplate{TControl}.cs`
