# ElementQueueOrderIndex Class

## Definition
Namespace: `Cerneala.UI.Invalidation`

Assembly/Project: `Cerneala`

Source: `UI/Invalidation/ElementQueueOrderIndex.cs`

Caches visual-tree preorder ordinals for the invalidation queues owned by one `UIRoot`.

```csharp
internal sealed class ElementQueueOrderIndex
```

Inheritance:
`object` -> `ElementQueueOrderIndex`

## Examples

The type is internal and is shared by the queue engine through `UIRoot.QueueOrderIndex`:

```csharp
ElementQueueOrderIndex index = root.QueueOrderIndex;
index.EnsureCurrent();

if (index.TryGetOrdinal(element, out int ordinal))
{
    // Use ordinal as the element's visual preorder position.
}
```

## Remarks

`EnsureCurrent` compares the cached version with `UIRoot.TreeVersion`. When they differ, it walks `ElementTreeWalker.PreOrder(root, ElementChildRole.Visual)`, builds a new reference-identity dictionary, and replaces the previous index atomically after the traversal completes.

All invalidation queues owned by the same root use this index. Multiple queue snapshots therefore share one tree traversal for a given tree version instead of rebuilding visual order independently.

Adding, removing, or moving attached children increments `TreeVersion`, so the next snapshot rebuilds the index. Repeated snapshots while the tree is unchanged reuse the current dictionary.

The diagnostic counters record completed rebuilds and visited nodes. They are internal implementation diagnostics and do not affect ordering.

## Constructors

| Name | Description |
| --- | --- |
| `ElementQueueOrderIndex(UIRoot root)` | Creates an index for `root`. Throws `ArgumentNullException` when `root` is `null`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `BuildCount` | `int` | Gets the number of completed index rebuilds. |
| `LastVisitedNodeCount` | `int` | Gets the number of elements visited by the latest rebuild. |
| `TotalVisitedNodeCount` | `long` | Gets the cumulative number of elements visited by all rebuilds. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `EnsureCurrent()` | `void` | Rebuilds the preorder index when `UIRoot.TreeVersion` changed; otherwise returns without walking the tree. |
| `TryGetOrdinal(UIElement element, out int ordinal)` | `bool` | Looks up the cached visual preorder ordinal for `element`. Call `EnsureCurrent` before lookup. |

## Applies to

Cerneala retained UI invalidation queue ordering.

## See also

- `Cerneala.UI.Invalidation.ElementWorkQueue<TMetadata>`
- `Cerneala.UI.Elements.UIRoot`
- `Cerneala.UI.Elements.ElementTreeWalker`
