# ElementChildRole Enum

## Definition
Namespace: `Cerneala.UI.Elements`

Assembly/Project: `Cerneala`

Source: `UI/Elements/ElementChildRole.cs`

Specifies which of an element's two child trees an operation uses: the logical tree or the visual tree.

```csharp
public enum ElementChildRole
```

## Examples

```csharp
using Cerneala.UI.Elements;

UIElement parent = new();
UIElement visual = new();
UIElement logical = new();
parent.VisualChildren.Add(visual);
parent.LogicalChildren.Add(logical);

Console.WriteLine(ElementTreeWalker.PreOrder(parent).Contains(logical));
Console.WriteLine(ElementTreeWalker.PreOrder(parent, ElementChildRole.Logical).Contains(logical));
Console.WriteLine($"{visual.VisualParent == parent} {logical.LogicalParent == parent}");
// False
// True
// True True
```

`PreOrder` walks the visual tree by default, so it does not find the logical child.

## Remarks

Every `UIElement` has two child collections: `LogicalChildren` (role `Logical`) and `VisualChildren` (role `Visual`). Each role has its own parent property: `LogicalParent` and `VisualParent`.

The two roles behave differently when a child is added or removed:

| Behavior | `Logical` | `Visual` |
| --- | --- | --- |
| The parent is invalidated for measure, arrange, render, hit testing, and inherited properties. | No | Yes |
| A removal can start a presence exit animation, which keeps the child in the tree until the exit ends. | No | Yes |
| `ElementTreeChange.Role` | `Logical` | `Visual` |

`ElementTreeWalker.PreOrder`, `PostOrder`, `Ancestors`, and `Descendants` take a role and default to `Visual`. Measure invalidation and `Subtree` invalidation walk the visual tree.

Some elements restrict their parents per role. For example, adding a `Collider2D` to any `LogicalChildren` or `VisualChildren` collection throws `InvalidOperationException`; a collider becomes the logical child of a `Sprite2D` only through the sprite's `Collider` property.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `Logical` | `0` | The logical tree: `LogicalChildren` and `LogicalParent`. |
| `Visual` | `1` | The visual tree: `VisualChildren` and `VisualParent`. |

## Applies to

`ElementTreeWalker`, `ElementTreeChange.Role`, and `UIElement`.

## See also

- `Cerneala.UI.Elements.ElementTreeWalker`
- `Cerneala.UI.Elements.ElementTreeChange`
- `Cerneala.UI.Elements.UIElementCollection`
