# CommandBindingCollection Class

## Definition

Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/CommandBindingCollection.cs`

Stores command bindings for an element and dispatches routed command events to matching bindings in collection order.

```csharp
public sealed class CommandBindingCollection : IReadOnlyList<CommandBinding>
```

Inheritance:
`Object` -> `CommandBindingCollection`

Implements:
`IEnumerable<CommandBinding>`, `IReadOnlyCollection<CommandBinding>`, `IReadOnlyList<CommandBinding>`, `IEnumerable`

## Examples

```csharp
using Cerneala.UI.Input;

RoutedCommand saveCommand = new("Save", typeof(EditorCommands));
CommandBindingCollection bindings = new();
List<string> calls = [];

bindings.Add(new CommandBinding(
    saveCommand,
    executed: (_, args) =>
    {
        calls.Add("save");
        args.Handled = true;
    },
    canExecute: (_, args) =>
    {
        args.CanExecute = true;
        args.Handled = true;
    }));

bindings.InvokeCanExecute(
    new UiElementId("editor"),
    new CanExecuteRoutedEventArgs(CommandEvents.CanExecuteEvent, "editor", saveCommand, null));

bindings.InvokeExecuted(
    new UiElementId("editor"),
    new ExecutedRoutedEventArgs(CommandEvents.ExecutedEvent, "editor", saveCommand, null));
```

## Remarks

`CommandBindingCollection` is the storage and dispatch surface behind `UIElement.CommandBindings`. Add `CommandBinding` instances to connect an `ICommand` to executed and can-execute routed event handlers.

Bindings are stored in insertion order. `InvokeCanExecute` and `InvokeExecuted` walk the bindings that existed when the invocation started, so a binding added by a handler is not called during that same dispatch pass. Dispatch stops as soon as the routed event arguments are marked as handled.

Each `CommandBinding` only invokes its callbacks when its `Command` reference matches the command in the routed event arguments. Non-matching bindings are skipped by the binding.

When the collection is owned by a `UIElement`, `Add`, successful `Remove`, and non-empty `Clear` queue command-state refreshes for the owner and its visual descendants. A standalone collection created with the public constructor does not have an owner and only updates its local list.

## Constructors

| Name | Description |
| --- | --- |
| `CommandBindingCollection()` | Initializes an empty standalone collection. |

## Properties

| Name | Description |
| --- | --- |
| `Count` | Gets the number of command bindings in the collection. |
| `this[int index]` | Gets the binding at the specified zero-based index. |

## Methods

| Name | Description |
| --- | --- |
| `Add(CommandBinding)` | Adds a non-null binding and notifies the owner, when present. |
| `Remove(CommandBinding)` | Removes a non-null binding and returns whether it was found. Owner notification occurs only when a binding is removed. |
| `Clear()` | Removes all bindings. Owner notification occurs only when the collection was not already empty. |
| `InvokeCanExecute(UiElementId, CanExecuteRoutedEventArgs)` | Invokes can-execute handling on bindings in insertion order until the args are handled or the initial binding count is exhausted. |
| `InvokeExecuted(UiElementId, ExecutedRoutedEventArgs)` | Invokes executed handling on bindings in insertion order until the args are handled or the initial binding count is exhausted. |
| `GetEnumerator()` | Returns an enumerator over the stored bindings. |

## Explicit Interface Implementations

| Name | Description |
| --- | --- |
| `IEnumerable.GetEnumerator()` | Returns the same enumerator as `GetEnumerator()`. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Add(CommandBinding)` | `ArgumentNullException` | `binding` is `null`. |
| `Remove(CommandBinding)` | `ArgumentNullException` | `binding` is `null`. |
| `InvokeCanExecute(UiElementId, CanExecuteRoutedEventArgs)` | `ArgumentNullException` | `args` is `null`. |
| `InvokeExecuted(UiElementId, ExecutedRoutedEventArgs)` | `ArgumentNullException` | `args` is `null`. |

## Applies to

Cerneala retained UI commanding.

## See also

- `Cerneala.UI.Elements.UIElement.CommandBindings`
- `Cerneala.UI.Input.CommandBinding`
- `Cerneala.UI.Input.CommandRouter`
- `Cerneala.UI.Input.RoutedCommand`
