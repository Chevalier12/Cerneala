# TemplateTokenBinding Class

## Definition
Namespace: `Cerneala.UI.Controls.Templates`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Templates/TemplateTokenBinding.cs`

Represents a component-template token binding that can apply an aspect token value to a generated template child and later clear that applied value.

```csharp
public abstract class TemplateTokenBinding
```

Inheritance:
`object` -> `TemplateTokenBinding`

Derived:
`TemplateTokenBinding<T>`

## Examples
Bind a generated `Border` padding property to a token value stored in the component template context environment:

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
ComponentTemplateContext context = new(button, environment);

context.BindToken(paddingToken, border, Control.PaddingProperty);

TemplateTokenBinding binding = context.TokenBindings[0];
binding.Attach();

Thickness appliedPadding = border.Padding;
binding.Detach();
```

Use `BindToken` inside a component template factory so the instance attaches the token binding with the rest of the template:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Layout;

AspectToken<Thickness> paddingToken = AspectToken.Thickness("button.padding");

Button button = new();
ComponentTemplate<Button> template = new("Button.Padding", context =>
{
    Border root = new();
    context.BindToken(paddingToken, root, Control.PaddingProperty);
    return root;
});
```

## Remarks
`TemplateTokenBinding` is the non-generic base used by component template instances to store token bindings with different value types in one collection. `ComponentTemplateContext.BindToken<T>` creates the concrete `TemplateTokenBinding<T>` implementation and adds it to `ComponentTemplateContext.TokenBindings`.

The concrete binding captures an `AspectToken<T>`, a target `UIElement`, a typed target `UiProperty<T>`, and an `AspectEnvironment`. When `Attach()` runs, it subscribes to that environment and immediately resolves the token. A compatible value is written with `UiPropertyValueSource.TemplateBinding`; a missing or incompatible value clears that value source.

`Detach()` clears the target property value that was written through `UiPropertyValueSource.TemplateBinding`. Component template instances call token binding `Attach()` after regular template bindings during `ComponentTemplateInstance.Attach(Control)`, and call token binding `Detach()` before regular template bindings during `ComponentTemplateInstance.Detach()`.

While attached, later changes to the captured token are applied immediately. Changes to unrelated tokens are ignored. `Detach()` removes the subscription before clearing the target, so later environment mutations cannot update a detached template child.

## Methods
| Name | Return Type | Description |
| --- | --- | --- |
| `Attach()` | `void` | Subscribes to the environment and synchronizes the current token value to the target. Repeated calls are safe. |
| `Detach()` | `void` | Removes the environment subscription and clears the target property's template-binding value source. Repeated calls are safe. |

## Applies To
Project: `Cerneala`

UI area: retained controls, component templates, aspect-aware templating.

## See Also
- `UI/Controls/Templates/TemplateTokenBinding.cs`
- `UI/Controls/Templates/ComponentTemplateContext.cs`
- `UI/Controls/Templates/ComponentTemplateInstance.cs`
- `UI/Aspect/AspectEnvironment.cs`
- `UI/Aspect/AspectToken.cs`
- `UI/Core/UiPropertyValueSource.cs`
