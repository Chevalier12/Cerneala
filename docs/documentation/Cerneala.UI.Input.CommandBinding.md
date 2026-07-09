# CommandBinding Class

## Definition

Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/CommandBinding.cs`

Connects an `ICommand` instance to optional routed executed and can-execute handlers.

```csharp
public sealed class CommandBinding
```

Inheritance:
`object` -> `CommandBinding`

## Examples

Register a routed command binding on a UI element:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

UIElement element = new();
RoutedCommand saveCommand = new("Save", typeof(Program));
object? savedParameter = null;

element.CommandBindings.Add(new CommandBinding(
    saveCommand,
    (_, args) =>
    {
        ExecutedRoutedEventArgs executedArgs = (ExecutedRoutedEventArgs)args;
        savedParameter = executedArgs.Parameter;
        args.Handled = true;
    },
    (_, args) =>
    {
        args.CanExecute = args.Parameter is string;
        args.Handled = true;
    }));
```

## Remarks

`CommandBinding` is the per-command callback object used by `CommandBindingCollection` and `CommandRouter`. A binding stores one command reference and invokes its callbacks only when the routed event arguments carry the same command object. Matching uses reference equality through `ReferenceEquals(Command, args.Command)`.

The executed callback uses the general `RoutedEventHandler` delegate, so code that needs command-specific data such as `Parameter` casts the event arguments to `ExecutedRoutedEventArgs`. The can-execute callback receives `CanExecuteRoutedEventArgs` directly and can set `CanExecute` and `Handled`.

Both callbacks are optional. If a matching event is raised and the corresponding callback is `null`, the binding does nothing. `OnExecuted` and `OnCanExecute` throw `ArgumentNullException` when their `args` argument is `null`; the constructor throws `ArgumentNullException` when `command` is `null`.

When a binding is stored in a `CommandBindingCollection`, bindings are invoked in insertion order for the current dispatch pass. The collection stops dispatching after a handler sets `Handled` to `true`.

## Constructors

| Name | Description |
| --- | --- |
| `CommandBinding(ICommand command, RoutedEventHandler? executed, Action<UiElementId, CanExecuteRoutedEventArgs>? canExecute = null)` | Initializes a command binding for `command` with optional executed and can-execute callbacks. Throws `ArgumentNullException` when `command` is `null`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Command` | `ICommand` | Gets the command instance this binding responds to. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `OnExecuted(UiElementId sender, ExecutedRoutedEventArgs args)` | `void` | Invokes the executed callback when `args.Command` is the same object as `Command`; otherwise returns without invoking it. Throws `ArgumentNullException` when `args` is `null`. |
| `OnCanExecute(UiElementId sender, CanExecuteRoutedEventArgs args)` | `void` | Invokes the can-execute callback when `args.Command` is the same object as `Command`; otherwise returns without invoking it. Throws `ArgumentNullException` when `args` is `null`. |

## Applies to

Project: `Cerneala`

## See also

- `UI/Input/CommandBinding.cs`
- `UI/Input/CommandBindingCollection.cs`
- `UI/Input/CommandRouter.cs`
- `UI/Input/ICommand.cs`
