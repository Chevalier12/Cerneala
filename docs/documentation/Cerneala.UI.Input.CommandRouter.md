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
            Save(args.Parameter);
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

During routing, `RoutedEventArgs.Source` is updated to the current `UiElementId`. Routing stops as soon as the event arguments are marked handled. Missing element ids in the route map are skipped.

## Constructors

| Name | Description |
| --- | --- |
| `CommandRouter()` | Initializes a command router. |

## Methods

| Name | Description |
| --- | --- |
| `CanExecute(RoutedCommandContext)` | Returns whether the routed command can execute for the supplied retained command context. Raises preview and bubble can-execute command binding callbacks. |
| `Execute(RoutedCommandContext)` | Executes a routed command by first checking `CanExecute`, then raising preview and bubble executed command binding callbacks when allowed. |

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
