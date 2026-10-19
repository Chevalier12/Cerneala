# IInputPressable Interface

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/IInputPressable.cs`

Marks an element whose pressed state the input system sets while a pointer button or the Space key holds it down.

```csharp
public interface IInputPressable
```

## Examples

`Button` implements this interface through `ButtonBase.IsPressed`. The root is 200 by 200, so the 80 by 30 button covers 60..140 and 85..115.

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;

UIRoot root = new(200, 200);
Button button = new() { Width = 80, Height = 30 };
root.VisualChildren.Add(button);
UiHost host = new(new UiHostOptions { Root = root, Viewport = new UiViewport(200, 200) });

PointerSnapshot up = PointerSnapshot.Empty.WithPosition(100, 100);
PointerSnapshot down = up.WithButton(InputMouseButton.Left, true);
host.Update(Frame(PointerSnapshot.Empty, up));
host.Update(Frame(up, down));
Console.WriteLine(button.IsPressed); // True
host.Update(Frame(down, up));
Console.WriteLine(button.IsPressed); // False

static InputFrame Frame(PointerSnapshot previous, PointerSnapshot current) =>
    new(previous, current, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
```

## Remarks

The input system writes `IsPressed`; the element does not set it by itself.

- **Pointer.** When the left mouse button goes down, `PressedStateTracker` finds the nearest `IInputPressable` on the route from the pointer target to the root and sets its `IsPressed` to `true`. The left-button release, or a left press over an element with no pressable on its route, sets it back to `false`. As a WPF `ButtonBase` sets `IsPressed` on `MouseLeftButtonDown` only, the right, middle, and extra buttons do not change `IsPressed`: a right click on a `Button` leaves it `false`, and a right click while the left button holds a `Button` leaves it `true`. The search stops at an element that takes its own press without being a pressable: the field of a non-editable `ComboBox`, or an item container of a `Selector`, such as a `ListBoxItem`, `ComboBoxItem`, or `TabItem`. A press there presses nothing: when a `ListBox` is the `Content` of a `Button`, holding the mouse on a `ListBoxItem` leaves the `Button`'s `IsPressed` at `false`.
- **Space.** When Space goes down and the key event is not handled, the focused element gets `IsPressed = true` if it implements both `IInputCommandSource` and `IInputPressable`. An ancestor of the focused element is not pressed: when a `ComboBox` inside a `Button` has keyboard focus, Space leaves the `Button`'s `IsPressed` at `false`. It goes back to `false` when Space is released, when no element has keyboard focus, or when the element is no longer a valid input target. When keyboard focus moves to another element while Space is held, the Space press is forgotten: `IsPressed` goes back to `false` at once, and the later Space release clicks nothing, also when focus has come back to the element before the release. The same holds when focus moves in the input frame of the Space press itself, for example because Tab goes down in the same frame or an unhandled `KeyDown` handler moves focus: the press does not start, so `IsPressed` stays `false`. If the pointer still holds the same element down at that moment, `IsPressed` stays `true` until the pointer releases it.
- **Aspect.** `AspectState.Pressed` is active for an element that implements `IInputPressable` while `IsPressed` is `true`. Aspect rules use it for pressed visuals.

`ButtonBase` is the only built-in implementation, so `Button`, `RepeatButton`, and `ToggleButton` all have it.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `IsPressed` | `bool` | Gets or sets whether the element is pressed. Set by the input system. |

## Applies to

`ButtonBase` and its derived controls.

## See also

- `Cerneala.UI.Input.PressedStateTracker`
- `Cerneala.UI.Input.IInputCommandSource`
- `Cerneala.UI.Controls.Primitives.ButtonBase`
