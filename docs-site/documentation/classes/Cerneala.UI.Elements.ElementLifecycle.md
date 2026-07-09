# ElementLifecycle Class

## Definition
Namespace: `Cerneala.UI.Elements`

Assembly/Project: `Cerneala`

Source: `UI/Elements/ElementLifecycle.cs`

Provides helper methods that attach and detach UI element subtrees from a `UIRoot`.

```csharp
public static class ElementLifecycle
```

## Examples

```csharp
using Cerneala.UI.Elements;

ElementLifecycle.AttachSubtree(root, child);
ElementLifecycle.DetachSubtree(root, child);
```

## Remarks

`AttachSubtree` walks the logical subtree first, then the visual subtree, and attaches each element to the supplied root. Elements already attached to that same root are skipped. Attaching an element that already belongs to a different root throws `InvalidOperationException`.

`DetachSubtree` walks the visual subtree and then the logical subtree in post-order. It tracks detached elements by reference so an element reached through both roles is cleaned up once.

Detaching releases the element id, removes resource dependency ownership, detaches the element from the root, and clears aspect processor state for the element.

## Methods

| Name | Description |
| --- | --- |
| `AttachSubtree(UIRoot, UIElement)` | Attaches an element and its logical and visual descendants to a root. |
| `DetachSubtree(UIRoot, UIElement)` | Detaches an element and its visual and logical descendants from a root. |

## Applies to

Cerneala retained UI element tree lifecycle.

## See also

- `Cerneala.UI.Elements.UIRoot`
- `Cerneala.UI.Elements.UIElement`
- `Cerneala.UI.Elements.ElementTreeWalker`
