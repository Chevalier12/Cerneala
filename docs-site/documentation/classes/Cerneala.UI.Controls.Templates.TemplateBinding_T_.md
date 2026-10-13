# TemplateBinding<T> Class

## Definition
Namespace: `Cerneala.UI.Controls.Templates`

Assembly/Project: `Cerneala`

Source: [`UI/Controls/Templates/TemplateBinding{T}.cs`](https://github.com/Chevalier12/Cerneala/blob/master/UI/Controls/Templates/TemplateBinding%7BT%7D.cs)

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
The common path is to record a binding from a template context with `ComponentTemplateContext.Bind<T>`. `Bind<T>` creates a `TemplateBinding<T>` and returns `void`; the template instance attaches the recorded binding when the control applies the template.

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Media;

Button button = new()
{
    Background = new SolidColorBrush(Color.White),
    ComponentTemplate = new ComponentTemplate<Button>("Button.Binding", context =>
    {
        Border border = new();
        context.Bind(Control.BackgroundProperty, border, Control.BackgroundProperty);
        return border;
    })
};
```

A binding can also be constructed and attached directly. The target follows the owner value only while the binding is attached.

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Media;

SolidColorBrush white = new(Color.White);
SolidColorBrush black = new(Color.Black);
Button owner = new() { Background = white };
Border child = new();

TemplateBinding<Brush?> binding = new(Control.BackgroundProperty, child, Control.BackgroundProperty);
binding.Attach(owner);
// child.Background is white.

owner.Background = black;
// child.Background is black.

binding.Detach();
owner.Background = white;
// child.Background is null: Detach cleared the TemplateOwnerBinding value and the binding no longer listens.
```

## Remarks
`TemplateBinding<T>` stores the source property on the template owner, the target element, the target property, and the value source used when writing the target value. The source and target properties must have the same `T` value type, and the target property cannot be read-only.

Calling `Attach(Control)` subscribes to the owner's `PropertyChanged` event, immediately copies the current owner value to the target, and then updates the target whenever the source property changes. If the initial update fails, the binding removes its event subscription before rethrowing.

The default target source is `UiPropertyValueSource.TemplateOwnerBinding`, which is above the target's own Aspect values and below its markup, animation, and local values. Pass `UiPropertyValueSource.TemplateBinding` for a weaker value that the target's Aspect values override.

Calling `Detach()` stops listening to the owner. Detach is idempotent. When the binding was created with a target source other than `UiPropertyValueSource.TemplateBinding`, which includes the default source, detach also clears that target value source.

## Constructors
| Name | Description |
| --- | --- |
| `TemplateBinding(UiProperty<T> sourceProperty, UIElement target, UiProperty<T> targetProperty, UiPropertyValueSource targetSource = UiPropertyValueSource.TemplateOwnerBinding)` | Creates a typed template binding. Throws when an argument required by the base binding is `null`, when the source and target property value types differ, or when the target property is read-only. |

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
| `Detach()` | Detaches the binding from its current owner and clears the target value when `TargetSource` is not `TemplateBinding`. If the binding is not attached, the method returns without error. |

## Applies to
Project: `Cerneala`

Target framework: `.NET 8`

## See Also
- `TemplateBinding`
- `ComponentTemplateContext`
- `ComponentTemplate<TControl>`
- `UiProperty<T>`
- `UiPropertyValueSource`
