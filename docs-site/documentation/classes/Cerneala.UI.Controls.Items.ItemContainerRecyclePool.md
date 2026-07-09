# ItemContainerRecyclePool Class

## Definition
Namespace: `Cerneala.UI.Controls.Items`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Items/ItemContainerRecyclePool.cs`

Stores recyclable item containers grouped by their exact runtime `UIElement` type.

```csharp
public sealed class ItemContainerRecyclePool
```

Inheritance:
`object` -> `ItemContainerRecyclePool`

## Examples

Reuse containers by exact container type:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;

ItemContainerRecyclePool pool = new();

ListBoxItem listBoxItem = new();
TabItem tabItem = new();

pool.Push(listBoxItem);
pool.Push(tabItem);

UIElement? reusedListBoxItem = pool.Pop(typeof(ListBoxItem));
UIElement? reusedTabItem = pool.Pop(typeof(TabItem));
UIElement? missingPresenter = pool.Pop(typeof(ContentPresenter));
```

## Remarks

`ItemContainerRecyclePool` is used by `ItemContainerGenerator` to keep detached item containers available for later reuse. Containers are stored in separate stacks keyed by `container.GetType()`, so lookup is based on the exact runtime type passed to `Pop(Type)`.

Within each type bucket, recycling is last-in, first-out because the implementation uses `Stack<UIElement>`. `Pop(Type)` returns `null` when no stack exists for the requested type or when that stack is empty.

`Count` returns the total number of containers currently held across all type buckets. `Clear()` removes every stored container from the pool.

## Constructors

| Name | Description |
| --- | --- |
| `ItemContainerRecyclePool()` | Creates an empty recycle pool. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Count` | `int` | Gets the total number of stored containers across all container-type stacks. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Push(UIElement container)` | `void` | Adds `container` to the stack keyed by its exact runtime type. Throws `ArgumentNullException` when `container` is `null`. |
| `Pop(Type containerType)` | `UIElement?` | Removes and returns the most recently pushed container for `containerType`, or `null` when none is available. Throws `ArgumentNullException` when `containerType` is `null`. |
| `Clear()` | `void` | Removes all stored containers from the pool. |

## Applies To

`Cerneala` retained UI item-container generation and recycling.

## See Also

- `Cerneala.UI.Controls.Items.ItemContainerGenerator`
- `Cerneala.UI.Controls.ItemsControl`
- `Cerneala.UI.Elements.UIElement`
