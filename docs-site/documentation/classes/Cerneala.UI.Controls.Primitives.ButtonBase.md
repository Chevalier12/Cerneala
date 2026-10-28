# ButtonBase Class

## Definition
Namespace: `Cerneala.UI.Controls.Primitives`  
Assembly/Project: `Cerneala`  
Source: `UI/Controls/Primitives/ButtonBase.cs`

Provides the shared content, pressed-state, focus, cursor, and command behavior for button-like controls.

```csharp
public class ButtonBase : ContentControl, IInputPressable, IInputCommandSource, ICommandStateSource, IInputActivatable
```

Inheritance:  
`object` -> `UiObject` -> `UIElement` -> `Control` -> `ContentControl` -> `ButtonBase`

Derived:  
`Button`, `RepeatButton`, `ToggleButton`

Implements:  
`IInputPressable`, `IInputCommandSource`, `ICommandStateSource`, `IInputActivatable`

## Examples

```csharp
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Input;

ButtonBase button = new()
{
    Content = "Save",
    CommandParameter = "document"
};

button.Command = new ActionCommand(parameter =>
{
    Save((string?)parameter);
});
```

## Remarks

`ButtonBase` is the primitive base class for controls that behave like buttons. It inherits content support from `ContentControl`, enables focus and tab navigation in its constructor, and sets the default cursor to `Cursor.Hand`.

The control exposes `IsPressed` as a UI property. The input system can set this property through `IInputPressable`; for example, pressing a child element inside the button, such as a `TextBlock` or `Border` in its `Content`, sets the ancestor button's pressed state. A press that a nearer element takes for itself does not press the button, as in WPF, where that element handles `MouseLeftButtonDown` before the button sees it: a nearer `ButtonBase`, the field of a non-editable `ComboBox`, or an item container of a `Selector`, such as a `ListBoxItem`, `ComboBoxItem`, or `TabItem`. For example, when the `Content` of a `Button` is a `ListBox`, holding the left mouse button on a `ListBoxItem` leaves the `Button`'s `IsPressed` at `false`, and the release selects the item without clicking the `Button`; holding it on the `Button` outside the `ListBox` sets `IsPressed` to `true` until the release. A release on the `Button` after a press on such an item does not click the `Button` either, and a `RepeatButton` does not repeat for such a press. An enabled `Thumb` in the button's content also takes a left press for itself, as a WPF `Thumb` handles `MouseLeftButtonDown` and captures the mouse to start its drag: the button's `IsPressed` stays `false` while the mouse is held on the thumb and during its drag, and the release raises no `Click` of the button and runs no `Command`. For example, with `Button button = new() { Content = new Thumb { Width = 40, Height = 20 }, Width = 200, Height = 80 };`, pressing the center of the thumb and dragging it 30 pixels raises `DragDelta` and leaves `button.IsPressed` at `false`; pressing `button` outside the thumb sets `IsPressed` to `true` until the release, and that click raises one `Click`. The same holds for a `Thumb` in the template of a control in the content, such as a `ScrollBar` or a `Slider`, and for an enabled `ColorSpectrum`, which captures the mouse to pick a color.

Command execution supports both direct `ICommand` instances and `RoutedCommand` instances. Direct commands call `CanExecute(CommandParameter)` before `Execute(CommandParameter)`. Routed commands are evaluated and executed through `CommandRouter` with a `RoutedCommandContext` that includes the command, the button, the input route map, and `CommandParameter`.

`ExecuteCommand` returns `false` without executing when the button is disabled, has no command, or the command cannot execute. `RefreshCommandState` synchronizes `IsEnabled` with command availability. A button with no command is considered enabled by command-state refresh; a button with a command is enabled only when that command can execute. So `button.Command = null` enables a button that its old command disabled, in the next frame; an `IsEnabled = false` that the app wrote itself stays, as `ICommandStateSource` describes. The one exception is a button with a `RoutedCommand` that takes no part in input, described in the next paragraph: its refresh keeps the current `IsEnabled` until the button takes part in input again.

A `RoutedCommand` finds its `CommandBinding` along the input route, so its refresh needs the button in the command-state route. A refresh while the button takes no part in input (it is collapsed, `Hidden`, has `IsVisible` set to `false`, is under such an ancestor, or is skipped by virtualization) leaves `IsEnabled` unchanged and runs again after the next change of the element tree or of the input route. For example, a Save button that is collapsed when it gets a `RoutedCommand` whose binding sets `CanExecute = true` has `IsEnabled == true` in the first frame processed after it becomes visible; with a binding that sets `CanExecute = false` it has `IsEnabled == false` in that frame. A direct `ICommand` does not need the route and is refreshed while the button is collapsed too.

