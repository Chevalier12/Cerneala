# ItemContainerGenerator Class

## Definition
Namespace: `Cerneala.UI.Controls.Items`
Assembly/Project: `Cerneala`
Source: `UI/Controls/Items/ItemContainerGenerator.cs`

Creates, prepares, tracks, recycles, and clears item containers for an `ItemsControl`.

```csharp
public sealed class ItemContainerGenerator
```

Inheritance:
`object` -> `ItemContainerGenerator`

## Examples
Realize the current items for an `ItemsControl` and read the metadata stored on the generated containers.

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;

ItemsControl itemsControl = new();
itemsControl.SetItems(new[] { "Ink", "Brush" });

IReadOnlyList<UIElement> containers = itemsControl.ItemContainerGenerator.Realize();

int firstIndex = ItemContainerGenerator.GetItemIndex(containers[0]);
object? firstItem = ItemContainerGenerator.GetItem(containers[0]);
bool firstIsSelected = ItemContainerGenerator.GetIsSelected(containers[0]);
```

## Remarks
`ItemContainerGenerator` is created by `ItemsControl` and uses the owning control to inspect items, choose container types, create containers, prepare containers, and clear containers.

`Realize` computes the requested item range from an optional `RealizationWindow`, clamps it to the current item count, recycles any realized containers outside that range, and returns containers for the requested indexes in order. `GetOrCreate` returns an already realized compatible container when possible. If the existing container is not compatible with the current item and container type, it is recycled before a replacement is obtained.

Recycled containers are stored in `RecyclePool` by concrete container type. If the item is itself the generated `UIElement`, the generator clears and detaches it but does not push it into the recycle pool.

Container metadata is stored separately from the container instance. `GetItemIndex` returns `-1` when no metadata exists, `GetItem` returns `null` when no item is recorded, and `GetIsSelected` returns `false` when no selection flag is recorded.

## Constructors
| Name | Description |
| --- | --- |
| `ItemContainerGenerator(ItemsControl owner)` | Initializes a generator for `owner` and creates an empty `ItemContainerRecyclePool`. Throws `ArgumentNullException` when `owner` is `null`. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `RecyclePool` | `ItemContainerRecyclePool` | Gets the pool used to reuse detached containers by concrete container type. |
| `RealizedContainers` | `IReadOnlyDictionary<int, UIElement>` | Gets the currently realized containers keyed by item index. |

## Methods
| Name | Return type | Description |
| --- | --- | --- |
| `Realize(RealizationWindow? window = null)` | `IReadOnlyList<UIElement>` | Realizes containers for the requested window, or for all items when `window` is `null`, and recycles containers outside that range. |
| `GetOrCreate(int index)` | `UIElement` | Returns the compatible realized container for `index`, reuses a recycled container when available, or asks the owner to create and prepare a new container. Throws `ArgumentOutOfRangeException` when `index` is outside the current item range. |
| `Recycle(int index)` | `void` | Removes the realized container for `index`, detaches it from visual and logical parents, clears owner-specific container state, and stores it in `RecyclePool` when it is reusable. |
| `Clear()` | `void` | Recycles every currently realized container. |
| `GetItemIndex(UIElement container)` | `int` | Returns the item index recorded for `container`, or `-1` when no index is recorded. Throws `ArgumentNullException` when `container` is `null`. |
| `GetItem(UIElement container)` | `object?` | Returns the item recorded for `container`, or `null` when no item is recorded. Throws `ArgumentNullException` when `container` is `null`. |
| `GetIsSelected(UIElement container)` | `bool` | Returns the selection flag recorded for `container`; returns `false` when no flag is recorded. Throws `ArgumentNullException` when `container` is `null`. |
| `SetInfo(UIElement container, int index, object? item, bool isSelected)` | `void` | Records item index, item value, and selection state for `container`. Throws `ArgumentNullException` when `container` is `null`. |
| `ClearInfo(UIElement container)` | `void` | Resets the recorded metadata for `container` to index `-1`, item `null`, and unselected state. Throws `ArgumentNullException` when `container` is `null`. |

## Applies to
`Cerneala.UI.Controls` item controls that use `ItemsControl.ItemContainerGenerator` for realization, recycling, and container metadata.

## See also
- `ItemsControl`
- `ItemContainerRecyclePool`
- `ItemsPresenter`
- `RealizationWindow`
