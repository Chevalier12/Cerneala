# KeyEventArgs Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/KeyEventArgs.cs`

Provides routed keyboard event data, including the key that changed and the keyboard modifier state captured for that event.

```csharp
public sealed class KeyEventArgs : RoutedEventArgs
```

Inheritance:
`object` -> `RoutedEventArgs` -> `KeyEventArgs`

## Examples

Handle a routed key event by casting the routed arguments to `KeyEventArgs` and marking the event as handled.

```csharp
target.Handlers.AddHandler(InputEvents.KeyDownEvent, (_, args) =>
{
    if (args is KeyEventArgs keyArgs &&
        keyArgs.Key == InputKey.Enter &&
        keyArgs.IsControlDown)
    {
        args.Handled = true;
    }
});
```

Create keyboard event data directly when raising or testing input behavior.

```csharp
object originalSource = new object();

KeyEventArgs args = new(
    InputEvents.KeyDownEvent,
    originalSource,
    InputKey.Enter,
    isControlDown: true,
    isShiftDown: false,
    isAltDown: false);
```

## Remarks

`KeyEventArgs` is used by the keyboard routed events registered on `InputEvents`: `PreviewKeyDownEvent`, `KeyDownEvent`, `PreviewKeyUpEvent`, and `KeyUpEvent`.

The class stores the triggering `InputKey` and immutable modifier flags for Control, Shift, and Alt. The modifier flags are captured when the instance is created; changing keyboard state later does not update an existing `KeyEventArgs` object.

Because `KeyEventArgs` derives from `RoutedEventArgs`, handlers can use inherited routing state such as `RoutedEvent`, `OriginalSource`, `Source`, and `Handled`. Setting `Handled` participates in the routed input flow used by the input system.

Constructors require non-null `routedEvent` and `originalSource` values through the `RoutedEventArgs` base constructor.

## Constructors

| Name | Description |
| --- | --- |
| `KeyEventArgs(RoutedEvent routedEvent, object originalSource, InputKey key)` | Initializes a keyboard event with no Control, Shift, or Alt modifier flags set. |
| `KeyEventArgs(RoutedEvent routedEvent, object originalSource, InputKey key, bool isControlDown, bool isShiftDown, bool isAltDown)` | Initializes a keyboard event with an explicit key and modifier state. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Key` | `InputKey` | Gets the key associated with the event. |
| `IsControlDown` | `bool` | Gets whether either Control modifier was down when the event data was created. |
| `IsShiftDown` | `bool` | Gets whether either Shift modifier was down when the event data was created. |
| `IsAltDown` | `bool` | Gets whether either Alt modifier was down when the event data was created. |

## Inherited Properties

| Name | Type | Description |
| --- | --- | --- |
| `RoutedEvent` | `RoutedEvent` | Gets the routed event represented by this event data. |
| `OriginalSource` | `object` | Gets the original source object supplied when the event data was created. |
| `Source` | `object` | Gets or sets the current routed source. It is initialized to `OriginalSource`. |
| `Handled` | `bool` | Gets or sets whether the routed event has been handled. |

## Related Routed Events

| Event Field | Routing Strategy | Description |
| --- | --- | --- |
| `InputEvents.PreviewKeyDownEvent` | `Tunnel` | Preview event for key-down input. |
| `InputEvents.KeyDownEvent` | `Bubble` | Bubble event for key-down input. |
| `InputEvents.PreviewKeyUpEvent` | `Tunnel` | Preview event for key-up input. |
| `InputEvents.KeyUpEvent` | `Bubble` | Bubble event for key-up input. |

## Applies to

`Cerneala` retained UI input routing.

## See also

- `InputEvents`
- `InputKey`
- `RoutedEventArgs`