When attached, `ButtonBase` subscribes to `IObservableCommand.CanExecuteChanged`. A notification raised on the UI thread queues the button for command-state refresh; the actual `CanExecute` re-query and `IsEnabled` update run later, in the frame's command-state phase. Off-thread bursts are coalesced into one pending Relay callback per attached button; that callback runs on the UI thread and only queues the same refresh. Replacing the command or detaching the button unsubscribes the old source and makes already queued callbacks harmless.

The protected `ShouldClickOnMouseUp` property defaults to `true`, preserving release activation for normal buttons. Derived controls can override it when pointer activation occurs earlier; `RepeatButton` returns `false` so release does not add a final click.

A left mouse-up raises the button's own `Click` only when the click belongs to this button: the nearest `IInputCommandSource` on the input route from the mouse-up's `OriginalSource` is this button. This is the same element whose `Command` the input system runs for the click. A click on a command source in the button's content, such as a `Button` or the `PART_DropDownToggle` of a non-editable `ComboBox`, raises that control's `Click` and not one of the outer button. The inner `Click` still bubbles, so handlers on the outer button's `Click` receive it with the inner control as `OriginalSource`. For example, with `Button outer = new() { Content = new Button { Content = "inner" } };`, one click on `inner` raises one `Click` whose `OriginalSource` is `inner`, and `outer.Command` does not run; one click on `outer` outside `inner` raises one `Click` whose `OriginalSource` is `outer` and runs `outer.Command`. The button decides with the input route the input system resolved for the click before any handler ran, so the result does not change when a handler changes the tree: when `inner`'s `Click` handler removes `inner`, collapses the panel that holds it, or removes `outer` from its parent, the click still raises no `Click` whose `OriginalSource` is `outer`, and `outer.Command` does not run. A left mouse-up that an element in the button's content marks handled raises no `Click` of the button, and the input system runs no `Command` for it. For example, a `Selector` item marks the release that selects it handled, so one click on an item of a `ListBox` that is the button's `Content` selects that item and does not click the button (see `Selector`). An enabled `TextBox` or `PasswordBox` in the button's content owns a left click into it, as WPF's text editor handles `MouseLeftButtonDown` and `MouseLeftButtonUp`: the press places its caret and focuses it, the input system does not set the button's `IsPressed`, and the text box marks the release handled, so the button raises no `Click` and its `Command` does not run. For example, with `Button button = new() { Content = new TextBox { Width = 120, Height = 30 }, Width = 200, Height = 80 };`, one click at the center of the text box raises no `Click` of `button`, and one click on `button` outside the text box raises one `Click` and runs `button.Command`. A press on the button outside the text box followed by a release over the text box still raises one `Click` of the button.

A left press whose `PreviewMouseDown` or `PreviewMouseLeftButtonDown` a handler marks handled does not reach the button, as in WPF, where a handled preview keeps `MouseLeftButtonDown` from `ButtonBase`: when the press events end, `IsPressed` is `false`, and the release raises no `Click` and the input system runs no `Command`. This holds when the handler is on the button and when it is on an element in the button's content. For example, with `button.PreviewMouseDown += (_, args) => args.Handled = true;`, one click at the center of `button` leaves `IsPressed` at `false` after the press, raises no `Click`, and does not run `button.Command`; without that handler the same click raises one `Click` and runs `button.Command` once. Handlers of the press events themselves, such as a `MouseDown` handler added with `AddHandler(MouseDownEvent, handler, handledEventsToo: true)`, still see `IsPressed` as `true`, because the input system sets it before it raises those events.

A left press whose bubbling `MouseDown` or `MouseLeftButtonDown` an element in the button's content marks handled does not press the button either, as in WPF, where `ButtonBase` presses itself in its `MouseLeftButtonDown` class handler, which does not run for an event that is already handled when it reaches the button: when the press events end, `IsPressed` is `false`, the release raises no `Click` and the input system runs no `Command`, a `RepeatButton` does not repeat, and a `ToggleButton` or `CheckBox` keeps its `IsChecked` value. For example, with `Border border = new() { Width = 120, Height = 30, Background = new SolidColorBrush(Color.White) };`, `Button button = new() { Content = border, Width = 200, Height = 80 };` and `border.MouseLeftButtonDown += (_, args) => args.Handled = true;`, holding the left mouse button at the center of `border` leaves `button.IsPressed` at `false`, and the release raises no `Click` and does not run `button.Command`; without that handler the same click sets `IsPressed` to `true` until the release, raises one `Click` and runs `button.Command` once. Only handlers on elements between the pressed element and the button count. A `MouseDown` or `MouseLeftButtonDown` handler on the button itself or on one of its ancestors runs after the button took the press, as WPF's class handler runs before the button's instance handlers and before the event bubbles further, so marking the event handled there still presses and clicks the button once.

