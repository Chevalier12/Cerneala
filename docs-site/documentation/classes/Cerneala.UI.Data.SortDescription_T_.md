# SortDescription&lt;T&gt; Class

## Definition
Namespace: `Cerneala.UI.Data`

Assembly/Project: `Cerneala`

Source: `UI/Data/SortDescription{T}.cs`

Describes one sort key and direction for items of type `T`.

```csharp
public sealed class SortDescription<T>
```

Inheritance:
`object` -> `SortDescription<T>`

## Examples
The following example adds two sort descriptions to a `CollectionView<T>`. The view sorts first by `Name` ascending, then by `Rank` descending for items with the same name.

```csharp
using Cerneala.UI.Data;

CollectionView<Person> view = new(
    [
        new Person("b", 2),
        new Person("a", 3),
        new Person("a", 1)
    ]);

view.SortDescriptions.Add(new SortDescription<Person>(person => person.Name));
view.SortDescriptions.Add(new SortDescription<Person>(person => person.Rank, descending: true));
view.Refresh();

private sealed record Person(string Name, int Rank);
```

## Remarks
`SortDescription<T>` is an immutable description object. The constructor stores a non-null key selector and a Boolean direction flag.

`CollectionView<T>` consumes `SortDescription<T>` values from its `SortDescriptions` list when `Refresh()` runs. The first sort description is applied with `OrderBy` or `OrderByDescending`; later descriptions are applied with `ThenBy` or `ThenByDescending`.

The key selector returns `IComparable?`, so the selected values must be comparable by the LINQ ordering methods used by `CollectionView<T>`.

## Constructors
| Name | Description |
| --- | --- |
| `SortDescription(Func<T, IComparable?> keySelector, bool descending = false)` | Initializes a sort description with a key selector and optional descending direction. Throws `ArgumentNullException` when `keySelector` is `null`. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `KeySelector` | `Func<T, IComparable?>` | Gets the function used to select the comparable sort key from an item. |
| `Descending` | `bool` | Gets whether the sort direction is descending. The default constructor argument is `false`. |

## Applies To
`Cerneala` project, `Cerneala.UI.Data` namespace.

## See Also
- `CollectionView<T>`
