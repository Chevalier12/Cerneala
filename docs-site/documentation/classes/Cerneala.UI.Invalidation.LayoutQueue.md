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
`LayoutQueue` stores measure and arrange work in separate `ElementWorkQueue<LayoutQueueEntryKind>` instances. Each phase uses reference identity for de-duplication, so enqueueing the same `UIElement` instance more than once leaves one queued entry for that phase. Duplicate internal requests promote metadata with `Direct` above `Required` above `Propagated`; lower-priority requests cannot demote existing work.

Snapshots defensively remove queued elements that no longer belong to the queue's root and return the remaining elements in visual tree pre-order. They reuse the root's `ElementQueueOrderIndex`, rebuilt only when `TreeVersion` changes, and sort only queued entries. The internal incremental-measure snapshot reverses that order for bottom-up processing.

`UIRoot` creates its own `LayoutQueue` and exposes it through `UIRoot.LayoutQueue`. The frame scheduler consumes the queue by taking a phase snapshot, removing each processed element, and re-enqueueing the element if phase processing throws.

`HasWork`, `MeasureCount`, and `ArrangeCount` read their queue dictionaries directly without allocating, pruning, walking the tree, or sorting. Lifecycle detach removes pending measure and arrange work actively; snapshot pruning remains as a defensive fallback.

## Constructors
| Name | Description |
| --- | --- |
| `LayoutQueue(UIRoot root)` | Creates a queue bound to `root`. Throws `ArgumentNullException` when `root` is `null`. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `MeasureCount` | `int` | Gets the number of elements currently held in the measure queue. |
| `ArrangeCount` | `int` | Gets the number of elements currently held in the arrange queue. |
| `HasWork` | `bool` | Gets whether either phase currently tracks work without allocation or tree traversal. |

## Methods
| Name | Return Type | Description |
| --- | --- | --- |
| `EnqueueMeasure(UIElement element)` | `void` | Adds `element` to the measure queue if it is not already queued by reference. Throws `ArgumentNullException` when `element` is `null`. |
| `EnqueueArrange(UIElement element)` | `void` | Adds `element` to the arrange queue if it is not already queued by reference. Throws `ArgumentNullException` when `element` is `null`. |
| `SnapshotMeasure()` | `IReadOnlyList<UIElement>` | Prunes measure entries outside the root and returns a visual-tree-ordered snapshot of measure work. |
| `SnapshotArrange()` | `IReadOnlyList<UIElement>` | Prunes arrange entries outside the root and returns a visual-tree-ordered snapshot of arrange work. |
| `RemoveMeasure(UIElement element)` | `void` | Removes the reference-identical entry from the measure queue when present. |
| `RemoveArrange(UIElement element)` | `void` | Removes the reference-identical entry from the arrange queue when present. |

## Applies To
`Cerneala` retained UI invalidation and frame scheduling.

## See Also
- `Cerneala.UI.Elements.UIRoot`
- `Cerneala.UI.Invalidation.UiFrameScheduler`
- `Cerneala.UI.Invalidation.DirtyPropagation`
