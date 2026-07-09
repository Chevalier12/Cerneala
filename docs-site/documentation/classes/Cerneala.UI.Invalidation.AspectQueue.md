# AspectQueue Class

## Definition
Namespace: `Cerneala.UI.Invalidation`

Assembly/Project: `Cerneala`

Source: `UI/Invalidation/AspectQueue.cs`

Stores unique elements that need aspect processing for one `UIRoot`.

```csharp
public sealed class AspectQueue
```

Inheritance:
`object` -> `AspectQueue`

## Examples

Queue an element for aspect processing, inspect the frame-order snapshot, then remove processed work:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

UIRoot root = new();
UIElement child = new();

root.VisualChildren.Add(child);
root.ProcessFrame();

root.AspectQueue.Enqueue(child);

foreach (UIElement element in root.AspectQueue.Snapshot())
{
    root.AspectQueue.Remove(element);
    // Process aspect work for element.
}
```

## Remarks

`AspectQueue` is owned by a `UIRoot` and is exposed through `UIRoot.AspectQueue`. `DirtyPropagation` enqueues elements when an invalidation request includes `InvalidationFlags.Aspect`, including property invalidations whose metadata affects aspects and root-level theme or aspect-registry changes.

The queue deduplicates elements by reference. Calling `Enqueue` repeatedly with the same `UIElement` instance keeps one pending entry while preserving the first enqueue position used for ordering fallback.

`Snapshot` removes queued elements whose `Root` is no longer the owning root, then returns the remaining elements in visual tree pre-order. Queued elements that still belong to the root but are not found in that traversal are returned after traversed elements and keep their relative enqueue order.

Taking a snapshot does not clear valid work. `UiFrameScheduler` consumes aspect work during the `FramePhase.Aspect` phase by taking a snapshot, removing each element before processing it, clearing the `InvalidationFlags.Aspect` dirty flag after successful processing, and re-enqueueing the element if processing throws.

`HasWork` calls `Snapshot()`, so checking it can also prune stale entries. `Count` returns the current backing set count without taking a sorted snapshot.

`ItemsPresenter` can process inherited and aspect work for realized item subtrees during measure and remove those elements from the root aspect queue after processing them.

## Constructors

| Name | Description |
| --- | --- |
| `AspectQueue(UIRoot root)` | Initializes an aspect queue for `root`. Throws `ArgumentNullException` when `root` is `null`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Count` | `int` | Gets the number of unique elements currently tracked by the queue. |
| `HasWork` | `bool` | Gets whether `Snapshot()` contains at least one valid queued element after stale entries are pruned. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Enqueue(UIElement element)` | `void` | Adds `element` to the queue if the same reference is not already queued. Throws `ArgumentNullException` when `element` is `null`. |
| `Snapshot()` | `IReadOnlyList<UIElement>` | Prunes elements outside the owning root and returns queued elements in aspect processing order. |
| `Remove(UIElement element)` | `void` | Removes `element` from the queue and from the preserved insertion-order list, when present. |

## Applies to

Cerneala retained UI invalidation, aspect resolution, and frame scheduling infrastructure.

## See Also

- `Cerneala.UI.Elements.UIRoot`
- `Cerneala.UI.Invalidation.DirtyPropagation`
- `Cerneala.UI.Invalidation.UiFrameScheduler`
- `Cerneala.UI.Invalidation.InvalidationFlags`
- `Cerneala.UI.Aspect.AspectProcessor`
