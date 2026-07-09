# TemplateChildOwner Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/TemplateInstance.cs`

Provides internal helper methods that attach and detach a template root as both a logical and visual child of a control.

```csharp
internal static class TemplateChildOwner
```

## Examples

```csharp
// Used by TemplateInstance and ComponentTemplateInstance when a template root exists.
TemplateChildOwner.Attach(templateOwner, root);
TemplateChildOwner.Detach(templateOwner, root);
```

## Remarks

`TemplateChildOwner` is shared by regular control templates and component templates. `Attach` first validates that the owner can own the supplied child, then adds the child to `LogicalChildren` and `VisualChildren`.

If adding the visual child fails, `Attach` removes the logical child before rethrowing, keeping the control tree from retaining a partially attached template child.

`Detach` removes the child from `VisualChildren` first and then from `LogicalChildren`. The type is internal because it is an implementation detail of template instance lifetime management.

## Methods

| Name | Description |
| --- | --- |
| `Attach(Control, UIElement)` | Validates ownership and attaches the template child to the owner's logical and visual child collections. |
| `Detach(Control, UIElement)` | Removes the template child from the owner's visual and logical child collections. |

## Applies to

Cerneala retained UI template infrastructure.

## See also

- `Cerneala.UI.Controls.TemplateInstance`
- `Cerneala.UI.Controls.Templates.ComponentTemplateInstance`
- `Cerneala.UI.Controls.ContentControl`
