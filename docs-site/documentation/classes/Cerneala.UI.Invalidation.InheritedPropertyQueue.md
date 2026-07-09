# InheritedPropertyQueue Class

## Definition
Namespace: `Cerneala.UI.Invalidation`

Assembly/Project: `Cerneala`

Source: `UI/Invalidation/InheritedPropertyQueue.cs`

Stores unique elements that need inherited-property propagation for one `UIRoot`.

```csharp
public sealed class InheritedPropertyQueue
```

Inheritance:
`object` -> `InheritedPropertyQueue`

## Examples

Queue an element for inherited-property processing, inspect the deterministic snapshot, then remove processed work:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

UIRoot root = new();
UIElement child = new();

root.VisualChildren.Add(child);
root.ProcessFrame();

root.InheritedPropertyQueue.Enqueue(child);

foreach (UIElement element in root.InheritedPropertyQueue.Snapshot())
{
    root.InheritedPropertyQueue.Remove(element);
    root.InheritedPropertyPropagator.PropagateFrom(element);
}
```

## Remarks

`InheritedPropertyQueue` is owned by a `UIRoot` and is exposed through `UIRoot.InheritedPropertyQueue`. `DirtyPropagation` enqueues elements when an invalidation request includes `InvalidationFlags.Inherited`, including property invalidations whose source property has `UiPropertyOptions.Inherits`.

The queue deduplicates elements by reference. Calling `Enqueue` repeatedly with the same `UIElement` instance keeps one pending entry while preserving the first enqueue position used as the fallback ordering.

`Snapshot` removes queued elements whose `Root` is no longer the owning root, then returns the remaining elements in visual tree pre-order. Queued elements that still belong to the root but are not found in that traversal are returned after traversed elements and keep their relative enqueue order.

Taking a snapshot does not clear valid work. `UiFrameScheduler` consumes inherited-property work during the `FramePhase.InheritedProperties` phase by taking a snapshot, removing each element before processing it, clearing `InvalidationFlags.Inherited` after successful processing, and re-enqueueing the element if processing throws.

`UiFrameScheduler` runs inherited-property processing before aspect processing and again after aspect processing, so inherited values dirtied by earlier phases can be propagated before layout and rendering continue. `HasWork` calls `Snapshot()`, so checking it can also prune stale entries. `Count` returns the current backing set count without taking a sorted snapshot.

## Constructors

| Name | Description |
| --- | --- |
| `InheritedPropertyQueue(UIRoot root)` | Initializes an inherited-property queue for `root`. Throws `ArgumentNullException` when `root` is `null`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Count` | `int` | Gets the number of unique elements currently tracked by the queue. |
| `HasWork` | `bool` | Gets whether `Snapshot()` contains at least one valid queued element after stale entries are pruned. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Enqueue(UIElement element)` | `void` | Adds `element` to the queue if the same reference is not already queued. Throws `ArgumentNullException` when `element` is `null`. |
| `Snapshot()` | `IReadOnlyList<UIElement>` | Prunes elements outside the owning root and returns queued elements in inherited-property processing order. |
| `Remove(UIElement element)` | `void` | Removes `element` from the queue and from the preserved insertion-order list, when present. |

## Applies to

Cerneala retained UI invalidation, inherited property propagation, and frame scheduling infrastructure.

## See Also

- `Cerneala.UI.Elements.UIRoot`
- `Cerneala.UI.Elements.InheritedPropertyPropagator`
- `Cerneala.UI.Invalidation.DirtyPropagation`
- `Cerneala.UI.Invalidation.UiFrameScheduler`
- `Cerneala.UI.Invalidation.InvalidationFlags`
