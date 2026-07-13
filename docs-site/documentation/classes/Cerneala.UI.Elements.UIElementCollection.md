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

`UIElementCollection` is used for logical and visual child roles. It validates tree invariants before adding a child: an element cannot be added to itself, ancestors cannot be re-added as children, duplicate children are rejected, and reparenting requires explicit removal from the current parent first.

When the owner is attached to a root, adding a child attaches the child's subtree and increments the root tree version. Removing a child detaches the subtree when it no longer has an attached parent, releases lifecycle state through `ElementLifecycle`, removes pending queue work, and increments the root tree version.

`Move` changes a child's position without detaching or reattaching it. A real move increments the attached root's tree version and invalidates visual mutation state, which refreshes cached invalidation-queue order on the next snapshot. Equal indexes are a no-op. `Move` does not raise `Changed` because no child was added or removed.

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
