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

`Snapshot` defensively removes queued elements whose `Root` is no longer the owning root, then returns the remaining elements in visual tree pre-order. It reuses the root's `ElementQueueOrderIndex`, rebuilt only when `TreeVersion` changes, and sorts only queued entries. Presence-exiting elements outside the current traversal remain after traversed elements in relative enqueue order.

Taking a snapshot does not clear valid work. `UiFrameScheduler` consumes inherited-property work during the `FramePhase.InheritedProperties` phase by taking a snapshot, removing each element before processing it, clearing `InvalidationFlags.Inherited` after successful processing, and re-enqueueing the element if processing throws.

`UiFrameScheduler` runs inherited-property processing before aspect processing and again after aspect processing, so inherited values dirtied by earlier phases can be propagated before layout and rendering continue. `HasWork` and `Count` read the queue dictionary directly without allocating, pruning, walking the tree, or sorting. Lifecycle detach removes pending work actively; snapshot pruning remains as a defensive fallback.

## Constructors

| Name | Description |
| --- | --- |
| `InheritedPropertyQueue(UIRoot root)` | Initializes an inherited-property queue for `root`. Throws `ArgumentNullException` when `root` is `null`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Count` | `int` | Gets the number of unique elements currently tracked by the queue without building a snapshot. |
| `HasWork` | `bool` | Gets whether the queue currently tracks inherited-property work without allocation or tree traversal. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Enqueue(UIElement element)` | `void` | Adds `element` to the queue if the same reference is not already queued. Throws `ArgumentNullException` when `element` is `null`. |
| `Snapshot()` | `IReadOnlyList<UIElement>` | Prunes elements outside the owning root and returns queued elements in inherited-property processing order. |
| `Remove(UIElement element)` | `void` | Removes the reference-identical entry from the queue when present. |

## Applies to

Cerneala retained UI invalidation, inherited property propagation, and frame scheduling infrastructure.

## See Also

- `Cerneala.UI.Elements.UIRoot`
- `Cerneala.UI.Elements.InheritedPropertyPropagator`
- `Cerneala.UI.Invalidation.DirtyPropagation`
- `Cerneala.UI.Invalidation.UiFrameScheduler`
- `Cerneala.UI.Invalidation.InvalidationFlags`
