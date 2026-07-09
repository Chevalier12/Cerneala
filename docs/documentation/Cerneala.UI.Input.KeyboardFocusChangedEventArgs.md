# KeyboardFocusChangedEventArgs Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/KeyboardFocusChangedEventArgs.cs`

Provides routed event data for keyboard focus transitions, including the previous and new focus targets.

```csharp
public sealed class KeyboardFocusChangedEventArgs : RoutedEventArgs
```

Inheritance:
`object` -> `RoutedEventArgs` -> `KeyboardFocusChangedEventArgs`

## Examples
The focus events are registered on `InputEvents` and delivered through the routed event system. Handlers receive `RoutedEventArgs`, so cast to `KeyboardFocusChangedEventArgs` when handling keyboard focus events.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

void AttachFocusHandler(UIElement element)
{
    element.Handlers.AddHandler(InputEvents.GotKeyboardFocusEvent, (_, args) =>
    {
        if (args is KeyboardFocusChangedEventArgs focusArgs)
        {
            object? oldFocus = focusArgs.OldFocus;
            object? newFocus = focusArgs.NewFocus;

            // Use oldFocus and newFocus to update local focus-dependent state.
        }
    });
}
```

## Remarks
`KeyboardFocusChangedEventArgs` is used by the keyboard focus routed events declared in `InputEvents`: `PreviewGotKeyboardFocusEvent`, `GotKeyboardFocusEvent`, `PreviewLostKeyboardFocusEvent`, and `LostKeyboardFocusEvent`.

`FocusManager` creates this event data when focus changes. The preview events use `RoutingStrategy.Tunnel`, and the non-preview keyboard focus events use `RoutingStrategy.Bubble`. If a preview keyboard focus event is marked as handled, `FocusManager` does not raise the corresponding non-preview keyboard focus event. The broader `GotFocusEvent` and `LostFocusEvent` are still raised with plain `RoutedEventArgs`.

`OldFocus` and `NewFocus` are nullable because a focus change can move from no focused element to an element, from an element to no focused element, or between two elements. The constructor accepts `object?` for both values; the built-in `FocusManager` passes `UIElement` instances or `null`.

Because this class derives from `RoutedEventArgs`, it also carries the routed event identity, original source, current source, and handled state used by the routed event router.

## Constructors
| Name | Description |
| --- | --- |
| `KeyboardFocusChangedEventArgs(RoutedEvent routedEvent, object originalSource, object? oldFocus, object? newFocus)` | Initializes a new instance for the specified routed event, source, previous focus target, and new focus target. `routedEvent` and `originalSource` are passed to `RoutedEventArgs` and cannot be `null`. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `OldFocus` | `object?` | Gets the focus target before the keyboard focus change, or `null` when there was no previous focus target. |
| `NewFocus` | `object?` | Gets the focus target after the keyboard focus change, or `null` when focus was cleared. |

## Inherited Properties
| Name | Type | Description |
| --- | --- | --- |
| `RoutedEvent` | `RoutedEvent` | Gets the routed event associated with this event data. |
| `OriginalSource` | `object` | Gets the original source supplied when the event data was created. |
| `Source` | `object` | Gets or sets the current source while the event is routed. |
| `Handled` | `bool` | Gets or sets whether event routing should stop. |

## Related Routed Events
| Event | Routing strategy | Event args type |
| --- | --- | --- |
| `InputEvents.PreviewGotKeyboardFocusEvent` | `Tunnel` | `KeyboardFocusChangedEventArgs` |
| `InputEvents.GotKeyboardFocusEvent` | `Bubble` | `KeyboardFocusChangedEventArgs` |
| `InputEvents.PreviewLostKeyboardFocusEvent` | `Tunnel` | `KeyboardFocusChangedEventArgs` |
| `InputEvents.LostKeyboardFocusEvent` | `Bubble` | `KeyboardFocusChangedEventArgs` |

## Applies To
Cerneala retained UI input system.

## See Also
- `FocusManager`
- `InputEvents`
- `RoutedEventArgs`
- `RoutedEventRouter`
