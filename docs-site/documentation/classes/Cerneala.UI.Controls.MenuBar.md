# MenuBar Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/MenuBar.cs`

Represents a horizontal menu root that coordinates one active top-level branch.

```csharp
public class MenuBar : Menu
```

Inheritance:
`Object` -> `UiObject` -> `UIElement` -> `Control` -> `ItemsControl` -> `Menu` -> `MenuBar`

## Examples

```csharp
using Cerneala.UI.Controls;

MenuItem file = new() { Header = "File" };
file.Items.Add(new MenuItem { Header = "Open" });
file.Items.Add(new MenuItem { Header = "Exit" });

MenuItem edit = new() { Header = "Edit" };
edit.Items.Add(new MenuItem { Header = "Copy" });

MenuBar menuBar = new();
menuBar.Items.Add(file);
menuBar.Items.Add(edit);
```

## Remarks

`MenuBar` inherits the item generation, nested session, command, and lifecycle behavior of `Menu`, but lays out its root containers in a horizontal `StackPanel`. Top-level submenus open below their owner; deeper submenus use lateral overlay placement.

Click, `Down`, `Enter`, or `Space` opens a top-level parent. While a branch is open, pointer movement over another top-level parent switches the active branch. `Left` and `Right` navigate root items with wrapping and preserve the single-open-branch invariant. Vertical submenu levels retain the `Menu` keyboard behavior.

The default component template requires `PART_ItemsPresenter`.

## Constructors

| Name | Description |
| --- | --- |
| `MenuBar()` | Creates a horizontal menu root with the default component template. |

## Properties

| Name | Description |
| --- | --- |
| `Items` | Gets the direct top-level item collection inherited from `ItemsControl`. |
| `ItemsSource` | Gets or sets the external top-level item source inherited from `ItemsControl`. |
| `ItemsPanel` | Gets or sets the root panel; the default is a horizontal `StackPanel`. |

## Template Parts

| Name | Type | Description |
| --- | --- | --- |
| `PART_ItemsPresenter` | `ItemsPresenter` | Realizes and presents the top-level `MenuItem` containers. |

## Applies to

Project: `Cerneala`

## See also

- `Menu`
- `MenuItem`
- `OverlayPlacement`
