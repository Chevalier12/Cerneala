# CanExecuteRoutedEventArgs Class

## Definition

Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/CanExecuteRoutedEventArgs.cs`

Provides routed event data for retained command can-execute queries.

```csharp
public sealed class CanExecuteRoutedEventArgs : RoutedEventArgs
```

Inheritance:
`object` -> `RoutedEventArgs` -> `CanExecuteRoutedEventArgs`

## Examples

```csharp
using Cerneala.UI.Input;

RoutedCommand saveCommand = new("Save", typeof(DocumentCommands));
object target = new();

CanExecuteRoutedEventArgs args = new(
    CommandEvents.CanExecuteEvent,
    target,
    saveCommand,
    "document-1");

args.CanExecute = true;
args.Handled = true;
```

## Remarks

`CanExecuteRoutedEventArgs` is used by the retained command routing path when a command asks whether it can execute for a target and parameter. The event args carry the command being queried, the optional command parameter, and the mutable `CanExecute` result.

`CommandRouter.CanExecute` raises a preview can-execute event first with `CommandEvents.PreviewCanExecuteEvent`, then raises the bubbling `CommandEvents.CanExecuteEvent` if the preview event was not handled. The bubbling event starts with the `CanExecute` value produced by preview handlers.

Command bindings only process the args when their `CommandBinding.Command` is the same command instance as `Command`; mismatched command instances are ignored.

`CanExecute` defaults to `false` because it is an unset `bool` property. Handlers set it to `true` when the command is available, and may also set `Handled` to stop further routing.

## Constructors

| Name | Description |
| --- | --- |
| `CanExecuteRoutedEventArgs(RoutedEvent, object, ICommand, object?)` | Initializes event data with a routed event, original source, command, and optional command parameter. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Command` | `ICommand` | Gets the command whose executable state is being queried. |
| `Parameter` | `object?` | Gets the optional parameter supplied to the command query. |
| `CanExecute` | `bool` | Gets or sets whether the command can execute for the current route and parameter. |

## Inherited Properties

| Name | Type | Description |
| --- | --- | --- |
| `RoutedEvent` | `RoutedEvent` | Gets the routed event represented by these args. |
| `OriginalSource` | `object` | Gets the original source passed to the constructor. |
| `Source` | `object` | Gets or sets the current routed source; command routing updates it as the event moves through route elements. |
| `Handled` | `bool` | Gets or sets whether routing should stop handling these args. |

## Exceptions

| Exception | Condition |
| --- | --- |
| `ArgumentNullException` | `routedEvent`, `originalSource`, or `command` is `null`. |

## Routed Event Information

| Event | Routing strategy | Args type |
| --- | --- | --- |
| `CommandEvents.PreviewCanExecuteEvent` | `Tunnel` | `CanExecuteRoutedEventArgs` |
| `CommandEvents.CanExecuteEvent` | `Bubble` | `CanExecuteRoutedEventArgs` |

## Applies to

Cerneala retained UI commanding.

## See also

- `Cerneala.UI.Input.CommandEvents`
- `Cerneala.UI.Input.CommandRouter`
- `Cerneala.UI.Input.CommandBinding`
- `Cerneala.UI.Input.RoutedEventArgs`