A left press on the button or its content focuses the button, as WPF's `ButtonBase` calls `Focus()` in the `MouseLeftButtonDown` class handler that presses it. The input system moves the focus after the press events, so `MouseDown` and `MouseLeftButtonDown` handlers still see the focus where it was. The two cases above that keep the press from the button keep the focus off it too: a handled `PreviewMouseDown` or `PreviewMouseLeftButtonDown`, and a bubbling `MouseDown` or `MouseLeftButtonDown` that an element in the button's content handled. In both cases the element that had keyboard focus keeps it. For example, with `border` as above and `border.MouseDown += (_, args) => args.Handled = true;`, a left press and release at the center of `border` while another `Button` named `other` has keyboard focus leave `button.IsKeyboardFocused` at `false` and `other.IsKeyboardFocused` at `true`, and `button` raises no `Click`. Without that handler, the same press sets `button.IsKeyboardFocused` to `true`. A handler of a press event that moves the focus somewhere else itself keeps its choice, and the button does not take the focus back. The `ElementInputBridge` page describes the complete rule.

Space and the left mouse button press the same button as one gesture with one `Click`, as WPF's `ButtonBase` does with its `IsSpaceKeyDown` state. While Space holds the focused button pressed, a left mouse press and release on it neither clears `IsPressed` nor clicks: `IsPressed` stays `true`, the release raises no `Click` and runs no `Command`, and the Space release then raises the one `Click`. This also holds when an element in the button's content marks the press's `MouseDown` or `PreviewMouseDown` handled. When Space is released while the left mouse button still holds the button, the button stays pressed and the Space release raises no `Click`; the mouse release raises it. Space on a button that the left mouse button already holds starts no Space press, so the mouse release raises the one `Click` and the later Space release raises none. For example, with a focused `button`: Space down, mouse down, mouse up on `button` leaves `IsPressed` at `true` and raises no `Click`; the Space release then sets `IsPressed` to `false` and raises one `Click`.

## Constructors

| Name | Description |
| --- | --- |
| `ButtonBase()` | Initializes a focusable, tab-stop button primitive with a hand cursor. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `IsPressedProperty` | `UiProperty<bool>` | Identifies the `IsPressed` UI property. Default value is `false`; metadata affects render, input visuals, and aspects. |
| `CommandProperty` | `UiProperty<ICommand?>` | Identifies the `Command` UI property. Default value is `null`; metadata affects input visuals. |
| `CommandParameterProperty` | `UiProperty<object?>` | Identifies the `CommandParameter` UI property. Default value is `null`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `IsPressed` | `bool` | Gets or sets whether the button is currently in a pressed state. |
| `Command` | `ICommand?` | Gets or sets the command executed by button activation. |
| `CommandParameter` | `object?` | Gets or sets the parameter passed to command `CanExecute` and `Execute` calls. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `CanExecuteCommand(CommandRouter router, ElementInputRouteMap routeMap)` | `bool` | Returns whether the current command can execute. Uses `CommandRouter` for routed commands and `ICommand.CanExecute` for direct commands. |
| `ExecuteCommand(CommandRouter router, ElementInputRouteMap routeMap)` | `bool` | Executes the current command when the button is enabled and the command can execute. Returns whether execution happened. |
| `RefreshCommandState(CommandRouter router, ElementInputRouteMap routeMap)` | `bool` | Updates `IsEnabled` from command availability and returns whether the enabled state changed. |

## Protected Methods

| Name | Description |
| --- | --- |
| `OnAttached()` | Subscribes to observable command changes and queues a command-state refresh. |
| `OnDetached()` | Unsubscribes from observable command changes before detaching. |
| `OnPropertyChanged(UiPropertyChangedEventArgs args)` | Queues command-state refreshes when `Command` or `CommandParameter` changes, and updates observable-command subscriptions when `Command` changes. |

## Protected Properties

| Name | Type | Description |
| --- | --- | --- |
| `ShouldClickOnMouseUp` | `bool` | Gets whether a valid left mouse-up that belongs to this button raises `Click`. The default is `true`. |

## Property Information

| Property | Identifier Field | Default Value | Metadata/Options |
| --- | --- | --- | --- |
| `IsPressed` | `IsPressedProperty` | `false` | `AffectsRender`, `AffectsInputVisual`, `AffectsAspect` |
| `Command` | `CommandProperty` | `null` | `AffectsInputVisual` |
| `CommandParameter` | `CommandParameterProperty` | `null` | `None` |

## Applies to

Project: `Cerneala`

## See also

- `Cerneala.UI.Controls.Button`
- `Cerneala.UI.Controls.Primitives.RepeatButton`
- `Cerneala.UI.Controls.ContentControl`
- `Cerneala.UI.Input.ICommand`
- `Cerneala.UI.Input.RoutedCommand`
