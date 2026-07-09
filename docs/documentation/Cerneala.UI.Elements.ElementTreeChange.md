# ElementTreeChange Class

## Definition
Namespace: `Cerneala.UI.Elements`

Assembly/Project: `Cerneala`

Source: `UI/Elements/ElementTreeChange.cs`

Describes a single child relationship change raised by a `UIElementCollection`.

```csharp
public sealed class ElementTreeChange
```

Inheritance:
`Object` -> `ElementTreeChange`

## Examples

```csharp
using Cerneala.UI.Elements;

UIElement parent = new();
UIElement child = new();
ElementTreeChange? latestChange = null;

parent.VisualChildren.Changed += (_, change) => latestChange = change;
parent.VisualChildren.Add(child);

if (latestChange is not null &&
    latestChange.Kind == ElementTreeChangeKind.Added &&
    latestChange.Role == ElementChildRole.Visual)
{
    UIElement changedParent = latestChange.Parent;
    UIElement changedChild = latestChange.Child;
}
```

## Remarks

`ElementTreeChange` is used as the event data for `UIElementCollection.Changed`. The collection creates it after a child is added or removed and records the parent element, the child element, the child role that changed, and whether the change was an add or remove operation.

The constructor rejects `null` values for `parent` and `child` by throwing `ArgumentNullException`. `Role` and `Kind` are stored exactly as supplied.

For visual child changes, `UIElementCollection` also performs its visual invalidation work before raising the `Changed` event.

## Constructors

| Name | Description |
| --- | --- |
| `ElementTreeChange(UIElement, UIElement, ElementChildRole, ElementTreeChangeKind)` | Initializes a change record for a parent, child, child role, and change kind. |

## Properties

| Name | Description |
| --- | --- |
| `Parent` | Gets the element whose child collection changed. |
| `Child` | Gets the child element that was added or removed. |
| `Role` | Gets whether the change affected the logical or visual child relationship. |
| `Kind` | Gets whether the child was added or removed. |

## Applies to

Cerneala retained UI element trees.

## See also

- `Cerneala.UI.Elements.UIElement`
- `Cerneala.UI.Elements.UIElementCollection`
- `Cerneala.UI.Elements.ElementChildRole`
- `Cerneala.UI.Elements.ElementTreeChangeKind`
