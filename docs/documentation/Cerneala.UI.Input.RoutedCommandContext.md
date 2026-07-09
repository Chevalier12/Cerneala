# RoutedCommandContext Class

## Definition

Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/RoutedCommandContext.cs`

Carries the command, target element, retained input route map, and optional parameter used by `CommandRouter` to query or execute a routed command.

```csharp
public sealed class RoutedCommandContext
```

Inheritance:
`object` -> `RoutedCommandContext`

## Examples

Create a routed command context and execute it through `CommandRouter`:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

UIRoot root = new();
UIElement child = new();
root.VisualChildren.Add(child);

ElementInputRouteMap routeMap = new ElementInputRouteBuilder().Build(root);
RoutedCommand saveCommand = new("Save", typeof(Program));
object? receivedParameter = null;

child.CommandBindings.Add(new CommandBinding(
    saveCommand,
    (_, args) =>
    {
        receivedParameter = ((ExecutedRoutedEventArgs)args).Parameter;
        args.Handled = true;
    },
    (_, args) =>
    {
        args.CanExecute = true;
        args.Handled = true;
    }));

RoutedCommandContext context = new(saveCommand, child, routeMap, "file");
bool executed = new CommandRouter().Execute(context);
```

## Remarks

`RoutedCommandContext` is a small immutable data object for retained routed command dispatch. It does not execute a command by itself. `CommandRouter.CanExecute` and `CommandRouter.Execute` read the context to locate the target in `RouteMap`, raise command events along the retained route, and pass `Parameter` to `CanExecuteRoutedEventArgs` or `ExecutedRoutedEventArgs`.

`Command` and `RouteMap` are required. The constructor throws `ArgumentNullException` when either argument is `null`. `Target` is allowed to be `null`, but `CommandRouter` cannot build a route without a target and returns `false` for can-execute or execute operations when the target is missing from the supplied route map.

The same `Parameter` value is forwarded to both can-execute and executed command event arguments. The context stores the `ICommand` interface type, but routed dispatch is normally used with `RoutedCommand`; direct calls to `RoutedCommand.CanExecute` or `RoutedCommand.Execute` are not the retained routed-command path.

## Constructors

| Name | Description |
| --- | --- |
| `RoutedCommandContext(ICommand command, UIElement? target, ElementInputRouteMap routeMap, object? parameter = null)` | Initializes a context for a command dispatch. Throws `ArgumentNullException` when `command` or `routeMap` is `null`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Command` | `ICommand` | Gets the command instance to query or execute through the router. |
| `Target` | `UIElement?` | Gets the element where routed command dispatch starts. |
| `RouteMap` | `ElementInputRouteMap` | Gets the retained element route map used to resolve `Target` and walk the command route. |
| `Parameter` | `object?` | Gets the optional command parameter forwarded to can-execute and executed routed event arguments. |

## Applies to

Project: `Cerneala`

## See also

- `UI/Input/RoutedCommandContext.cs`
- `UI/Input/CommandRouter.cs`
- `UI/Input/RoutedCommand.cs`
- `UI/Input/CommandBinding.cs`
- `UI/Input/ElementInputRouteMap.cs`
