# HitTestQueue Class

## Definition
Namespace: `Cerneala.UI.Invalidation`

Assembly/Project: `Cerneala`

Source: `UI/Invalidation/HitTestQueue.cs`

Tracks UI elements whose hit-test state must be processed during a retained UI frame.

```csharp
public sealed class HitTestQueue
```

Inheritance:
`object` -> `HitTestQueue`

## Examples

Queue an element for hit-test processing and inspect the current snapshot:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

UIRoot root = new();
UIElement child = new();

root.VisualChildren.Add(child);
root.ProcessFrame();

root.HitTestQueue.Enqueue(child);

IReadOnlyList<UIElement> pendingHitTests = root.HitTestQueue.Snapshot();
if (pendingHitTests.Count > 0)
{
    root.HitTestQueue.Remove(pendingHitTests[0]);
}
```

## Remarks

`HitTestQueue` is owned by a `UIRoot` and is exposed through `UIRoot.HitTestQueue`. `DirtyPropagation` enqueues elements when an invalidation request includes `InvalidationFlags.HitTest`, keeping hit-test work separate from render work.

The queue deduplicates elements by reference. Calling `Enqueue` multiple times with the same `UIElement` keeps only one pending entry.

`Snapshot` defensively removes queued elements that are no longer inside the owning root, then returns the remaining elements in visual tree pre-order. It reuses the root's `ElementQueueOrderIndex`, rebuilt only when `TreeVersion` changes, and sorts only queued entries. Taking a snapshot does not clear valid queued elements; the frame scheduler removes each element before processing it and re-enqueues it if processing throws.

`HasWork` and `Count` read the queue dictionary directly without allocation, pruning, tree traversal, or sorting. Lifecycle detach removes pending work actively; snapshot pruning remains as a defensive fallback.

## Constructors

| Name | Description |
| --- | --- |
| `HitTestQueue(UIRoot root)` | Initializes a queue for the specified root. Throws `ArgumentNullException` when `root` is `null`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Count` | `int` | Gets the number of elements currently tracked by the queue without building a snapshot. |
| `HasWork` | `bool` | Gets whether the queue currently tracks hit-test work without allocation or tree traversal. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Enqueue(UIElement element)` | `void` | Adds an element to the queue if it is not already queued. Throws `ArgumentNullException` when `element` is `null`. |
| `Snapshot()` | `IReadOnlyList<UIElement>` | Removes entries outside the owning root and returns the queued elements in frame processing order. |
| `Remove(UIElement element)` | `void` | Removes the reference-identical entry from the queue when present. |

## Applies to

Cerneala retained UI invalidation pipeline.

## See also

- `Cerneala.UI.Elements.UIRoot`
- `Cerneala.UI.Invalidation.DirtyPropagation`
- `Cerneala.UI.Invalidation.UiFrameScheduler`
- `Cerneala.UI.Invalidation.InvalidationFlags`
