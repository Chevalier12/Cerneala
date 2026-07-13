# RenderQueue Class

## Definition
Namespace: `Cerneala.UI.Invalidation`  
Assembly/Project: `Cerneala`  
Source: `UI/Invalidation/RenderQueue.cs`

Stores unique elements that need render-cache processing for one `UIRoot`.

```csharp
public sealed class RenderQueue
```

Inheritance:  
`object` -> `RenderQueue`

## Examples

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

UIRoot root = new(viewportWidth: 800, viewportHeight: 600);
RenderQueue queue = root.RenderQueue;

queue.Enqueue(root);

foreach (UIElement element in queue.Snapshot())
{
    queue.Remove(element);
    // Process render-cache work for element.
}
```

## Remarks

`RenderQueue` is the render invalidation work queue owned by `UIRoot.RenderQueue`.
`DirtyPropagation` enqueues elements when an invalidation includes `InvalidationFlags.Render`, and `UiFrameScheduler` consumes the queue during the `FramePhase.RenderCache` phase.

Each element is stored once by reference. Calling `Enqueue` repeatedly with the same `UIElement` reference does not create duplicate work.

`Snapshot` defensively removes queued elements whose `Root` is no longer the queue root, then returns the remaining elements in visual-tree preorder. It reuses the root's `ElementQueueOrderIndex`, rebuilt only when `TreeVersion` changes, and sorts only queued entries. Presence-exiting elements that still belong to the root but have left `VisualChildren` remain after traversed elements in relative enqueue order so exit rendering can finish.

`HasWork` and `Count` read the queue dictionary directly without allocating, pruning, walking the tree, or sorting. Lifecycle detach removes pending work actively; snapshot pruning remains as a defensive fallback.

During frame processing, `UiFrameScheduler` removes each snapshotted element before invoking render-cache processors. If processing throws, the scheduler re-enqueues that element.

## Constructors

| Name | Description |
| --- | --- |
| `RenderQueue(UIRoot root)` | Creates a render queue for `root`. Throws `ArgumentNullException` when `root` is `null`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Count` | `int` | Gets the number of unique elements currently tracked by the queue. |
| `HasWork` | `bool` | Gets whether the queue currently tracks render work without allocation or tree traversal. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Enqueue(UIElement element)` | `void` | Adds `element` to the queue if the same reference is not already queued. Throws `ArgumentNullException` when `element` is `null`. |
| `Remove(UIElement element)` | `void` | Removes `element` from the queue when present. |
| `Snapshot()` | `IReadOnlyList<UIElement>` | Prunes elements outside the queue root and returns queued elements in render processing order. |

## Applies to

Cerneala UI invalidation and retained rendering infrastructure.

## See Also

- `Cerneala.UI.Elements.UIRoot`
- `Cerneala.UI.Invalidation.DirtyPropagation`
- `Cerneala.UI.Invalidation.UiFrameScheduler`
