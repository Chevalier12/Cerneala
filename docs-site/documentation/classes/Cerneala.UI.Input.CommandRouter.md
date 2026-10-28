# CommandRouter Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/CommandRouter.cs`

Routes retained UI command queries and execution through command bindings on an element route.

```csharp
public sealed class CommandRouter
```

Inheritance:
`Object` -> `CommandRouter`

## Examples

The router is used with a `RoutedCommandContext` that supplies the command, target element, current input route map, and optional command parameter. The target must be present in the supplied route map.

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

static class EditorCommands
{
    public static readonly RoutedCommand Save = new("Save", typeof(EditorCommands));
}

static bool ExecuteSave(UIRoot root, Button saveButton)
{
    saveButton.Command = EditorCommands.Save;
    saveButton.CommandParameter = "document-42";
    saveButton.CommandBindings.Add(new CommandBinding(
        EditorCommands.Save,
        (_, args) =>
        {
            ExecutedRoutedEventArgs executedArgs = (ExecutedRoutedEventArgs)args;
            Save(executedArgs.Parameter);
            args.Handled = true;
        },
        (_, args) =>
        {
            args.CanExecute = CanSave(args.Parameter);
            args.Handled = true;
        }));

    ElementInputRouteMap routeMap = root.InputCache.EnsureCurrent(root);
    CommandRouter router = new();
    RoutedCommandContext context = new(EditorCommands.Save, saveButton, routeMap, saveButton.CommandParameter);

    return router.CanExecute(context) && router.Execute(context);
}

static bool CanSave(object? parameter) => parameter is not null;

static void Save(object? parameter)
{
    // Persist the document represented by parameter.
}
```

## Remarks

`CommandRouter` is the retained UI dispatcher for `RoutedCommand` command state and execution. It does not execute `ICommand` directly; instead, it raises command binding callbacks along the target element's input route.

`CanExecute` first raises `CommandEvents.PreviewCanExecuteEvent` from the root toward the target. If the preview arguments are handled, the method returns the preview `CanExecute` value. Otherwise, it raises `CommandEvents.CanExecuteEvent` from the target toward the root and returns the final `CanExecute` value.

`Execute` calls `CanExecute` before raising execution events. If the command cannot execute, if the context has no target, or if the target is not present in the route map, it returns `false`. When execution proceeds, it raises `CommandEvents.PreviewExecutedEvent` from root to target; if that event is not handled, it raises `CommandEvents.ExecutedEvent` from target to root. The method returns `true` once the command passes `CanExecute` and execution routing is attempted.

A `CommandBinding` answers only the bubbling events: its executed callback runs for `ExecutedEvent` and its can-execute callback for `CanExecuteEvent`, not for the preview events. So one `Execute` runs a binding's executed callback once, and one `CanExecute` runs its can-execute callback once.

During routing, `RoutedEventArgs.Source` is updated to the current `UIElement`, and the same element is passed to the binding callbacks as `sender`. Routing stops as soon as the event arguments are marked handled. Route elements that the route map does not contain (or whose input route is unavailable) are skipped.

## Constructors

| Name | Description |
| --- | --- |
| `CommandRouter()` | Initializes a command router. |

## Methods

| Name | Description |
| --- | --- |
| `CanExecute(RoutedCommandContext)` | Returns whether the routed command can execute for the supplied retained command context. Raises preview and bubble can-execute command binding callbacks. |
| `Execute(RoutedCommandContext)` | Checks `CanExecute`, then raises `PreviewExecutedEvent`, and raises `ExecutedEvent` only when the preview was not handled. Returns `true` when `CanExecute` passed. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `CanExecute(RoutedCommandContext)` | `ArgumentNullException` | `context` is `null`. |
| `Execute(RoutedCommandContext)` | `ArgumentNullException` | `context` is `null`. |

## Applies to

Cerneala retained UI command routing for input bindings, keyboard activation, and command source controls.

## See also

- `Cerneala.UI.Input.RoutedCommandContext`
- `Cerneala.UI.Input.RoutedCommand`
- `Cerneala.UI.Input.CommandBinding`
- `Cerneala.UI.Input.CommandEvents`
- `Cerneala.UI.Input.IInputCommandSource`
