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

`AttachSubtree` and `DetachSubtree` verify the supplied root's Relay before traversing or changing lifecycle state. Calls from another thread throw `InvalidOperationException` without partially attaching or detaching the subtree.

`AttachSubtree` validates the complete reachable subtree before attaching any element. At every node it follows logical children before visual children, including paths that alternate between those roles. It attaches parents before their descendants and visits each element once by reference. Elements already attached to that same root keep their attachment; their descendants are still traversed. An element belonging to a different root throws `InvalidOperationException` during validation, before partial attachment.

`DetachSubtree` follows visual children before logical children at every node, in post-order. Mixed-role paths are included, and an element reached through both roles is cleaned up once. For example, a visual `RenderSurface2D` child of a panel still owns the lifecycle of its logical scene descendants; constructing the scene before panel attachment does not exclude those descendants.

Detaching releases the element id, removes resource dependency ownership, detaches the element from the root, removes pending measure, arrange, inherited-property, command-state, aspect, render, and hit-test work, and clears aspect processor state for the element. Pending work is removed after detach callbacks complete, so work enqueued by a callback for the departing element is removed as well.

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
