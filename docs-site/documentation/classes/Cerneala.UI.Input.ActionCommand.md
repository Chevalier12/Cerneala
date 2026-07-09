# ActionCommand Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/ActionCommand.cs`

Adapts delegates into an observable command that can be queried, executed, and invalidated.

```csharp
public sealed class ActionCommand : IObservableCommand
```

Implements:
`ICommand`, `IObservableCommand`

## Examples

```csharp
using Cerneala.UI.Input;

ActionCommand saveCommand = new(
    execute: parameter => Save(),
    canExecute: parameter => CanSave);

if (saveCommand.CanExecute(null))
{
    saveCommand.Execute(null);
}

saveCommand.RaiseCanExecuteChanged();
```

## Remarks

`ActionCommand` is a small command implementation backed by an execute delegate and an optional `canExecute` predicate. When no predicate is provided, `CanExecute` returns `true`.

`Execute` checks `CanExecute` before invoking the execute delegate. If the command cannot execute for the supplied parameter, `Execute` returns without doing anything.

Call `RaiseCanExecuteChanged` when external state changes and command consumers need to refresh their enabled state.

## Constructors

| Name | Description |
| --- | --- |
| `ActionCommand(Action<object?>, Predicate<object?>?)` | Initializes a command from an execute delegate and optional can-execute predicate. |

## Methods

| Name | Description |
| --- | --- |
| `CanExecute(object?)` | Returns whether the command can execute for the supplied parameter. |
| `Execute(object?)` | Executes the command when `CanExecute` returns `true`. |
| `RaiseCanExecuteChanged()` | Raises `CanExecuteChanged`. |

## Events

| Name | Description |
| --- | --- |
| `CanExecuteChanged` | Raised when command executable state should be refreshed. |

## Applies to

Cerneala retained UI commanding.

## See also

- `Cerneala.UI.Input.ICommand`
- `Cerneala.UI.Input.IObservableCommand`
- `Cerneala.UI.Input.RoutedCommand`
