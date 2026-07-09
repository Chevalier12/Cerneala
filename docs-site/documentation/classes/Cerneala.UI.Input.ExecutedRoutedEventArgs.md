# ExecutedRoutedEventArgs Class

## Definition
Namespace: `Cerneala.UI.Input`  
Assembly/Project: `Cerneala`  
Source: `UI/Input/ExecutedRoutedEventArgs.cs`

Provides routed event data for command execution events.

```csharp
public sealed class ExecutedRoutedEventArgs : RoutedEventArgs
```

Inheritance:
`object` -> `RoutedEventArgs` -> `ExecutedRoutedEventArgs`

## Examples

The following example creates executed command event data and passes it to a command binding. The binding receives only matching command instances.

```csharp
using Cerneala.UI.Input;

RoutedCommand saveCommand = new("Save", typeof(ExecutedRoutedEventArgs));
object? receivedParameter = null;

CommandBinding binding = new(
    saveCommand,
    (_, args) => receivedParameter = ((ExecutedRoutedEventArgs)args).Parameter);

ExecutedRoutedEventArgs args = new(
    CommandEvents.ExecutedEvent,
    new UiElementId("editor"),
    saveCommand,
    "document.cerneala");

binding.OnExecuted(new UiElementId("editor"), args);
```

## Remarks

`ExecutedRoutedEventArgs` carries the `ICommand` being executed and the optional command parameter through the retained command routing system.

`CommandRouter.Execute` creates this type for both `CommandEvents.PreviewExecutedEvent` and `CommandEvents.ExecutedEvent`. Preview execution routes from the root toward the target with tunneling semantics. If the preview event is not handled, the executed event routes from the target toward the root with bubbling semantics.

Command bindings compare `CommandBinding.Command` with `ExecutedRoutedEventArgs.Command` by reference. A binding ignores execution arguments for a different command instance.

Set the inherited `Handled` property to stop further command binding invocation along the route.

## Constructors

| Name | Description |
| --- | --- |
| `ExecutedRoutedEventArgs(RoutedEvent routedEvent, object originalSource, ICommand command, object? parameter)` | Initializes a new instance for the specified routed event, original source, command, and optional command parameter. Throws `ArgumentNullException` when `routedEvent`, `originalSource`, or `command` is `null`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Command` | `ICommand` | Gets the command associated with the execution event. |
| `Parameter` | `object?` | Gets the optional command parameter associated with the execution event. |
| `RoutedEvent` | `RoutedEvent` | Gets the routed event associated with these arguments. Inherited from `RoutedEventArgs`. |
| `OriginalSource` | `object` | Gets the original source passed to the routed event arguments. Inherited from `RoutedEventArgs`. |
| `Source` | `object` | Gets or sets the current source while routing. Inherited from `RoutedEventArgs`. |
| `Handled` | `bool` | Gets or sets whether routing should stop processing the event. Inherited from `RoutedEventArgs`. |

## Routed Event Usage

| Event | Routing strategy | Args type |
| --- | --- | --- |
| `CommandEvents.PreviewExecutedEvent` | `Tunnel` | `ExecutedRoutedEventArgs` |
| `CommandEvents.ExecutedEvent` | `Bubble` | `ExecutedRoutedEventArgs` |

## Applies to

Cerneala retained UI command routing.

## See also

- `CommandEvents`
- `CommandRouter`
- `CommandBinding`
- `RoutedCommand`
- `RoutedEventArgs`
