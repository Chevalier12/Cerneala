# ItemCollection Class

## Definition
Namespace: `Cerneala.UI.Controls.Items`
Assembly/Project: `Cerneala`
Source: `UI/Controls/Items/ItemCollection.cs`

Represents the retained local item list used by `ItemsControl` when `ItemsSource` is not set.

```csharp
public sealed class ItemCollection : IList<object?>
```

Inheritance:
`object` -> `ItemCollection`

Implements:
`IList<object?>`, `ICollection<object?>`, `IEnumerable<object?>`, `IEnumerable`

## Examples

```csharp
using Cerneala.UI.Controls;

var itemsControl = new ItemsControl();

itemsControl.Items.Add("one");
itemsControl.Items.Add("two");

int count = itemsControl.Items.Count;      // 2
object? first = itemsControl.Items[0];     // "one"
```

```csharp
using Cerneala.UI.Controls;

var items = new ItemCollection();
items.Changed += (_, _) =>
{
    // Refresh item-dependent state.
};

items.ReplaceWith(new object?[] { "alpha", "beta" });
```

## Remarks

`ItemCollection` stores nullable `object` items and exposes list-style operations through `IList<object?>`. It is the backing collection for `ItemsControl.Items`; `ItemsControl.SetItems(IEnumerable?)` replaces this collection's contents through `ReplaceWith`.

Mutating operations raise `Changed` when the collection's observable contents change. `Add`, `Insert`, and `RemoveAt` always raise it after a successful list mutation. `Remove` raises it only when an item was removed. `Clear` raises it only when the collection was not already empty.

Setting an indexed item or calling `ReplaceWith` does not raise `Changed` when the new value or sequence is equal to the current value or sequence according to `Equals`/`SequenceEqual`. In those cases the internal list is still assigned or rebuilt from the supplied values. When used by `ItemsControl`, `Changed` invalidates the retained item presentation only while `ItemsSource` is `null`.

`ReplaceWith(null)` replaces the collection with an empty sequence. The class does not implement `INotifyCollectionChanged`; consumers observe changes through the `Changed` event.

## Constructors

| Name | Description |
| --- | --- |
| `ItemCollection()` | Initializes an empty item collection. |

## Properties

| Name | Description |
| --- | --- |
| `Count` | Gets the number of items in the collection. |
| `IsReadOnly` | Gets `false`; the collection is mutable. |
| `this[int index]` | Gets or sets the item at the specified zero-based index. Setting raises `Changed` only when the assigned value is not equal to the existing value. |

## Methods

| Name | Description |
| --- | --- |
| `Add(object?)` | Appends an item and raises `Changed`. |
| `Clear()` | Removes all items and raises `Changed` when the collection was not already empty. |
| `Contains(object?)` | Returns whether the collection contains the specified item. |
| `CopyTo(object?[], int)` | Copies the items to an array starting at the specified array index. |
| `GetEnumerator()` | Returns the generic enumerator for the collection. |
| `IndexOf(object?)` | Returns the zero-based index of the first matching item, or `-1` when not found. |
| `Insert(int, object?)` | Inserts an item at the specified index and raises `Changed`. |
| `Remove(object?)` | Removes the first matching item and raises `Changed` only when removal succeeds. |
| `RemoveAt(int)` | Removes the item at the specified index and raises `Changed`. |
| `ReplaceWith(IEnumerable?)` | Replaces the collection with the supplied sequence, or with an empty sequence when the source is `null`; raises `Changed` only when the resulting sequence differs from the current one. |

## Events

| Name | Description |
| --- | --- |
| `Changed` | Raised after an observable collection mutation. |

## Explicit Interface Implementations

| Name | Description |
| --- | --- |
| `IEnumerable.GetEnumerator()` | Returns the non-generic enumerator for the collection. |

## Applies to

Project: `Cerneala`

## See also

- Source: `UI/Controls/Items/ItemCollection.cs`
- `ItemsControl`
