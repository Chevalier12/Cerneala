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

Detaching removes resource dependency ownership, detaches the element from the root, removes pending measure, arrange, inherited-property, command-state, aspect, render, and hit-test work, and clears aspect processor state for the element. Pending work is removed after detach callbacks complete, so work enqueued by a callback for the departing element is removed as well.

A detach callback that throws, such as an `Unloaded` handler or an `OnDetached` override, does not stop the traversal. That element still has its root, bindings, and pending work cleared, the remaining elements of the subtree are detached as usual, and then the first exception propagates to the caller. For example, when two children of a removed panel throw from `OnDetached`, the panel and both children end with `Root` equal to `null`, and the first child's exception is thrown. A throwing callback does not skip the same element's later callbacks either: the element's aspect behavior is disposed, `Unloaded` is raised, Motion layout and presence tracking end, every lifecycle behavior detaches (for example the `Timbre` scope), and `OnDetached` runs, each one even when an earlier one threw. For example, when an open `Overlay` has an `Unloaded` handler that throws, removing its parent still runs `Overlay.OnDetached`, so the overlay ends closed with its projected content withdrawn, and the caller of the removal receives the handler's exception.

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
