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

The control exposes `IsPressed` as a UI property. The input system can set this property through `IInputPressable`; for example, pressing a child element inside the button can set the ancestor button's pressed state.

Command execution supports both direct `ICommand` instances and `RoutedCommand` instances. Direct commands call `CanExecute(CommandParameter)` before `Execute(CommandParameter)`. Routed commands are evaluated and executed through `CommandRouter` with a `RoutedCommandContext` that includes the command, the button, the input route map, and `CommandParameter`.

`ExecuteCommand` returns `false` without executing when the button is disabled, has no command, or the command cannot execute. `RefreshCommandState` synchronizes `IsEnabled` with command availability. A button with no command is considered enabled by command-state refresh; a button with a command is enabled only when that command can execute.

When attached, `ButtonBase` subscribes to `IObservableCommand.CanExecuteChanged`. Notifications raised on the UI thread refresh synchronously; off-thread bursts are coalesced into one pending Relay callback per attached button. The callback re-queries the current command on the UI thread. Replacing the command or detaching the button unsubscribes the old source and makes already queued callbacks harmless.

The protected `ShouldClickOnMouseUp` property defaults to `true`, preserving release activation for normal buttons. Derived controls can override it when pointer activation occurs earlier; `RepeatButton` returns `false` so release does not add a final click.

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
| `ShouldClickOnMouseUp` | `bool` | Gets whether a valid left mouse-up raises `Click`. The default is `true`. |

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
