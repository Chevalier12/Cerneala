# TemplateInstance Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/TemplateInstance.cs`

Represents one materialized classic control template, including its root element and template bindings.

```csharp
public sealed class TemplateInstance : IDisposable
```

Inheritance:
`object` -> `TemplateInstance`

Implements:
`IDisposable`

## Examples
Create and attach a template instance directly:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;

Control owner = new();
UIElement root = new();

using TemplateInstance instance = new(root);
instance.Attach(owner);

bool isAttached = instance.IsAttached;

instance.Detach();
```

Create a template instance through a `ControlTemplate`:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;

Control owner = new();
ControlTemplate template = new ControlTemplate<Control>(_ => new UIElement());

TemplateInstance instance = template.CreateInstance(owner);
instance.Attach(owner);
```

## Remarks
`TemplateInstance` is the retained runtime object produced by classic `ControlTemplate` creation. `ControlTemplate<TControl>` builds a `TemplateContext<TControl>`, invokes the template factory, and returns a `TemplateInstance` containing the factory root and the bindings recorded by the context.

`Attach(Control)` connects the instance to a single template owner. When `Root` is not `null`, the root is added to the owner's logical and visual child collections through the template child ownership helper. Each recorded `TemplateBinding` is then attached to the same owner. If any part of attachment fails, the instance detaches anything already attached and rethrows the original exception.

An instance can be attached to only one owner at a time. Calling `Attach(Control)` while `IsAttached` is `true` throws `InvalidOperationException`. `Detach()` is idempotent when the instance is not attached; otherwise it detaches bindings, removes the root from the owner, and clears the current owner.

`Dispose()` detaches the instance once and marks it disposed. Calling `Attach(Control)` after disposal throws `ObjectDisposedException`.

## Constructors
| Name | Description |
| --- | --- |
| `TemplateInstance(UIElement? root, IEnumerable<TemplateBinding>? bindings = null)` | Initializes a template instance with an optional root element and optional template bindings. The binding sequence is copied into the instance. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `Root` | `UIElement?` | Gets the root element created by the template factory, or `null` when the template has no root. |
| `Bindings` | `IReadOnlyList<TemplateBinding>` | Gets the template bindings owned by this instance. |
| `IsAttached` | `bool` | Gets whether the instance currently has an attached owner. |

## Methods
| Name | Return Type | Description |
| --- | --- | --- |
| `Attach(Control templateOwner)` | `void` | Attaches the root and bindings to `templateOwner`. |
| `Detach()` | `void` | Detaches bindings and the root from the current owner, if any. |
| `Dispose()` | `void` | Detaches the instance and prevents future attachment. |

## Exceptions
| Member | Exception | Condition |
| --- | --- | --- |
| `Attach(Control templateOwner)` | `ObjectDisposedException` | The instance has already been disposed. |
| `Attach(Control templateOwner)` | `ArgumentNullException` | `templateOwner` is `null`. |
| `Attach(Control templateOwner)` | `InvalidOperationException` | The instance is already attached to an owner. |

## Applies To
Project: `Cerneala`

UI area: retained controls and classic control templating.

## See Also
- `UI/Controls/TemplateInstance.cs`
- `UI/Controls/ControlTemplate.cs`
- `UI/Controls/ControlTemplate{TControl}.cs`
- `UI/Controls/TemplateContext.cs`
- `UI/Controls/TemplateBinding{T}.cs`
