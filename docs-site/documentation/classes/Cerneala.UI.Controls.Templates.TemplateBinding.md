# TemplateBinding Class

## Definition
Namespace: `Cerneala.UI.Controls.Templates`
Assembly/Project: `Cerneala`
Source: `UI/Controls/Templates/TemplateBinding{T}.cs`

Represents a template-owned binding that copies a property value from a templated `Control` owner to a property on a generated template child.

```csharp
public abstract class TemplateBinding
```

Generic implementation:

```csharp
public sealed class TemplateBinding<T> : TemplateBinding
```

Inheritance:  
`Object` -> `TemplateBinding` -> `TemplateBinding<T>`

## Examples

Bind a generated `Border` background to the templated `Button` background from inside a control template:

```csharp
Button button = new()
{
    Background = Color.White,
    ComponentTemplate = new ComponentTemplate<Button>("Button.Binding", context =>
    {
        Border border = new();
        context.Bind(Control.BackgroundProperty, border, Control.BackgroundProperty);
        return border;
    })
};
```

Create and attach a typed binding directly:

```csharp
UiProperty<float> ownerFontSizeProperty = Control.FontSizeProperty;

Button owner = new();
Border child = new();
TemplateBinding<float> binding = new(
    ownerFontSizeProperty,
    child,
    Control.FontSizeProperty);

binding.Attach(owner);
```

## Remarks

`TemplateBinding` is used by the template system to keep generated children synchronized with properties on the control that owns the template. `ComponentTemplateContext.Bind` creates these bindings while a `ComponentTemplate<TControl>` builds its visual tree, and `ComponentTemplateInstance.Attach` attaches each recorded binding to the template owner.

When attached, a binding immediately copies the current source property value from the owner to the target property on the generated child. It also subscribes to the owner's `PropertyChanged` event and updates the target again when the same source property changes.

The source and target properties must have the same `ValueType`. The target property must not be read-only. These checks happen before the binding is used, so invalid template bindings fail before a broken template root remains attached.

The default value source written to the target is `UiPropertyValueSource.TemplateBinding`. A custom `UiPropertyValueSource` can be supplied when constructing or creating the binding. `Detach` always removes the owner subscription; when the target source is not `TemplateBinding`, it also clears that value source from the target.

`Attach` is single-use while the binding is attached. Calling it again before `Detach` throws `InvalidOperationException`. `Detach` is idempotent when the binding is not attached.

## Constructors

| Name | Description |
| --- | --- |
| `TemplateBinding<T>(UiProperty<T> sourceProperty, UIElement target, UiProperty<T> targetProperty, UiPropertyValueSource targetSource = UiPropertyValueSource.TemplateBinding)` | Initializes a typed template binding from an owner property to a target child property. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `SourceProperty` | `UiProperty` | Gets the owner property read by the binding. |
| `Target` | `UIElement` | Gets the generated template child that receives the value. |
| `TargetProperty` | `UiProperty` | Gets the target property written on `Target`. |
| `TargetSource` | `UiPropertyValueSource` | Gets the property value source used when writing the target property. |
| `TemplateBinding<T>.SourceProperty` | `UiProperty<T>` | Gets the typed owner property read by the generic binding. |
| `TemplateBinding<T>.TargetProperty` | `UiProperty<T>` | Gets the typed target property written by the generic binding. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Attach(Control owner)` | `void` | Attaches the binding to a template owner, copies the current owner value to the target, and starts listening for owner property changes. |
| `Detach()` | `void` | Detaches the binding from its owner and stops listening for owner property changes. |
| `Create(UiProperty sourceProperty, UIElement target, UiProperty targetProperty, UiPropertyValueSource targetSource = UiPropertyValueSource.TemplateBinding)` | `TemplateBinding` | Creates a typed `TemplateBinding<T>` instance for the source property value type. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| Constructor | `ArgumentNullException` | `sourceProperty`, `target`, or `targetProperty` is `null`. |
| Constructor | `ArgumentException` | Source and target property value types differ, or the target property is read-only. |
| `Create` | `ArgumentNullException` | `sourceProperty`, `target`, or `targetProperty` is `null`. |
| `Create` | `ArgumentException` | Source and target property value types differ, or the target property is read-only. |
| `Attach` | `ArgumentNullException` | `owner` is `null`. |
| `Attach` | `InvalidOperationException` | The binding is already attached. |

## Applies to

Cerneala retained UI control templates.

## See Also

- `ComponentTemplateContext`
- `ComponentTemplateInstance`
- `ComponentTemplate<TControl>`
- `UiProperty<T>`
- `UiPropertyValueSource`
