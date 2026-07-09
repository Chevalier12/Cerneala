# CommandEvents Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/CommandEvents.cs`

Defines the routed events used by retained UI commanding for can-execute queries and command execution.

```csharp
public static class CommandEvents
```

## Examples

```csharp
using Cerneala.UI.Input;

RoutedEvent canExecuteEvent = CommandEvents.CanExecuteEvent;

if (canExecuteEvent.RoutingStrategy == RoutingStrategy.Bubble &&
    canExecuteEvent.ArgsType == typeof(CanExecuteRoutedEventArgs))
{
    CanExecuteRoutedEventArgs args = new(
        canExecuteEvent,
        originalSource: "target",
        command: new ActionCommand(_ => Save()),
        parameter: "document");

    args.CanExecute = true;
    args.Handled = true;
}
```

## Remarks

`CommandEvents` is a static catalog for the four routed events used by `CommandRouter`. Preview events use `RoutingStrategy.Tunnel`, and non-preview events use `RoutingStrategy.Bubble`.

During `CommandRouter.CanExecute`, `PreviewCanExecuteEvent` is raised first along the route from root toward the command target. If that event is handled, the router returns its `CanExecute` value. Otherwise, `CanExecuteEvent` is raised from the target back toward the root.

During `CommandRouter.Execute`, the router first checks `CanExecute`. If execution is allowed, `PreviewExecutedEvent` is raised from root toward target. If it is not handled, `ExecutedEvent` is raised from target toward root.

Each event is registered with `OwnerType` set to `typeof(CommandEvents)`. The can-execute events use `CanExecuteRoutedEventArgs`; the execution events use `ExecutedRoutedEventArgs`.

## Fields

| Name | Routed event name | Routing strategy | Event args type | Description |
| --- | --- | --- | --- | --- |
| `PreviewCanExecuteEvent` | `PreviewCanExecute` | `Tunnel` | `CanExecuteRoutedEventArgs` | Preview routed event for querying whether a command can execute before the bubble phase. |
| `CanExecuteEvent` | `CanExecute` | `Bubble` | `CanExecuteRoutedEventArgs` | Bubble routed event for querying whether a command can execute. |
| `PreviewExecutedEvent` | `PreviewExecuted` | `Tunnel` | `ExecutedRoutedEventArgs` | Preview routed event raised before command execution reaches the bubble phase. |
| `ExecutedEvent` | `Executed` | `Bubble` | `ExecutedRoutedEventArgs` | Bubble routed event raised for command execution. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `All` | `IReadOnlyList<RoutedEvent>` | Returns the command routed events in declaration order: `PreviewCanExecuteEvent`, `CanExecuteEvent`, `PreviewExecutedEvent`, and `ExecutedEvent`. |

## Routed Event Information

| Item | Value |
| --- | --- |
| Owner type | `CommandEvents` |
| Preview can-execute identifier field | `PreviewCanExecuteEvent` |
| Can-execute identifier field | `CanExecuteEvent` |
| Preview executed identifier field | `PreviewExecutedEvent` |
| Executed identifier field | `ExecutedEvent` |

## Applies to

Cerneala retained UI commanding.

## See also

- [ActionCommand](Cerneala.UI.Input.ActionCommand.md)
- `Cerneala.UI.Input.CommandRouter`
- `Cerneala.UI.Input.CommandBinding`
- `Cerneala.UI.Input.RoutedEvent`
