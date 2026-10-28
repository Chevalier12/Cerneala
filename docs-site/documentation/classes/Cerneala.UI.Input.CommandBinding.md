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
        savedParameter = args.Parameter;
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

As in WPF, where `CommandBinding.Executed` and `CommandBinding.CanExecute` handle only `CommandManager.ExecutedEvent` and `CommandManager.CanExecuteEvent`, the executed callback runs only for arguments whose `RoutedEvent` is `CommandEvents.ExecutedEvent`, and the can-execute callback only for `CommandEvents.CanExecuteEvent`. `CommandRouter` raises the preview events `PreviewExecutedEvent` and `PreviewCanExecuteEvent` along the route too, and the binding ignores them. So one `CommandRouter.Execute` runs the executed callback once, also when the callback leaves `Handled` at `false`, and one `CommandRouter.CanExecute` runs the can-execute callback once:

```csharp
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

UIRoot root = new(100, 100);
ButtonBase button = new();
root.VisualChildren.Add(button);
RoutedCommand save = new("Save", typeof(Program));
int executions = 0;
root.CommandBindings.Add(new CommandBinding(save, (_, _) => executions++, (_, args) => args.CanExecute = true));
root.ProcessFrame();

new CommandRouter().Execute(new RoutedCommandContext(save, button, root.InputCache.EnsureCurrent(root), null));
Console.WriteLine(executions); // 1
```

The binding does not mark the arguments handled after its executed callback runs, so the next binding for the same command in the same collection still runs, as `CommandBindingCollection` describes.

The executed callback is a `RoutedEventHandler<ExecutedRoutedEventArgs>`, so it reads command-specific data such as `Parameter` directly, without a cast. The can-execute callback receives `CanExecuteRoutedEventArgs` directly and can set `CanExecute` and `Handled`. Both callbacks receive the element whose binding runs as `sender` (an `object`).

Both callbacks can be `null`. `executed` has no default value, so pass `null` explicitly when you only need `canExecute`. If a matching event is raised and the corresponding callback is `null`, the binding does nothing. `OnExecuted` and `OnCanExecute` throw `ArgumentNullException` when their `args` argument is `null`; the constructor throws `ArgumentNullException` when `command` is `null`.

When a binding is stored in a `CommandBindingCollection`, bindings are invoked in insertion order for the current dispatch pass. The collection stops dispatching after a handler sets `Handled` to `true`.

## Constructors

| Name | Description |
| --- | --- |
| `CommandBinding(ICommand command, RoutedEventHandler<ExecutedRoutedEventArgs>? executed, Action<object, CanExecuteRoutedEventArgs>? canExecute = null)` | Initializes a command binding for `command` with an executed callback and an optional can-execute callback; both can be `null`. Throws `ArgumentNullException` when `command` is `null`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Command` | `ICommand` | Gets the command instance this binding responds to. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `OnExecuted(object sender, ExecutedRoutedEventArgs args)` | `void` | Invokes the executed callback when `args.Command` is the same object as `Command` and `args.RoutedEvent` is `CommandEvents.ExecutedEvent`; otherwise returns without invoking it. Throws `ArgumentNullException` when `args` is `null`. |
| `OnCanExecute(object sender, CanExecuteRoutedEventArgs args)` | `void` | Invokes the can-execute callback when `args.Command` is the same object as `Command` and `args.RoutedEvent` is `CommandEvents.CanExecuteEvent`; otherwise returns without invoking it. Throws `ArgumentNullException` when `args` is `null`. |

## Applies to

Project: `Cerneala`

## See also

- `UI/Input/CommandBinding.cs`
- `UI/Input/CommandBindingCollection.cs`
- `UI/Input/CommandRouter.cs`
- `UI/Input/ICommand.cs`
