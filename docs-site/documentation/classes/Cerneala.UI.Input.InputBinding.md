# InputBinding Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/InputBinding.cs`

Associates an `ICommand` with an `InputGesture` and an optional command parameter.

```csharp
public class InputBinding
```

Inheritance:
`object` -> `InputBinding`

Derived:
`KeyBinding`

## Examples

The base `InputBinding` can be created with any concrete `InputGesture`, such as `KeyGesture`, then added to a `UIElement` through its `InputBindings` collection.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

UIElement editor = new() { Focusable = true };

editor.InputBindings.Add(
    new InputBinding(
        new ActionCommand(parameter => SaveDocument((string?)parameter)),
        new KeyGesture(InputKey.S, KeyModifiers.Control),
        "document-1"));
```

`KeyBinding` is the focused keyboard convenience type for the same pattern:

```csharp
editor.InputBindings.Add(
    new KeyBinding(
        new ActionCommand(_ => SaveDocument(null)),
        InputKey.Enter));
```

## Remarks

`InputBinding` is the command side of the retained input binding pipeline. It stores a command, a gesture, and an optional parameter. `Matches` delegates gesture matching to `Gesture.Matches(frame)`.

For direct commands, `TryExecute(InputFrame)` first requires the gesture to match, then calls `Command.CanExecute(CommandParameter)`. If the command can execute, it calls `Command.Execute(CommandParameter)` and returns `true`; otherwise it returns `false`.

For retained routed command execution, use the `TryExecute(InputFrame, CommandRouter, ElementInputRouteMap, UIElement)` overload. When `Command` is a `RoutedCommand`, that overload creates a `RoutedCommandContext` with the supplied target, route map, and `CommandParameter`, then delegates execution to `CommandRouter.Execute`. For non-routed commands, it falls back to the direct `TryExecute(InputFrame)` overload.

In the retained keyboard dispatch path, bindings are checked on the focused element and then its visual ancestors. The first binding that executes suppresses the remaining default keyboard activation for that dispatch result. Handled key events do not reach input binding execution.

The constructor rejects a `null` command or gesture. The routed overload also rejects a `null` router, route map, or target.

## Constructors

| Name | Description |
| --- | --- |
| `InputBinding(ICommand command, InputGesture gesture, object? commandParameter = null)` | Initializes a binding for `command` and `gesture`, storing `commandParameter` for command execution. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Command` | `ICommand` | Gets the command executed when the gesture matches and the command can execute. |
| `Gesture` | `InputGesture` | Gets the gesture used to test an `InputFrame`. |
| `CommandParameter` | `object?` | Gets the optional parameter passed to direct command execution or routed command context creation. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Matches(InputFrame frame)` | `bool` | Returns the result of `Gesture.Matches(frame)`. |
| `TryExecute(InputFrame frame)` | `bool` | Executes a direct command when the gesture matches and `Command.CanExecute(CommandParameter)` returns `true`. |
| `TryExecute(InputFrame frame, CommandRouter router, ElementInputRouteMap routeMap, UIElement target)` | `bool` | Executes a routed command through `CommandRouter` when appropriate, or falls back to direct execution for non-routed commands. |

## Applies to

Cerneala retained UI input and command routing.

## See also

- `KeyBinding`
- `InputGesture`
- `KeyGesture`
- `ICommand`
- `RoutedCommand`
- `CommandRouter`
- `InputBindingCollection`
