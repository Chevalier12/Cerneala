# RoutedCommand Class

## Definition

Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/RoutedCommand.cs`

Represents a named command that must be executed through the retained UI command router.

```csharp
public sealed class RoutedCommand : ICommand
```

Inheritance:
`object` -> `RoutedCommand`

Implements:
`ICommand`

## Examples

Create a routed command, attach a command binding to an element, and execute it through `CommandRouter`:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

UIRoot root = new();
UIElement editor = new();
root.VisualChildren.Add(editor);

ElementInputRouteMap routeMap = new ElementInputRouteBuilder().Build(root);
RoutedCommand saveCommand = new("Save", typeof(Program));

editor.CommandBindings.Add(new CommandBinding(
    saveCommand,
    (_, args) =>
    {
        ExecutedRoutedEventArgs executedArgs = (ExecutedRoutedEventArgs)args;
        Save(executedArgs.Parameter);
        args.Handled = true;
    },
    (_, args) =>
    {
        args.CanExecute = args.Parameter is string;
        args.Handled = true;
    }));

CommandRouter router = new();
RoutedCommandContext context = new(saveCommand, editor, routeMap, "document.cerneala");

if (router.CanExecute(context))
{
    router.Execute(context);
}
```

## Remarks

`RoutedCommand` is an `ICommand` marker for commands whose executable state and execution are resolved along an element route. It stores only the command `Name` and `OwnerType`; it does not store execute or can-execute delegates.

Callers should use `CommandRouter.CanExecute` and `CommandRouter.Execute` with a `RoutedCommandContext`. The direct `ICommand.CanExecute` and `ICommand.Execute` members intentionally throw `InvalidOperationException` because a routed command needs a retained command context, target element, and `ElementInputRouteMap`.

`ButtonBase` and routed `InputBinding` paths detect `RoutedCommand` and delegate to `CommandRouter` instead of invoking the command directly. Non-routed commands are executed through their `ICommand` members.

The constructor rejects empty or whitespace command names and a `null` owner type.

## Constructors

| Name | Description |
| --- | --- |
| `RoutedCommand(string name, Type ownerType)` | Initializes a routed command with a non-empty name and declaring owner type. Throws `ArgumentException` when `name` is empty or whitespace, and `ArgumentNullException` when `ownerType` is `null`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Name` | `string` | Gets the command name supplied to the constructor. |
| `OwnerType` | `Type` | Gets the type that owns or declares the command. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `CanExecute(object? parameter)` | `bool` | Always throws `InvalidOperationException`; use `CommandRouter.CanExecute(RoutedCommandContext)` for routed commands. |
| `Execute(object? parameter)` | `void` | Always throws `InvalidOperationException`; use `CommandRouter.Execute(RoutedCommandContext)` for routed commands. |

## Applies to

Cerneala retained UI commanding.

## See also

- `Cerneala.UI.Input.CommandRouter`
- `Cerneala.UI.Input.RoutedCommandContext`
- `Cerneala.UI.Input.CommandBinding`
- `Cerneala.UI.Input.ICommand`
