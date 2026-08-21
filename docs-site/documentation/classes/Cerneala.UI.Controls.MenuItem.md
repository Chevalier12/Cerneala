# MenuItem Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/MenuItem.cs`

Represents a command-capable item that can either activate as a leaf or host a nested submenu.

```csharp
public class MenuItem : ItemsControl, IInputCommandSource, ICommandStateSource, IInputActivatable
```

Inheritance:
`Object` -> `UiObject` -> `UIElement` -> `Control` -> `ItemsControl` -> `MenuItem`

Implements: `IInputCommandSource`, `ICommandStateSource`, `IInputActivatable`

## Examples

```csharp
using System;
using Cerneala.UI.Controls;
using Cerneala.UI.Input;

MenuItem file = new() { Header = "File" };
file.Items.Add(new MenuItem
{
    Header = "Open",
    Command = new ActionCommand(_ => Console.WriteLine("Open document")),
    CommandParameter = "Document.crn"
});
```

The parent item opens a submenu. Its leaf child raises `Click`, executes its command, and then closes the shared menu session when activated.

## Remarks

A `MenuItem` with children is a parent item: activation opens or closes its submenu and never executes `Command` in this version. A `MenuItem` without children is a leaf: valid activation raises the bubbling `Click` event once, executes its command through the retained command router once, and then closes the complete menu session. Disabled items do not activate.

Simple child data is wrapped in generated `MenuItem` containers. `DisplayMemberPath` determines each generated container's `Header`; an explicitly supplied `MenuItem` is used directly.

The default template projects `PART_ItemsPresenter` through `PART_SubmenuOverlay`. A top-level item in a `MenuBar` places that overlay below its header; deeper levels use `OverlayPlacement.AutoHorizontal`, prefer the right side, fall back to the left, and clamp to the current viewport. Every open overlay in one menu session shares the same light-dismiss domain.

`IsSubmenuOpen` can be set programmatically, but an item without children or an item that is disabled is coerced closed. Detach, root changes, removal from an active item source, loss of all children, and session shutdown also close stale branches. Replacing the component template closes the old overlay, removes its event handlers, clears its dismiss scope, and attaches the new parts without duplicate subscriptions.

### Keyboard interaction

| Key | Behavior |
| --- | --- |
| `Up` / `Down` | Moves between eligible items in a vertical menu without wrapping. |
| `Home` / `End` | Moves to the first or last eligible item at the current level. |
| `Right` | Opens a focused parent and moves focus to its first eligible child. |
| `Left` | Closes the current submenu level and restores focus to its parent item. |
| `Enter` / `Space` | Opens a parent or activates a leaf. |
| `Escape` | Closes the current level; at the root level it closes the full session and restores root focus. |
| `Tab` | Closes the full session and remains unhandled so normal focus navigation can continue. |

At the root of a `MenuBar`, `Left` and `Right` move between top-level items with wrapping, while `Down`, `Enter`, and `Space` open the active branch.

### Intentional limitations

This first version does not provide a dedicated separator, icon or shortcut-text slots, access-key mnemonics, checkable or radio menu items, `StaysOpenOnClick`, or a global `Alt` accelerator. `ContextMenu` is a separate follow-up control intended to reuse the same menu-session foundation.

## Constructors

| Name | Description |
| --- | --- |
| `MenuItem()` | Creates a focusable menu item with a vertical child panel and the default component template. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `ClickEvent` | `RoutedEvent` | Identifies the bubbling `Click` routed event. |
| `SubmenuOpenedEvent` | `RoutedEvent` | Identifies the bubbling `SubmenuOpened` routed event. |
| `SubmenuClosedEvent` | `RoutedEvent` | Identifies the bubbling `SubmenuClosed` routed event. |
| `HeaderProperty` | `UiProperty<object?>` | Identifies the `Header` UI property. |
| `CommandProperty` | `UiProperty<ICommand?>` | Identifies the `Command` UI property. |
| `CommandParameterProperty` | `UiProperty<object?>` | Identifies the `CommandParameter` UI property. |
| `IsSubmenuOpenProperty` | `UiProperty<bool>` | Identifies the `IsSubmenuOpen` UI property. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Header` | `object?` | Gets or sets the object displayed by `PART_HeaderPresenter`. The default is `null`. |
| `Command` | `ICommand?` | Gets or sets the command executed by an enabled leaf item. The default is `null`. |
| `CommandParameter` | `object?` | Gets or sets the value passed to `Command`. The default is `null`. |
| `IsSubmenuOpen` | `bool` | Gets or sets whether the effective submenu overlay is open. The default is `false`. |
| `Items` | `ItemCollection` | Gets the direct child-item collection inherited from `ItemsControl`. |
| `ItemsSource` | `IEnumerable?` | Gets or sets the child-item source inherited from `ItemsControl`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `CanExecuteCommand(CommandRouter, ElementInputRouteMap)` | `bool` | Returns whether the item is a leaf whose command can execute through the current input route. |
| `ExecuteCommand(CommandRouter, ElementInputRouteMap)` | `bool` | Executes the leaf command through direct or routed-command infrastructure and completes a pending leaf activation. |
| `RefreshCommandState(CommandRouter, ElementInputRouteMap)` | `bool` | Refreshes command state and synchronizes the effective enabled state. |

## Events

| Name | Type | Routing | Description |
| --- | --- | --- | --- |
| `Click` | `RoutedEventHandler` | Bubble | Occurs once when an enabled leaf item is activated, before command execution. |
| `SubmenuOpened` | `RoutedEventHandler` | Bubble | Occurs once when the effective submenu overlay opens. |
| `SubmenuClosed` | `RoutedEventHandler` | Bubble | Occurs once when the effective submenu overlay closes. |

## Template Parts

| Name | Type | Description |
| --- | --- | --- |
| `PART_HeaderPresenter` | `ContentPresenter` | Presents `Header`. |
| `PART_SubmenuOverlay` | `Overlay` | Projects child items outside the parent visual bounds. |
| `PART_ItemsPresenter` | `ItemsPresenter` | Realizes the child-item containers inside the overlay. |

## Applies to

Project: `Cerneala`

## See also

- `ItemsControl`
- `Overlay`
- `OverlayPlacement`
- `Menu`
- `MenuBar`
