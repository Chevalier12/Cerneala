# ControlTemplate Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/ControlTemplate.cs`

Defines the abstract base contract for classic control templates that create a retained `TemplateInstance` for a compatible `Control` owner.

```csharp
public abstract class ControlTemplate
```

Inheritance:
`object` -> `ControlTemplate`

Derived:
`ControlTemplate<TControl>`

## Examples
Assign a typed template to a button and let the control apply it.

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

TemplateInstance instance = button.TemplateInstance!;
```

Create a template instance directly when the owner type is known to be compatible.

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;

Control owner = new();
ControlTemplate template = new ControlTemplate<Control>(_ => new UIElement());

TemplateInstance instance = template.CreateInstance(owner);
```

## Remarks
`ControlTemplate` stores the `OwnerType` that a template can be applied to. `CreateInstance(Control)` validates that the supplied owner is not `null` and is an instance of `OwnerType`; if the owner is valid, it delegates creation to the derived template implementation.

The public concrete implementation is `ControlTemplate<TControl>`. It sets `OwnerType` to `typeof(TControl)`, creates a `TemplateContext<TControl>` for the owner, invokes the factory, and returns a `TemplateInstance` containing the generated root and any bindings recorded in the context.

`Control.ApplyTemplate()` is the normal entry point for applying a classic template. It creates a `TemplateInstance`, attaches the instance to the control, and stores it in `Control.TemplateInstance`. Reapplying the same template reuses the existing instance; replacing the template detaches the old template root before attaching the new one.

Template roots participate in the retained UI tree after attachment. Tests cover logical and visual parent assignment, layout, rendering, hit testing, routed input, aspect state, and cleanup when template attachment fails.

The base constructor is `private protected`, so external consumers normally use `ControlTemplate<TControl>` rather than deriving new `ControlTemplate` types outside the `Cerneala` assembly.

## Constructors
This class does not expose public constructors.

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `OwnerType` | `Type` | Gets the concrete `Control` type that this template accepts as an owner. The type is supplied by derived templates and must derive from `Control`. |

## Methods
| Name | Return Type | Description |
| --- | --- | --- |
| `CreateInstance(Control owner)` | `TemplateInstance` | Creates a template instance for `owner` after checking that the owner is not `null` and is compatible with `OwnerType`. |

## Exceptions
| Member | Exception | Condition |
| --- | --- | --- |
| `ControlTemplate(Type ownerType)` | `ArgumentException` | `ownerType` does not derive from `Control`. |
| `CreateInstance(Control owner)` | `ArgumentNullException` | `owner` is `null`. |
| `CreateInstance(Control owner)` | `InvalidOperationException` | `owner` is not an instance of `OwnerType`. |

## Applies To
Project: `Cerneala`

UI area: retained controls and classic control templating.

## See Also
- `UI/Controls/ControlTemplate.cs`
- `UI/Controls/ControlTemplate{TControl}.cs`
- `UI/Controls/Control.cs`
- `UI/Controls/TemplateInstance.cs`
- `UI/Controls/TemplateContext.cs`
