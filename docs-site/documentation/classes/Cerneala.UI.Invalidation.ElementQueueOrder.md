# ElementQueueOrder Class

## Definition
Namespace: `Cerneala.UI.Invalidation`

Assembly/Project: `Cerneala`

Source: `UI/Invalidation/ElementQueueOrder.cs`

Provides shared ordering and root-filtering helpers for UI element invalidation queues.

```csharp
internal static class ElementQueueOrder
```

Inheritance:
`object` -> `ElementQueueOrder`

## Examples

The following example shows the pattern used by invalidation queues before returning a snapshot. This helper is internal, so the example applies to code inside the `Cerneala` assembly.

```csharp
using System.Collections.Generic;
using System.Linq;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

UIRoot root = new();
UIElement child = new();
UIElement detached = new();

root.VisualChildren.Add(child);

HashSet<UIElement> elements = new(ReferenceEqualityComparer.Instance)
{
    detached,
    child,
};

List<UIElement> order =
[
    detached,
    child,
];

ElementQueueOrder.RemoveElementsOutsideRoot(root, elements, order);
IReadOnlyList<UIElement> snapshot = ElementQueueOrder.Sort(root, order.Where(elements.Contains));
```

After `RemoveElementsOutsideRoot` runs, elements whose `Root` is not the supplied `root` are removed from both queue collections. `Sort` then returns the remaining elements in visual-tree preorder.

## Remarks

`ElementQueueOrder` centralizes two behaviors shared by retained UI invalidation queues:

- removing queued elements that no longer belong to the queue's `UIRoot`;
- sorting queued elements by visual-tree preorder before a snapshot is processed.

`Sort` builds a preorder index by walking `ElementTreeWalker.PreOrder(root, ElementChildRole.Visual)`. Elements that are not found in that traversal sort after elements that are found. When two input elements have the same effective tree sort position, their original input order is preserved by using the captured enumerable index as a secondary key.

`RemoveElementsOutsideRoot` mutates both collections passed to it. It first removes stale elements from the ordered list, removing the same elements from the set while doing so, and then performs a second `RemoveWhere` pass over the set to catch stale entries that may not appear in the list.

The type is `internal`; it is an implementation detail for queue types such as `AspectQueue`, `CommandStateQueue`, `InheritedPropertyQueue`, `LayoutQueue`, `RenderQueue`, and `HitTestQueue`.

## Methods

| Name | Description |
| --- | --- |
| `RemoveElementsOutsideRoot(UIElement root, HashSet<UIElement> elements, List<UIElement> order)` | Removes queued elements whose `Root` is not the supplied root from the set and ordered list. |
| `Sort(UIElement root, IEnumerable<UIElement> elements)` | Returns the supplied elements sorted by visual-tree preorder under `root`, preserving input order for elements with the same fallback position. |

## RemoveElementsOutsideRoot Method

```csharp
public static void RemoveElementsOutsideRoot(
    UIElement root,
    HashSet<UIElement> elements,
    List<UIElement> order)
```

### Parameters

| Name | Type | Description |
| --- | --- | --- |
| `root` | `UIElement` | The root that queued elements must still belong to. In current queue usage this is a `UIRoot`. |
| `elements` | `HashSet<UIElement>` | The reference-identity set of queued elements to prune. |
| `order` | `List<UIElement>` | The enqueue-order list to prune in sync with `elements`. |

### Remarks

An element is kept only when `ReferenceEquals(element.Root, root)` is `true`. Detached elements and elements attached to another root are removed.

This method mutates `elements` and `order`; it does not return a new collection.

## Sort Method

```csharp
public static IReadOnlyList<UIElement> Sort(
    UIElement root,
    IEnumerable<UIElement> elements)
```

### Parameters

| Name | Type | Description |
| --- | --- | --- |
| `root` | `UIElement` | The root used to build the visual preorder index. |
| `elements` | `IEnumerable<UIElement>` | The elements to order. |

### Returns

`IReadOnlyList<UIElement>`

An array-backed read-only list containing the input elements sorted by visual preorder.

### Exceptions

| Exception | Condition |
| --- | --- |
| `ArgumentNullException` | `root` is `null`; the exception is thrown by `ElementTreeWalker.PreOrder`. |
| `ArgumentNullException` | `elements` is `null`; the exception is thrown by LINQ when the input sequence is projected. |

### Remarks

The primary sort key is the element's index in `ElementTreeWalker.PreOrder(root, ElementChildRole.Visual)`. Elements not present in that traversal use `int.MaxValue` as their primary key, so they appear after known elements. The secondary sort key is the input enumeration index, which keeps the ordering stable for fallback entries.

## Applies to

Cerneala retained UI invalidation queues.

## See also

- `Cerneala.UI.Elements.ElementTreeWalker`
- `Cerneala.UI.Elements.UIElement`
- `Cerneala.UI.Invalidation.AspectQueue`
- `Cerneala.UI.Invalidation.LayoutQueue`
