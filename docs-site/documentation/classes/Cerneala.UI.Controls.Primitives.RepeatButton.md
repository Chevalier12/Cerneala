# RepeatButton Class

## Definition
Namespace: `Cerneala.UI.Controls.Primitives`

Assembly/Project: `Cerneala`
Source: `UI/Controls/Primitives/RepeatButton.cs`

Represents a button that activates immediately on a valid left-pointer press and repeats activation while the press remains valid.

```csharp
public class RepeatButton : Button
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Control` -> `ContentControl` -> `ButtonBase` -> `Button` -> `RepeatButton`

## Examples

Create a repeat button in C#:

```csharp
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Input;

int value = 0;

RepeatButton incrementButton = new()
{
    Content = "+",
    Delay = 400,
    Interval = 75,
    Command = new ActionCommand(_ => value++)
};
```

Create the same control in markup:

```xml
<RepeatButton Content="+" Delay="400" Interval="75" />
```

## Remarks

`RepeatButton` uses the inherited `Button` content, layout, fallback rendering, focus, pressed-state, routed event, and command behavior. It does not create a thread, timer, or delayed task. Repetition advances from the `TimeSpan` delta supplied to `ElementInputBridge.Dispatch(UIRoot, InputFrame, TimeSpan)` by the host.

A valid left-pointer press raises the inherited `Click` event immediately and then executes the inherited command through the existing `IInputCommandSource` and `CommandRouter` path. The first repeat occurs after `Delay`. Later repeats use `Interval`, with at most one activation per input frame. A late frame skips missed intervals instead of executing a catch-up burst.

Releasing the pointer ends the repeat session before time is advanced for that frame, so release wins when it occurs exactly at a repeat deadline. Pointer release does not add a final click or command execution. Repetition is also canceled when the pointer leaves the control's hit target, the control becomes disabled or non-visible, the control is detached, its root changes, or its input route becomes invalid.

Changing `Delay` during a session affects the next session. Changing `Interval` affects the next interval calculated after a repeat. `Delay = 0` permits the first repeat on the next input frame but never duplicates the immediate activation in the press frame.

The current MVP repeats only left-pointer activation. Inherited keyboard activation remains one-shot: Space activates on release, and keyboard input does not start a repeat session.

## Constructors

| Name | Description |
| --- | --- |
| `RepeatButton()` | Initializes a repeat button with `Delay` set to `500` milliseconds and `Interval` set to `100` milliseconds. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `DelayProperty` | `UiProperty<int>` | Identifies the `Delay` UI property. The default is `500`; values must be greater than or equal to zero. |
| `IntervalProperty` | `UiProperty<int>` | Identifies the `Interval` UI property. The default is `100`; values must be greater than zero. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Delay` | `int` | Gets or sets the delay, in milliseconds, before the first repeat. The value must be non-negative. |
| `Interval` | `int` | Gets or sets the interval, in milliseconds, between later repeats. The value must be positive. |

## Important Inherited Members

| Name | Declared by | Description |
| --- | --- | --- |
| `Click` | `ButtonBase` | Occurs for the immediate activation and each repeat activation. |
| `Command` | `ButtonBase` | Gets or sets the command evaluated and executed for each activation. |
| `CommandParameter` | `ButtonBase` | Gets or sets the parameter passed to the command. |
| `Content` | `ContentControl` | Gets or sets the content displayed by the button. |
| `IsPressed` | `ButtonBase` | Gets or sets the pressed visual and input state. |

## Property Information

| Property | Identifier Field | Default Value | Validation | Markup Constraint |
| --- | --- | --- | --- | --- |
| `Delay` | `DelayProperty` | `500` | `value >= 0` | `NonNegative` |
| `Interval` | `IntervalProperty` | `100` | `value > 0` | `Positive` |

## Applies to

Project: `Cerneala`

## See also

- `Cerneala.UI.Controls.Button`
- `Cerneala.UI.Controls.Primitives.ButtonBase`
- `Cerneala.UI.Input.ElementInputBridge`
- `Cerneala.UI.Input.IInputActivatable`
