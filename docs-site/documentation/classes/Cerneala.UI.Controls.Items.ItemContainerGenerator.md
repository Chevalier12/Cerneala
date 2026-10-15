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
Create a generator for an `ItemsControl`, realize its items, and read the metadata stored on the generated containers.

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Items;
using Cerneala.UI.Elements;

ItemsControl itemsControl = new();
itemsControl.SetItems(new[] { "Ink", "Brush" });

ItemContainerGenerator generator = new(itemsControl);
IReadOnlyList<UIElement> containers = generator.Realize();

int firstIndex = ItemContainerGenerator.GetItemIndex(containers[0]);    // 0
object? firstItem = ItemContainerGenerator.GetItem(containers[0]);      // "Ink"
bool firstIsSelected = ItemContainerGenerator.GetIsSelected(containers[0]); // false
```

## Remarks
Every `ItemsControl` creates its own `ItemContainerGenerator` in its constructor and uses it to realize, prepare, recycle, and clear its containers. That instance is not exposed as public API. Code outside the framework can create another generator with the public constructor; the new generator keeps its own `RealizedContainers` and `RecyclePool`, separate from the control's own generator. The generator uses the owning control to inspect items, choose container types, create containers, prepare containers, and clear containers.

`GetItemIndex`, `GetItem`, `GetIsSelected`, `SetInfo`, and `ClearInfo` are static. They read and write metadata attached to a container, so they work for containers produced by any generator, including the control's own one.

`Realize` computes the requested item range from an optional `RealizationWindow`, clamps it to the owner's current view item count, recycles any realized containers outside that range, and returns containers for the requested indexes in order. `GetOrCreate` returns an already realized compatible container when possible, and prepares it again when its recorded item or index differs from the item and index requested, or when its recorded selection flag differs from the owner's `IsItemSelected(index)`. If the existing container is not compatible with the current item and container type, it is recycled before a replacement is obtained. A container whose recorded item is the container itself is compatible only with that item, so it is never prepared for another item, even when the container type matches. A container that shows another item is not compatible with a `UIElement` item whose type is the type that `GetContainerTypeForItem` returns or derives from it, because `CreateItemContainer` can return that item as its own container; the container is recycled and the item is realized as described below. A container that already shows that item is kept.

The control's own generator follows item changes whose position is known, before the control's selection follows them. When items are removed, the containers of the removed items are recycled. When items are inserted, removed, or moved, every other realized container moves to the current index of its item, so `RealizedContainers` stays keyed by the current item index. A container that moved keeps its recorded index until the next `Realize` or `GetOrCreate` prepares it for the new index. A `Selector` item click does not read this recorded index: it selects the index at which the container is realized now, so a click in this window selects the item the clicked container shows. When an item is replaced, its container keeps the index and is prepared for the new item, or recycled if it is not compatible, by the next realization. Until then the container still shows the replaced item, not the item at its index, so a `Selector` item click on it selects nothing, and a selection change does not prepare it. The next realization prepares it with the current selection even when the original item was put back at that index in the meantime. For example, a `ListBox` holds the items `a` (a `ListBoxItem`) and `"str"`, `a` is realized, and `SelectedIndex` is `0`. After `listBox.Items[0] = "x"` the selection is cleared, and after `listBox.Items[0] = a` and the next realization, `SelectedIndex` is `-1`, `a.IsSelected` is `false`, and `GetIsSelected(a)` returns `false`. A replaced item that was its own container is not compatible with the new item, so the next realization recycles it. The exception is a container that wraps a replaced `UIElement` item, such as the `ListBoxItem` generated for a `UIElement` that is not a `ListBoxItem`: that container is recycled when the replacement is reported, so the element is released at once and can be wrapped by the container at its new index. A reset, or a change whose position is unknown, recycles every realized container. A generator created with the public constructor is not notified of item changes.

For example, a `ListBox` holds three `UIElement` items `e0`, `e1`, and `e2`, each wrapped in a `ListBoxItem`. After `listBox.Items.RemoveAt(0)`, the container that showed `e0` is recycled and its `Content` is `null`. `RealizedContainers[0]` is the container that shows `e1`, and `RealizedContainers[1]` is the container that shows `e2`. A `TabItem` item is its own container, so removing it detaches it and clears its item state, but its `Content` is kept.

For example, a `ListBox` holds the `UIElement` items `e0` and `e1`, each wrapped in a `ListBoxItem`, and the two items are swapped with `listBox.Items[0] = e1` and then `listBox.Items[1] = e0`. Each assignment recycles the `ListBoxItem` that showed the replaced element, so its `Content` becomes `null`. After the next layout, `RealizedContainers[0]` shows `e1` and `RealizedContainers[1]` shows `e0`, and each element is the `Content` of only one container. Swapping the items of an `ObservableCollection<UIElement>` used as `ItemsSource` works the same way.

A container that is its own item, such as a `ListBoxItem` placed directly in a `ListBox`, belongs to that item rather than to an index. If such a realized container is still keyed by an index that no longer holds its item, `Realize` moves it to the current index of its item before it recycles anything, and then prepares it for that index; the container is not cleared, detached, or recycled. This happens in a generator that is not notified of item changes. For example, `ItemContainerGenerator generator = new(listBox)` realizes the `ListBoxItem`s `a`, `b`, and `c` of `listBox`. After `listBox.Items.RemoveAt(0)`, the next `generator.Realize()` recycles only `a`; `b` is realized at index `0` with `ItemIndex` `0` and `Item` `b`, and `c` at index `1`. The control's own generator already moved `b` and `c` to the keys `0` and `1` when the removal was reported, so its next `Realize` only prepares them for their new indexes.

Such a container never serves a data item that takes its old index. For example, a `ListBox` holds the items `a` (a `ListBoxItem` with `Content` `"A"`), `"str"`, and `c` (a `ListBoxItem`), and all three are realized. After `listBox.Items[0] = "new"`, the next realization recycles `a` and realizes a generated `ListBoxItem` that shows `"new"` at index `0`. `a` keeps `Content` `"A"`, has no visual parent, has `ItemIndex` `-1`, and is not in `RecyclePool`. The same happens in a generator created with the public constructor after `listBox.Items.RemoveAt(0)`: `a` is still keyed by index `0`, and the next `Realize` recycles it and shows `"str"` in a generated `ListBoxItem`.

Recycled containers are stored in `RecyclePool` by concrete container type. If the item is itself the generated `UIElement`, the generator clears and detaches it but does not push it into the recycle pool. An item that its owner realizes as itself, such as a `ListBoxItem` item in a `ListBox` or a `UIElement` item in an `ItemsControl` without an `ItemTemplate`, never takes a recycled container from the pool. For example, a `ListBox` holds the items `"str"` and `a` (a `ListBoxItem` with `Content` `"A"`). `Realize(new RealizationWindow(0, 1))` realizes a generated `ListBoxItem` that shows `"str"`, and `Realize(new RealizationWindow(1, 2))` then returns `a` itself; `a` keeps `Content` `"A"`, and the generated `ListBoxItem` stays in `RecyclePool` with `Content` `null`. An `ItemsControl` with an `ItemTemplate` wraps every item in a `ContentPresenter`, a `ContentPresenter` item too, so that wrapper is an ordinary container: each later `Realize` returns the same wrapper, and `RecyclePool` stays empty. An item can be its own container only when it is a `UIElement` whose type is the type that `GetContainerTypeForItem` returns or derives from it; `CreateItemContainer` then decides whether it is, by returning the item itself. For example, an `ItemsControl` subclass whose `GetContainerTypeForItem` returns `typeof(ContentPresenter)` and whose `CreateItemContainer` returns `new ContentPresenter()` holds one item, a `ListBoxItem`: a `ListBoxItem` is not a `ContentPresenter`, so the first `Realize` wraps it in a `ContentPresenter`, each later `Realize` returns that same `ContentPresenter`, and `RecyclePool` stays empty. When the item is of that type, the generator asks `CreateItemContainer` before the pool, and uses the item itself when `CreateItemContainer` returns it. For example, a `ListBox` subclass whose `GetContainerTypeForItem` returns `typeof(ListBoxItem)` for every item, and which keeps the `CreateItemContainer` of `ListBox`, holds the items `"str"` and `a` (an instance of a class derived from `ListBoxItem`, with `Content` `"A"`). `Realize(new RealizationWindow(0, 1))` realizes a generated `ListBoxItem` that shows `"str"`, and each later `Realize(new RealizationWindow(1, 2))` returns `a` itself with `ItemIndex` `1`; `a` keeps `Content` `"A"`, and the generated `ListBoxItem` stays in `RecyclePool` with `Content` `null`. If the subclass still returns a new container instead of the item, the generator takes a recycled container of that type when `RecyclePool` has one, so `RecyclePool` does not grow on each `Realize`. This applies to a subclass with an `ItemTemplate` too. For example, an `ItemsControl` subclass whose `GetContainerTypeForItem` returns `typeof(ListBoxItem)` and whose `CreateItemContainer` returns the item when it is a `ListBoxItem` and `new ListBoxItem()` otherwise has an `ItemTemplate` and holds the items `"str"` and `a` (a `ListBoxItem` with `Content` `"A"`). `Realize(new RealizationWindow(0, 1))` realizes a generated `ListBoxItem` that shows `"str"`, and each later `Realize(new RealizationWindow(1, 2))` returns `a` itself; `a` keeps `Content` `"A"`, and the generated `ListBoxItem` stays in `RecyclePool` with `Content` `null`. If the same control instead holds `"str"` and `"b"`, all realized, and then `Items[0] = a`, the next `Realize` recycles the generated `ListBoxItem` that showed `"str"` and returns `a` at index `0`. That wrapper, or any other wrapper that already shows such an item, then stays the item's container while it shows the item. For example, if the `ContentPresenter` subclass above holds one item, a `ContentPresenter` with `Content` `"P"`, the first `Realize` wraps it in a new `ContentPresenter`, and each later `Realize` returns that same wrapper without detaching it from its parents, clearing it, calling `CreateItemContainer`, or preparing it again while the item keeps its index and selection.

Container metadata is stored separately from the container instance. `GetItemIndex` returns `-1` when no metadata exists, `GetItem` returns `null` when no item is recorded, and `GetIsSelected` returns `false` when no selection flag is recorded.

## Constructors
| Name | Description |
| --- | --- |
| `ItemContainerGenerator(ItemsControl owner)` | Initializes a generator for `owner` and creates an empty `ItemContainerRecyclePool`. Throws `ArgumentNullException` when `owner` is `null`. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `RecyclePool` | `ItemContainerRecyclePool` | Gets the pool used to reuse detached containers by concrete container type. |
| `RealizedContainers` | `IReadOnlyDictionary<int, UIElement>` | Gets the currently realized containers keyed by item index. For the control's own generator, the keys follow inserted, removed, and moved items. |

## Methods
| Name | Return type | Description |
| --- | --- | --- |
| `Realize(RealizationWindow? window = null)` | `IReadOnlyList<UIElement>` | Realizes containers for the requested window, or for all items when `window` is `null`, moves realized containers that are their own items from an old index to their items' current indexes, and recycles containers outside that range. |
| `GetOrCreate(int index)` | `UIElement` | Returns the compatible realized container for `index` (prepared again when its recorded item, index, or selection flag differs from the current one; a container that is its own item is compatible only with that item; a container that shows another item is not compatible with a `UIElement` item of the container type or of a type derived from it, and a wrapper that already shows that item stays compatible), reuses a recycled container when available, or asks the owner to create and prepare a new container. For a `UIElement` item of the container type or of a type derived from it, it asks `CreateItemContainer` first and takes a recycled container only when the returned container is not the item. Throws `ArgumentOutOfRangeException` when `index` is outside the current item range. |
| `Recycle(int index)` | `void` | Removes the realized container for `index`, detaches it from visual and logical parents, clears owner-specific container state, and stores it in `RecyclePool` when it is reusable. |
| `Clear()` | `void` | Recycles every currently realized container. |
| `GetItemIndex(UIElement container)` | `int` | Returns the item index recorded for `container`, or `-1` when no index is recorded. After an item change, a moved container's recorded index can differ from its `RealizedContainers` key until the container is prepared again. Throws `ArgumentNullException` when `container` is `null`. |
| `GetItem(UIElement container)` | `object?` | Returns the item recorded for `container`, or `null` when no item is recorded. Throws `ArgumentNullException` when `container` is `null`. |
| `GetIsSelected(UIElement container)` | `bool` | Returns the selection flag recorded for `container`; returns `false` when no flag is recorded. Throws `ArgumentNullException` when `container` is `null`. |
| `SetInfo(UIElement container, int index, object? item, bool isSelected)` | `void` | Records item index, item value, and selection state for `container`. Throws `ArgumentNullException` when `container` is `null`. |
| `ClearInfo(UIElement container)` | `void` | Resets the recorded metadata for `container` to index `-1`, item `null`, and unselected state. Throws `ArgumentNullException` when `container` is `null`. |

## Applies to
`Cerneala.UI.Controls` item controls that realize, recycle, and describe item containers.

## See also
- `ItemsControl`
- `ItemContainerRecyclePool`
- `ItemsPresenter`
- `RealizationWindow`
