# KeyEventArgs Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/KeyEventArgs.cs`

Provides routed keyboard event data, including the key that changed, the keyboard modifier state captured for that event, and whether the event is an operating-system key auto-repeat.

```csharp
public sealed class KeyEventArgs : RoutedEventArgs
```

Inheritance:
`object` -> `RoutedEventArgs` -> `KeyEventArgs`

## Examples

Handle a routed key event and mark it as handled. `UIElement.KeyDown` is a `RoutedEventHandler<KeyEventArgs>`, so `args` is already a `KeyEventArgs` and needs no cast.

```csharp
target.KeyDown += (_, args) =>
{
    if (args.Key == InputKey.Enter && args.IsControlDown)
    {
        args.Handled = true;
    }
};
```

React only to the first press of a held key and ignore the auto-repeated `KeyDown` events that follow while the key stays down.

```csharp
target.KeyDown += (_, args) =>
{
    if (args is { Key: InputKey.F5, IsRepeat: false })
    {
        args.Handled = true;
    }
};
```

Holding F5 on the SDL3 desktop backend raises one `KeyDown` with `IsRepeat == false`, then one `KeyDown` with `IsRepeat == true` for each operating-system auto-repeat. The handler above handles only the first one.

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

`IsRepeat` follows the WPF `KeyEventArgs.IsRepeat` contract. While a key is held, the operating system sends auto-repeat key-down messages. For each one, the focus manager raises one more `PreviewKeyDownEvent` and `KeyDownEvent` pair for that key with `IsRepeat` set to `true`. The first press of the key has `IsRepeat` set to `false`. Key-up events always have `IsRepeat` set to `false`. A repeat does not raise a key-up event; the key stays down until it is released. Repeats reach the focus manager through `InputFrame.KeyboardFrame.GetRepeatCount`; the SDL3 desktop backend fills them from SDL key-down events that SDL marks as repeats. An input source that does not supply repeat events produces no repeated key events.

Repeated key-down events are raised as routed events only. Command input bindings, Tab navigation, and Enter or Space activation react to the first press of a key and do not run again for its repeats.

Because `KeyEventArgs` derives from `RoutedEventArgs`, handlers can use inherited routing state such as `RoutedEvent`, `OriginalSource`, `Source`, and `Handled`. Setting `Handled` participates in the routed input flow used by the input system.

Constructors require non-null `routedEvent` and `originalSource` values through the `RoutedEventArgs` base constructor.

## Constructors

| Name | Description |
| --- | --- |
| `KeyEventArgs(RoutedEvent routedEvent, object originalSource, InputKey key)` | Initializes a keyboard event with no Control, Shift, or Alt modifier flags set. `IsRepeat` is `false`. |
| `KeyEventArgs(RoutedEvent routedEvent, object originalSource, InputKey key, bool isControlDown, bool isShiftDown, bool isAltDown)` | Initializes a keyboard event with an explicit key and modifier state. `IsRepeat` is `false`. |
| `KeyEventArgs(RoutedEvent routedEvent, object originalSource, InputKey key, bool isControlDown, bool isShiftDown, bool isAltDown, bool isRepeat)` | Initializes a keyboard event with an explicit key, modifier state, and repeat flag. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Key` | `InputKey` | Gets the key associated with the event. |
| `IsControlDown` | `bool` | Gets whether either Control modifier was down when the event data was created. |
| `IsShiftDown` | `bool` | Gets whether either Shift modifier was down when the event data was created. |
| `IsAltDown` | `bool` | Gets whether either Alt modifier was down when the event data was created. |
| `IsRepeat` | `bool` | Gets whether the key-down event is an auto-repeat of a key that is already held down. It is `false` for the first press and for key-up events. |

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
- `InputFrame.KeyboardFrame`
- `InputKey`
- `RoutedEventArgs`
