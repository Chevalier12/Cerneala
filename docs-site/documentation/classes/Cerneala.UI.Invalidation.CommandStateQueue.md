# CommandStateQueue Class

## Definition
Namespace: `Cerneala.UI.Invalidation`

Assembly/Project: `Cerneala`

Source: `UI/Invalidation/CommandStateQueue.cs`

Maintains the command-state refresh queue for a single `UIRoot`.

```csharp
public sealed class CommandStateQueue
```

Inheritance:
`object` -> `CommandStateQueue`

## Examples
Queue a command-state source and process the returned snapshot:

```csharp
using System.Collections.Generic;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

UIRoot root = new();
ButtonBase button = new();

root.VisualChildren.Add(button);
button.QueueCommandStateRefresh();

IReadOnlyList<UIElement> snapshot = root.CommandStateQueue.Snapshot();
foreach (UIElement element in snapshot)
{
    root.CommandStateQueue.Remove(element);
}
```

## Remarks
`CommandStateQueue` stores elements that need their command-enabled state refreshed. `UIRoot` creates one queue and exposes it through `UIRoot.CommandStateQueue`; `UIElement.QueueCommandStateRefresh` enqueues command-state sources on that root.

The queue de-duplicates by `UIElement` reference, so enqueueing the same element more than once keeps a single queued entry. `Snapshot` defensively prunes elements that no longer belong to the queue's root and returns the remaining elements in visual tree pre-order. It reuses the root's `ElementQueueOrderIndex`, which is rebuilt only when `TreeVersion` changes, and sorts only queued entries. Presence-exiting elements outside the current traversal keep their relative enqueue order after in-tree elements.

`UiFrameScheduler` consumes this queue during the command-state phase. It snapshots the queue, removes each element before invoking the phase processor, and re-enqueues the element if command-state processing throws. Command-state processing itself does not clear `DirtyState` flags; the scheduler records the phase with `InvalidationFlags.None`.

`HasWork` and `Count` read the queue dictionary directly without allocating, pruning, walking the tree, or sorting. Lifecycle detach removes pending work actively; snapshot pruning remains as a defensive fallback.

## Constructors
| Name | Description |
| --- | --- |
| `CommandStateQueue(UIRoot root)` | Creates a command-state queue bound to `root`. Throws `ArgumentNullException` when `root` is `null`. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `Count` | `int` | Gets the number of elements currently held in the queue without building a snapshot. |
| `HasWork` | `bool` | Gets whether the queue currently tracks command-state work without allocation or tree traversal. |

## Methods
| Name | Return Type | Description |
| --- | --- | --- |
| `Enqueue(UIElement element)` | `void` | Adds `element` to the queue if it is not already queued by reference. Throws `ArgumentNullException` when `element` is `null`. |
| `Snapshot()` | `IReadOnlyList<UIElement>` | Prunes entries outside the root and returns a visual-tree-ordered snapshot of command-state work. |
| `Remove(UIElement element)` | `void` | Removes the reference-identical entry from the queue when present. |

## Applies To
`Cerneala` retained UI command-state invalidation and frame scheduling.

## See Also
- `Cerneala.UI.Elements.UIRoot`
- `Cerneala.UI.Elements.UIElement`
- `Cerneala.UI.Input.ICommandStateSource`
- `Cerneala.UI.Invalidation.UiFrameScheduler`
