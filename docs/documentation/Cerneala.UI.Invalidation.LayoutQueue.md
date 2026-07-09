# LayoutQueue Class

## Definition
Namespace: `Cerneala.UI.Invalidation`

Assembly/Project: `Cerneala`

Source: `UI/Invalidation/LayoutQueue.cs`

Maintains the measure and arrange invalidation queues for a single `UIRoot`.

```csharp
public sealed class LayoutQueue
```

Inheritance:
`object` -> `LayoutQueue`

## Examples
Queue the root for both layout phases, take deterministic snapshots, then remove the processed entries:

```csharp
using System.Collections.Generic;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

UIRoot root = new();
LayoutQueue queue = root.LayoutQueue;

queue.EnqueueMeasure(root);
queue.EnqueueArrange(root);

IReadOnlyList<UIElement> measureSnapshot = queue.SnapshotMeasure();
foreach (UIElement element in measureSnapshot)
{
    queue.RemoveMeasure(element);
}

IReadOnlyList<UIElement> arrangeSnapshot = queue.SnapshotArrange();
foreach (UIElement element in arrangeSnapshot)
{
    queue.RemoveArrange(element);
}
```

## Remarks
`LayoutQueue` stores measure and arrange work separately. Each phase uses reference identity for de-duplication, so enqueueing the same `UIElement` instance more than once leaves one queued entry for that phase.

Snapshots remove queued elements that no longer belong to the queue's root and return the remaining elements in visual tree pre-order. If two queued elements are not found in that traversal, their relative enqueue order is preserved after the in-tree elements.

`UIRoot` creates its own `LayoutQueue` and exposes it through `UIRoot.LayoutQueue`. The frame scheduler consumes the queue by taking a phase snapshot, removing each processed element, and re-enqueueing the element if phase processing throws.

`HasWork` calls the snapshot methods, so checking it can also prune stale entries that have moved outside the root. `MeasureCount` and `ArrangeCount` return the current backing set counts directly.

## Constructors
| Name | Description |
| --- | --- |
| `LayoutQueue(UIRoot root)` | Creates a queue bound to `root`. Throws `ArgumentNullException` when `root` is `null`. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `MeasureCount` | `int` | Gets the number of elements currently held in the measure queue. |
| `ArrangeCount` | `int` | Gets the number of elements currently held in the arrange queue. |
| `HasWork` | `bool` | Gets whether either phase has snapshot-visible work after stale entries are pruned. |

## Methods
| Name | Return Type | Description |
| --- | --- | --- |
| `EnqueueMeasure(UIElement element)` | `void` | Adds `element` to the measure queue if it is not already queued by reference. Throws `ArgumentNullException` when `element` is `null`. |
| `EnqueueArrange(UIElement element)` | `void` | Adds `element` to the arrange queue if it is not already queued by reference. Throws `ArgumentNullException` when `element` is `null`. |
| `SnapshotMeasure()` | `IReadOnlyList<UIElement>` | Prunes measure entries outside the root and returns a visual-tree-ordered snapshot of measure work. |
| `SnapshotArrange()` | `IReadOnlyList<UIElement>` | Prunes arrange entries outside the root and returns a visual-tree-ordered snapshot of arrange work. |
| `RemoveMeasure(UIElement element)` | `void` | Removes `element` from the measure queue and its insertion-order tracking list when present. |
| `RemoveArrange(UIElement element)` | `void` | Removes `element` from the arrange queue and its insertion-order tracking list when present. |

## Applies To
`Cerneala` retained UI invalidation and frame scheduling.

## See Also
- `Cerneala.UI.Elements.UIRoot`
- `Cerneala.UI.Invalidation.UiFrameScheduler`
- `Cerneala.UI.Invalidation.DirtyPropagation`
