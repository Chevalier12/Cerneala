# InputBindingCollection Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/InputBindingCollection.cs`

Stores the input bindings associated with a retained UI element.

```csharp
public sealed class InputBindingCollection : Collection<InputBinding>
```

Inheritance:
`object` -> `Collection<InputBinding>` -> `InputBindingCollection`

Implements:
`IEnumerable<InputBinding>`, `IList<InputBinding>`, `ICollection<InputBinding>`, `IReadOnlyList<InputBinding>`, `IReadOnlyCollection<InputBinding>`, `IEnumerable`, `IList`, `ICollection`

## Examples
The following example attaches a keyboard command binding to a focusable element.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

UIElement editor = new() { Focusable = true };

editor.InputBindings.Add(new KeyBinding(
    new ActionCommand(parameter => Save((string?)parameter)),
    InputKey.S,
    KeyModifiers.Control,
    "document-1"));

static void Save(string? documentId)
{
    // Persist the selected document.
}
```

## Remarks
`InputBindingCollection` is the concrete collection type exposed by `UIElement.InputBindings`. It stores `InputBinding` instances, including specialized bindings such as `KeyBinding`, in collection order.

The retained input binding processor walks the focused element and then its visual ancestors. For each valid attached, enabled, input-participating element, it enumerates that element's `InputBindings` collection and executes the first binding whose gesture matches and whose command can execute. When a binding executes, default keyboard activation for that dispatch result is suppressed.

The collection inherits the standard `Collection<InputBinding>` editing surface. Adding or replacing an item rejects `null` by overriding `InsertItem` and `SetItem`. Removing, clearing, enumerating, and indexing follow the base collection behavior.

## Constructors
| Name | Description |
| --- | --- |
| `InputBindingCollection()` | Initializes an empty collection. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `Count` | `int` | Gets the number of input bindings in the collection. Inherited from `Collection<InputBinding>`. |
| `Items` | `IList<InputBinding>` | Gets the underlying item list. Inherited from `Collection<InputBinding>`. |
| `this[int index]` | `InputBinding` | Gets or sets the binding at the specified zero-based index. Setting rejects `null`. Inherited from `Collection<InputBinding>` through the overridden `SetItem` path. |

## Methods
| Name | Return Type | Description |
| --- | --- | --- |
| `Add(InputBinding item)` | `void` | Adds a non-null binding to the end of the collection. Inherited from `Collection<InputBinding>` through the overridden `InsertItem` path. |
| `Clear()` | `void` | Removes all bindings from the collection. Inherited from `Collection<InputBinding>`. |
| `Contains(InputBinding item)` | `bool` | Returns whether the collection contains the specified binding. Inherited from `Collection<InputBinding>`. |
| `CopyTo(InputBinding[] array, int index)` | `void` | Copies the collection to an array starting at the specified index. Inherited from `Collection<InputBinding>`. |
| `GetEnumerator()` | `IEnumerator<InputBinding>` | Returns an enumerator over the bindings. Inherited from `Collection<InputBinding>`. |
| `IndexOf(InputBinding item)` | `int` | Returns the zero-based index of a binding, or `-1` when it is not found. Inherited from `Collection<InputBinding>`. |
| `Insert(int index, InputBinding item)` | `void` | Inserts a non-null binding at the specified index. Inherited from `Collection<InputBinding>` through the overridden `InsertItem` path. |
| `Remove(InputBinding item)` | `bool` | Removes the first occurrence of the specified binding and returns whether it was found. Inherited from `Collection<InputBinding>`. |
| `RemoveAt(int index)` | `void` | Removes the binding at the specified index. Inherited from `Collection<InputBinding>`. |

## Protected Methods
| Name | Return Type | Description |
| --- | --- | --- |
| `InsertItem(int index, InputBinding item)` | `void` | Inserts a binding after rejecting `null`. |
| `SetItem(int index, InputBinding item)` | `void` | Replaces a binding after rejecting `null`. |

## Exceptions
| Member | Exception | Condition |
| --- | --- | --- |
| `Add(InputBinding)`, `Insert(int, InputBinding)`, indexer setter | `ArgumentNullException` | The binding is `null`. |

## Applies to
Cerneala retained UI input and command routing.

## See also
- `Cerneala.UI.Elements.UIElement.InputBindings`
- `Cerneala.UI.Input.InputBinding`
- `Cerneala.UI.Input.KeyBinding`
- `Cerneala.UI.Input.RetainedInputBindingProcessor`
