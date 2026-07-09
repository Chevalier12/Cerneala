# CollectionView<T> Class

## Definition
Namespace: `Cerneala.UI.Data`

Assembly/Project: `Cerneala`

Source: `UI/Data/CollectionView{T}.cs`

Represents a read-only, refreshable view over an enumerable source with optional filtering, sorting, and observable-source refresh notifications.

```csharp
public sealed class CollectionView<T> : IReadOnlyList<T>, IDisposable
```

Inheritance:
`Object` -> `CollectionView<T>`

Implements:
`IReadOnlyList<T>`, `IReadOnlyCollection<T>`, `IEnumerable<T>`, `IEnumerable`, `IDisposable`

## Examples

```csharp
using Cerneala.UI.Data;

CollectionView<int> view = new([1, 2, 3, 4])
{
    Filter = value => value % 2 == 0
};

view.Refresh();

int[] visibleItems = view.ToArray(); // [2, 4]
```

```csharp
using Cerneala.UI.Data;

ObservableList<string> source = new(["Ink", "Brush"]);
using CollectionView<string> view = new(source);

view.Changed += (_, args) =>
{
    ObservableListChangeKind kind = args.Kind; // Reset
    IReadOnlyList<string> currentItems = args.Items;
};

source.Add("Canvas");
```

## Remarks

`CollectionView<T>` materializes the current view into an internal list. The constructor builds the initial view without raising `Changed`.

Filtering is applied before sorting. Set `Filter` to include only matching source items, and add one or more `SortDescription<T>` entries to `SortDescriptions` to order the view. Changing `Filter` or `SortDescriptions` does not rebuild the view by itself; call `Refresh()` after changing view settings.

When the source implements `IObservableList<T>`, the view subscribes to source changes and refreshes automatically. Manual and automatic refreshes raise `Changed` with `ObservableListChangeKind.Reset`, `Items` set to the refreshed view snapshot, and `OldItems` set to the previous view snapshot.

Call `Dispose()` to unsubscribe from an observable source. Calling `Refresh()` after disposal throws `ObjectDisposedException`.

## Constructors

| Name | Description |
| --- | --- |
| `CollectionView(IEnumerable<T>)` | Initializes a view over the supplied source and builds the initial snapshot. Throws `ArgumentNullException` when `source` is `null`. |

## Properties

| Name | Description |
| --- | --- |
| `Count` | Gets the number of items in the current view snapshot. |
| `Filter` | Gets or sets the predicate used to include source items during refresh. |
| `SortDescriptions` | Gets the mutable list of sort descriptions applied in order during refresh. |
| `this[int index]` | Gets the item at the specified index in the current view snapshot. |

## Methods

| Name | Description |
| --- | --- |
| `Dispose()` | Marks the view as disposed and unsubscribes from the source when it implements `IObservableList<T>`. |
| `GetEnumerator()` | Returns an enumerator over the current view snapshot. |
| `Refresh()` | Rebuilds the view from the source, applies the current filter and sort descriptions, and raises `Changed`. Throws `ObjectDisposedException` after disposal. |

## Events

| Name | Description |
| --- | --- |
| `Changed` | Raised when `Refresh()` rebuilds the view with notification enabled, including refreshes caused by an observable source change. |

## Explicit Interface Implementations

| Name | Description |
| --- | --- |
| `IEnumerable.GetEnumerator()` | Returns an untyped enumerator over the current view snapshot. |

## Applies to

Cerneala retained UI data binding and item controls that need a filtered or sorted read-only view over source items.

## See also

- `Cerneala.UI.Data.FilterPredicate<T>`
- `Cerneala.UI.Data.SortDescription<T>`
- `Cerneala.UI.Data.IObservableList<T>`
- `Cerneala.UI.Data.ObservableList<T>`
- `Cerneala.UI.Data.ObservableListChangedEventArgs<T>`
