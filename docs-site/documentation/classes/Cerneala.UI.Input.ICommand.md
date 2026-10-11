# ICommand Interface

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/ICommand.cs`

Defines an action that a command source, such as a `Button` or a `MenuItem`, can check and run.

```csharp
public interface ICommand
```

## Examples

A command that implements only `ICommand` does not tell the button when `CanExecute` changes. The button reads it again only when a command-state refresh is queued.

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

SwitchCommand save = new();
Button button = new() { Command = save };
UIRoot root = new(200, 200);
root.VisualChildren.Add(button);
root.ProcessFrame();
Console.WriteLine(button.IsEnabled); // True

save.Allowed = false;
root.ProcessFrame();
Console.WriteLine(button.IsEnabled); // True, nothing queued a refresh

button.QueueCommandStateRefresh();
root.ProcessFrame();
Console.WriteLine(button.IsEnabled); // False

sealed class SwitchCommand : ICommand
{
    public bool Allowed { get; set; } = true;

    public bool CanExecute(object? parameter) => Allowed;

    public void Execute(object? parameter) => Console.WriteLine("saved");
}
```

## Remarks

`ICommand` has no change event. `IObservableCommand` extends it with `CanExecuteChanged`. The built-in implementations are:

| Type | Behavior |
| --- | --- |
| `ActionCommand` | Implements `IObservableCommand`. `RaiseCanExecuteChanged` raises `CanExecuteChanged`. |
| `RoutedCommand` | Throws `InvalidOperationException` from both `CanExecute` and `Execute`. It runs only through `CommandRouter`, which finds a `CommandBinding` on the route. |

`ButtonBase` and `MenuItem` use a command like this:

- **State refresh.** During the command-state phase of `UIRoot.ProcessFrame`, the element calls `CanExecute(CommandParameter)` and sets `IsEnabled` to the result. A `null` command leaves `IsEnabled` unchanged. Because the refresh writes `IsEnabled`, a button set to `IsEnabled = false` with an executable command becomes enabled again.
- **When a refresh is queued.** When the element is attached, when `Command` or `CommandParameter` changes, when an `IObservableCommand` raises `CanExecuteChanged`, and when you call `UIElement.QueueCommandStateRefresh`. With a plain `ICommand`, as in the example, only the first, second, and last cases apply.
- **Execution.** On a left-button click, Enter, or Space, the element runs `Execute(CommandParameter)` only when `IsEnabled` is `true` and `CanExecute(CommandParameter)` returns `true` at that moment.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `CanExecute(object? parameter)` | `bool` | Returns whether the command can run with `parameter`. Command sources pass their `CommandParameter`. |
| `Execute(object? parameter)` | `void` | Runs the command with `parameter`. |

## Applies to

`ButtonBase`, `MenuItem`, `KeyBinding`, `ActionCommand`, and `RoutedCommand`.

## See also

- `Cerneala.UI.Input.IObservableCommand`
- `Cerneala.UI.Input.ActionCommand`
- `Cerneala.UI.Input.RoutedCommand`
- `Cerneala.UI.Input.CommandRouter`
- `Cerneala.UI.Input.ICommandStateSource`
