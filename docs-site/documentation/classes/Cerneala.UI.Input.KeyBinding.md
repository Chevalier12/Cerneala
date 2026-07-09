# KeyBinding Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/KeyBinding.cs`

Represents an input binding that executes an `ICommand` when a keyboard `KeyGesture` matches the current input frame.

```csharp
public sealed class KeyBinding : InputBinding
```

Inheritance:
`object` -> `InputBinding` -> `KeyBinding`

## Examples
The following example attaches a `Ctrl+S` key binding to a `UIElement`.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

UIElement editor = new() { Focusable = true };

editor.InputBindings.Add(new KeyBinding(
    new ActionCommand(parameter => Save((string?)parameter)),
    InputKey.S,
    KeyModifiers.Control,
    "file"));

static void Save(string? target)
{
    // Persist the target document.
}
```

## Remarks
`KeyBinding` is the keyboard-specific `InputBinding` implementation. It stores a strongly typed `KeyGesture` in the `KeyGesture` property while also passing the same gesture to the inherited `Gesture` property.

The constructor overload that accepts `InputKey` creates a `KeyGesture` internally. Because `KeyGesture` requires a concrete key, `InputKey.None` and `InputKey.Unknown` are invalid for that overload.

Gesture matching is delegated to `KeyGesture.Matches`. Modifier matching is exact for `Shift`, `Control`, and `Alt`: extra pressed modifiers cause the gesture not to match. Once a match is found, inherited `InputBinding` execution checks `Command.CanExecute(CommandParameter)` before calling `Command.Execute(CommandParameter)`.

When the command is a `RoutedCommand`, the inherited routed `TryExecute` overload executes it through `CommandRouter` with the supplied target and route map. In retained input processing, key bindings can be attached through `UIElement.InputBindings`; tests cover execution on the focused element and matching ancestor bindings.

## Constructors
| Name | Description |
| --- | --- |
| `KeyBinding(ICommand command, InputKey key, KeyModifiers modifiers = KeyModifiers.None, object? commandParameter = null)` | Initializes a binding by creating a `KeyGesture` from `key` and `modifiers`, then stores `commandParameter` for command execution. |
| `KeyBinding(ICommand command, KeyGesture gesture, object? commandParameter = null)` | Initializes a binding with an existing `KeyGesture` and stores it in `KeyGesture`. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `KeyGesture` | `KeyGesture` | Gets the keyboard gesture associated with this binding. |
| `Command` | `ICommand` | Gets the inherited command executed by the binding. |
| `Gesture` | `InputGesture` | Gets the inherited gesture used for matching input frames. For `KeyBinding`, this is the same gesture supplied through `KeyGesture`. |
| `CommandParameter` | `object?` | Gets the inherited parameter passed to `CanExecute` and `Execute`. |

## Methods
| Name | Return Type | Description |
| --- | --- | --- |
| `Matches(InputFrame frame)` | `bool` | Inherited from `InputBinding`; returns whether the binding gesture matches the supplied frame. |
| `TryExecute(InputFrame frame)` | `bool` | Inherited from `InputBinding`; executes the command when the gesture matches and the command can execute. |
| `TryExecute(InputFrame frame, CommandRouter router, ElementInputRouteMap routeMap, UIElement target)` | `bool` | Inherited from `InputBinding`; routes `RoutedCommand` instances through `CommandRouter`, otherwise uses direct command execution. |

## Exceptions
| Member | Exception | Condition |
| --- | --- | --- |
| Constructors | `ArgumentNullException` | `command` is `null`; `gesture` is `null` for the `KeyGesture` overload. |
| `KeyBinding(ICommand, InputKey, KeyModifiers, object?)` | `ArgumentException` | `key` is `InputKey.None` or `InputKey.Unknown`. |

## Applies to
`Cerneala` retained UI input and command routing.

## See also
- `InputBinding`
- `KeyGesture`
- `InputBindingCollection`
- `UIElement.InputBindings`
