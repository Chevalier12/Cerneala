# ElementWorkQueue<TMetadata> Class

## Definition
Namespace: `Cerneala.UI.Invalidation`

Assembly/Project: `Cerneala`

Source: `UI/Invalidation/ElementWorkQueue.cs`

Stores unique queued elements, optional per-element metadata, and stable snapshot ordering for one `UIRoot`.

```csharp
internal sealed class ElementWorkQueue<TMetadata>
```

Inheritance:
`object` -> `ElementWorkQueue<TMetadata>`

## Examples

The type is internal. Queue wrappers use `ElementQueueUnit` when no metadata is required:

```csharp
ElementWorkQueue<ElementQueueUnit> queue = new(root);

queue.Enqueue(element, ElementQueueUnit.Value);

foreach (UIElement pending in queue.Snapshot())
{
    queue.Remove(pending);
}
```

A queue can supply a merge function when duplicate enqueue operations must promote metadata:

```csharp
ElementWorkQueue<LayoutQueueEntryKind> queue =
    new(root, static (current, incoming) => incoming > current ? incoming : current);
```

## Remarks

Entries are keyed by `UIElement` reference identity. `Count`, `HasWork`, `Contains`, `Enqueue`, and `Remove` use the backing dictionary directly and do not walk or sort the visual tree. Enqueueing an existing element keeps its original sequence and merges its metadata with the configured strategy. Without a merge function, incoming metadata replaces the current value.

`Snapshot` first refreshes the root's shared `ElementQueueOrderIndex` when `UIRoot.TreeVersion` changed. It then sorts only the queued entries by cached visual preorder ordinal and uses enqueue sequence as the stable secondary key. Passing `reverse: true` reverses the resulting processing order.

Elements no longer attached to the owning root are removed defensively while a snapshot is built. Normal lifecycle detach also removes pending work from every root queue. A presence-exiting element that still belongs to the root but has left `VisualChildren` remains snapshot-visible after in-tree elements so its exit rendering can finish.

Taking a snapshot does not remove valid entries. The returned array is stable: later enqueue or remove operations affect subsequent snapshots, not a snapshot already returned.

## Constructors

| Name | Description |
| --- | --- |
| `ElementWorkQueue(UIRoot root, Func<TMetadata, TMetadata, TMetadata>? mergeMetadata = null)` | Creates a queue for `root` and optionally configures duplicate-entry metadata merging. Throws `ArgumentNullException` when `root` is `null`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Count` | `int` | Gets the number of currently tracked entries in constant time. |
| `HasWork` | `bool` | Gets whether at least one entry is currently tracked, without allocating or building a snapshot. |
| `LastSnapshotSortCount` | `int` | Gets the number of valid entries sorted by the latest snapshot operation. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Contains(UIElement element)` | `bool` | Returns whether the element reference is currently queued. A `null` value returns `false`. |
| `Enqueue(UIElement element, TMetadata metadata)` | `void` | Adds a new entry or merges metadata into an existing reference-identical entry. Throws `ArgumentNullException` when `element` is `null`. |
| `GetMetadataOrDefault(UIElement element, TMetadata fallback)` | `TMetadata` | Returns queued metadata, or `fallback` when the element is `null` or absent. |
| `Remove(UIElement element)` | `bool` | Removes the reference-identical entry and returns whether it was present. A `null` value returns `false`. |
| `Snapshot(bool reverse = false)` | `IReadOnlyList<UIElement>` | Prunes stale entries and returns a stable visual-order snapshot, optionally reversed. |

## Applies to

Cerneala retained UI layout, inherited property, command-state, aspect, render, and hit-test queues.

## See also

- `Cerneala.UI.Invalidation.ElementQueueOrderIndex`
- `Cerneala.UI.Invalidation.LayoutQueue`
- `Cerneala.UI.Invalidation.RenderQueue`
- `Cerneala.UI.Elements.UIRoot`
