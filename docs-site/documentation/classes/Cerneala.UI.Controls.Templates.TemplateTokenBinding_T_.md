# TemplateTokenBinding<T> Class

## Definition

Namespace: `Cerneala.UI.Controls.Templates`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Templates/TemplateTokenBinding.cs`

Represents a typed component-template token binding that copies an `AspectEnvironment` token value into a target `UIElement` property while a component template instance is attached.

```csharp
public sealed class TemplateTokenBinding<T> : TemplateTokenBinding
```

Inheritance:
`object` -> `TemplateTokenBinding` -> `TemplateTokenBinding<T>`

### Type Parameters

| Name | Description |
| --- | --- |
| `T` | The value type shared by the aspect token and the target UI property. |

## Examples

The common path is to register token bindings from a `ComponentTemplateContext`. The created binding is attached by `ComponentTemplateInstance.Attach(Control)`.

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Layout;

AspectToken<Thickness> paddingToken = AspectToken.Thickness("button.padding");
AspectEnvironment environment = new("template");
environment.Set(paddingToken, new Thickness(8));

Button button = new();
Border border = new();

ComponentTemplate<Button> template = new("modern", context =>
{
    context.BindToken(paddingToken, border, Control.PaddingProperty);
    return border;
});

ComponentTemplateInstance instance = template.CreateInstance(
    button,
    new ComponentTemplateContext(button, environment));

instance.Attach(button);

Thickness appliedPadding = border.Padding;
```

## Remarks

`TemplateTokenBinding<T>` stores an `AspectToken<T>`, a target `UIElement`, a target `UiProperty<T>`, and the `AspectEnvironment` used for lookup. The constructor rejects `null` values for all four inputs.

Calling `Attach()` subscribes to the environment and asks for the token value by using `AspectEnvironment.TryGet<T>`. When the environment contains a matching value, the binding writes that value to the target property with `UiPropertyValueSource.TemplateBinding`. When the token is absent or incompatible, the binding clears that value source. Repeated calls do not create duplicate subscriptions.

Calling `Detach()` clears the target property value stored at `UiPropertyValueSource.TemplateBinding`. Component template instances detach token bindings before regular template bindings.

While attached, the binding refreshes the target whenever its token changes and ignores unrelated token changes. `Detach()` removes this subscription, so later environment mutations leave the detached target unchanged.

## Constructors

| Name | Description |
| --- | --- |
| `TemplateTokenBinding(AspectToken<T> token, UIElement target, UiProperty<T> targetProperty, AspectEnvironment environment)` | Creates a token binding for a typed aspect token, target element, target property, and environment. Throws `ArgumentNullException` when any argument is `null`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Attach()` | `void` | Subscribes to the environment and synchronizes the token value through the template-binding value source. |
| `Detach()` | `void` | Removes the environment subscription and clears the target property's template-binding value source. |

## Applies To

Project: `Cerneala`

UI area: retained controls, component templates, aspect-aware templating.

## See Also

- `UI/Controls/Templates/TemplateTokenBinding.cs`
- `UI/Controls/Templates/ComponentTemplateContext.cs`
- `UI/Controls/Templates/ComponentTemplateInstance.cs`
- `UI/Aspect/AspectEnvironment.cs`
- `UI/Aspect/AspectToken{T}.cs`
- `UI/Core/UiProperty{T}.cs`
