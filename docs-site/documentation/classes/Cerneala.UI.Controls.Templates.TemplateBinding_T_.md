# TemplateBinding<T> Class

## Definition
Namespace: `Cerneala.UI.Controls.Templates`

Assembly/Project: `Cerneala`

Source: [`UI/Controls/Templates/TemplateBinding{T}.cs`](../../UI/Controls/TemplateBinding%7BT%7D.cs)

Represents a typed template binding that copies a property value from a template owner `Control` to a target `UIElement` property and keeps it synchronized while attached.

```csharp
public sealed class TemplateBinding<T> : TemplateBinding
```

Inheritance:
`object` -> `TemplateBinding` -> `TemplateBinding<T>`

### Type Parameters
| Name | Description |
| --- | --- |
| `T` | The value type shared by the source and target UI properties. |

## Examples
The common path is to create a binding from a template context. The binding is attached by the template instance when the control applies the template.

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;

Button button = new() { Background = Color.White };
Border? border = null;

button.ComponentTemplate = new ComponentTemplate<Button>("Button.Binding", context =>
{
    border = new Border();
    TemplateBinding<Color> binding = context.Bind(
        Control.BackgroundProperty,
        border,
        Control.BackgroundProperty);

    return border;
});

button.Background = Color.Black;

// The generated border follows the owner control value while attached.
Color currentBackground = border!.Background;
```

## Remarks
`TemplateBinding<T>` stores the source property on the template owner, the target element, the target property, and the value source used when writing the target value. The source and target properties must have the same `T` value type, and the target property cannot be read-only.

Calling `Attach(Control)` subscribes to the owner's `PropertyChanged` event, immediately copies the current owner value to the target, and then updates the target whenever the source property changes. If the initial update fails, the binding removes its event subscription before rethrowing.

Calling `Detach()` stops listening to the owner. Detach is idempotent. When the binding was created with a target source other than `UiPropertyValueSource.TemplateBinding`, detach also clears that target value source.

## Constructors
| Name | Description |
| --- | --- |
| `TemplateBinding(UiProperty<T> sourceProperty, UIElement target, UiProperty<T> targetProperty, UiPropertyValueSource targetSource = UiPropertyValueSource.TemplateBinding)` | Creates a typed template binding. Throws when an argument required by the base binding is `null`, when the source and target property value types differ, or when the target property is read-only. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `SourceProperty` | `UiProperty<T>` | Gets the owner property that supplies values to the binding. |
| `Target` | `UIElement` | Gets the element that receives copied values. Inherited from `TemplateBinding`. |
| `TargetProperty` | `UiProperty<T>` | Gets the target property that receives copied values. |
| `TargetSource` | `UiPropertyValueSource` | Gets the value source used when setting or clearing the target property. Inherited from `TemplateBinding`. |

## Methods
| Name | Description |
| --- | --- |
| `Attach(Control owner)` | Attaches the binding to a control, copies the current source value to the target, and begins tracking later source property changes. Throws `ArgumentNullException` when `owner` is `null` and `InvalidOperationException` when the binding is already attached. |
| `Detach()` | Detaches the binding from its current owner. If the binding is not attached, the method returns without error. |

## Applies to
Project: `Cerneala`

Target framework: `.NET 8`

## See Also
- `TemplateBinding`
- `ComponentTemplateContext`
- `ComponentTemplate<TControl>`
- `UiProperty<T>`
- `UiPropertyValueSource`
