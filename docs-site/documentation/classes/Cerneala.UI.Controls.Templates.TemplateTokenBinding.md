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

The concrete binding captures an `AspectToken<T>`, a target `UIElement`, a typed target `UiProperty<T>`, and an `AspectEnvironment`. When `Attach()` runs, it calls `AspectEnvironment.TryGet<T>`. If the token resolves successfully, the value is written to the target property with `UiPropertyValueSource.TemplateBinding`. If the token is missing or resolves to an incompatible value, no target value is written.

`Detach()` clears the target property value that was written through `UiPropertyValueSource.TemplateBinding`. Component template instances call token binding `Attach()` after regular template bindings during `ComponentTemplateInstance.Attach(Control)`, and call token binding `Detach()` before regular template bindings during `ComponentTemplateInstance.Detach()`.

This type does not listen for later `AspectEnvironment` changes. It applies the environment value available at attach time.

## Methods
| Name | Return Type | Description |
| --- | --- | --- |
| `Attach()` | `void` | Applies the token value from the captured aspect environment to the target property when the token can be resolved. |
| `Detach()` | `void` | Clears the target property value written through the template-binding value source. |

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
