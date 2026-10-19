# UIElementCollection Class

## Definition
Namespace: `Cerneala.UI.Elements`

Assembly/Project: `Cerneala`

Source: `UI/Elements/UIElementCollection.cs`

Represents a role-specific child collection for a `UIElement`.

```csharp
public sealed class UIElementCollection : IReadOnlyList<UIElement>
```

Implements:
`IReadOnlyList<UIElement>`, `IReadOnlyCollection<UIElement>`, `IEnumerable<UIElement>`, `IEnumerable`

## Examples

```csharp
using Cerneala.UI.Elements;

UIElement parent = new();
UIElement child = new();

parent.VisualChildren.Add(child);
parent.VisualChildren.Move(0, 0);
bool removed = parent.VisualChildren.Remove(child);
```

## Remarks

When the owner or child is attached, `Add`, `Insert`, `Move`, and `Remove` verify the relevant root Relay before changing parents, collection storage, tree versions, motion state, or invalidation queues. Cross-thread tree changes throw `InvalidOperationException`; post the mutation to `UIRoot.Relay` instead.

`UIElementCollection` is used for logical and visual child roles. It validates tree invariants before adding a child: an element cannot be added to itself, ancestors cannot be re-added as children, duplicate children are rejected, and reparenting requires explicit removal from the current parent first.

A `Panel` owner imposes a layout invariant: the panel measures, arranges, and draws only its visual children, so `Add` and `Insert` on a panel's `LogicalChildren` throw `InvalidOperationException` when the child is not already a visual child of that panel. The message names `Panel.Children.Add`, which adds the child to both collections with one call. `VisualChildren` insertion on a panel is unchanged.

Owner-managed collider nodes impose an additional invariant: application code must use the singular `Sprite2D.Collider` property to attach, replace, or clear a live collider. Each owner accepts at most one. Generic logical/visual collection mutation throws `InvalidOperationException` rather than desynchronizing that slot. Internal static tile adapters likewise cannot be detached or reparented through this API.

When the owner is attached to a root, adding a child attaches the child's subtree and increments the root tree version. Removing a child detaches the subtree when it no longer has an attached parent, that is, a parent attached to the root that holds the child in its `LogicalChildren` or `VisualChildren`; a `LogicalParent` that does not hold the child in its `LogicalChildren`, such as the `TabItem` that holds the content of a tab that is not selected, does not keep the child attached. Removing the child also releases lifecycle state through `ElementLifecycle`, removes pending queue work, and increments the root tree version. When a detach callback of the removed subtree throws, the child is still removed and detached, the root tree version is incremented, the owner is invalidated, and `Changed` is raised before the exception propagates from `Remove`.

`Move` changes a child's position without detaching or reattaching it. A real move increments the attached root's tree version, which refreshes cached invalidation-queue order on the next snapshot. In a visual collection it also invalidates the owner and the moved child like an added visual child; in a logical collection it invalidates nothing. Equal indexes are a no-op. `Move` does not raise `Changed` because no child was added or removed.

For visual child mutations, the collection increments layout and render versions and invalidates measure, arrange, render, hit-test, and inherited state. Added visual children that are already rooted are also invalidated for aspect and subtree state.

## Properties

| Name | Description |
| --- | --- |
| `Count` | Gets the number of children in the collection. |
| `this[int index]` | Gets the child at the specified index. |

## Methods

| Name | Description |
| --- | --- |
| `Add(UIElement)` | Adds a child after validating tree ownership and attachment rules. |
| `Move(int oldIndex, int newIndex)` | Moves a child within the collection. Throws `ArgumentOutOfRangeException` when either index is outside the current collection. |
| `Remove(UIElement)` | Removes a child and returns whether it was present. |
| `GetEnumerator()` | Returns an enumerator over the children. |

## Events

| Name | Description |
| --- | --- |
| `Changed` | Raised after a child is added or removed. |

## Explicit Interface Implementations

| Name | Description |
| --- | --- |
| `IEnumerable.GetEnumerator()` | Returns an untyped enumerator over the children. |

## Applies to

Cerneala retained UI element trees.

## See also

- `Cerneala.UI.Elements.UIElement`
- `Cerneala.UI.Elements.ElementLifecycle`
- `Cerneala.UI.Elements.ElementTreeChange`
- `Cerneala.UI.Elements.ElementChildRole`
