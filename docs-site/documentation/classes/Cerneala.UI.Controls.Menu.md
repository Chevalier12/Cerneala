# Menu Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Menu.cs`

Represents a vertical menu whose items participate in one shared open-menu session.

```csharp
public class Menu : ItemsControl
```

Inheritance:
`Object` -> `UiObject` -> `UIElement` -> `Control` -> `ItemsControl` -> `Menu`

Derived: `MenuBar`

## Examples

```csharp
using Cerneala.UI.Controls;

MenuItem recent = new() { Header = "Recent" };
recent.Items.Add(new MenuItem { Header = "Document.crn" });

Menu menu = new();
menu.Items.Add(new MenuItem { Header = "New" });
menu.Items.Add(recent);
```

## Remarks

`Menu` lays out its root items vertically. Data values that are not already controls receive generated `MenuItem` containers; an explicitly supplied `MenuItem` remains its own container. `DisplayMemberPath` and `ItemTemplate` use the inherited `ItemsControl` presentation contract to populate generated headers.

The root owns one menu session for every nested level. Opening a parent closes an incompatible sibling branch, while pointer movement over a vertical parent opens that parent's submenu. All submenu overlays in the open path share one light-dismiss domain.

`Up`, `Down`, `Home`, and `End` navigate eligible items without wrapping. `Right` enters a submenu, `Left` returns to its parent, `Enter` and `Space` activate the focused item, `Escape` closes the current level, and `Tab` closes the session without consuming normal focus navigation. Disabled and collapsed items are skipped.

Disabling or detaching the root, removing an open item, or replacing the active item source closes stale submenu overlays. The default component template requires `PART_ItemsPresenter`.

## Constructors

| Name | Description |
| --- | --- |
| `Menu()` | Creates a vertical menu with its shared interaction session and default component template. |

## Properties

| Name | Description |
| --- | --- |
| `Items` | Gets the direct root-item collection inherited from `ItemsControl`. |
| `ItemsSource` | Gets or sets the external root-item source inherited from `ItemsControl`. |
| `DisplayMemberPath` | Gets or sets the property path used for generated `MenuItem.Header` values. |
| `ItemTemplate` | Gets or sets the content template used for generated item headers. |
| `ItemsPanel` | Gets or sets the panel that lays out root containers; the default is a vertical `StackPanel`. |

## Template Parts

| Name | Type | Description |
| --- | --- | --- |
| `PART_ItemsPresenter` | `ItemsPresenter` | Realizes and presents the root `MenuItem` containers. |

## Applies to

Project: `Cerneala`

## See also

- `MenuBar`
- `MenuItem`
- `ItemsControl`
- `Overlay`
