# MenuItemAutomationPeer Class

## Definition
Namespace: `Cerneala.UI.Accessibility`

Assembly/Project: `Cerneala`

Source: `UI/Accessibility/MenuItemAutomationPeer.cs`

Provides the automation peer used to expose a `MenuItem` and its submenu state in the semantics tree.

```csharp
public sealed class MenuItemAutomationPeer : AutomationPeer
```

Inheritance:
`object` -> `AutomationPeer` -> `MenuItemAutomationPeer`

## Examples

The peer derives its accessible name from `Header` when no explicit accessible name is set.

```csharp
using System.Collections.Generic;
using Cerneala.UI.Accessibility;
using Cerneala.UI.Controls;

MenuItem item = new() { Header = "Open" };
item.Items.Add(new MenuItem { Header = "Recent" });

MenuItemAutomationPeer peer = new(item);
IReadOnlyDictionary<SemanticsProperty, object?> properties = peer.GetProperties();

string? name = peer.Name; // "Open"
int itemCount = (int)properties[SemanticsProperty.ItemCount]!; // 1
bool isExpanded = (bool)properties[SemanticsProperty.IsExpanded]!;
```

An explicit accessible name takes precedence over the header.

```csharp
MenuItem item = new() { Header = "Open" };
AccessibleName.SetName(item, "Open document");

string? name = new MenuItemAutomationPeer(item).Name; // "Open document"
```

## Remarks

`AutomationPeer.Create(UIElement)` creates this peer for `MenuItem` before applying the generic `ItemsControl` fallback. The peer always reports `SemanticsRole.MenuItem`.

`Name` first uses the explicit value assigned through `AccessibleName`. When no explicit name is available, it derives text from supported `Header` content. `GetProperties()` exposes the current child count and maps `MenuItem.IsSubmenuOpen` to `SemanticsProperty.IsExpanded`, in addition to the inherited enabled and focused state.

When a submenu opens or closes, a subsequent semantics-tree update reflects both `IsExpanded` and the currently projected submenu children.

## Constructors

| Name | Description |
| --- | --- |
| `MenuItemAutomationPeer(MenuItem menuItem)` | Initializes a peer for the specified `MenuItem`. A `null` argument is rejected by the base constructor. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Role` | `SemanticsRole` | Gets `SemanticsRole.MenuItem`. |
| `Name` | `string?` | Gets the explicit accessible name, or text derived from `Header`. |
| `Owner` | `UIElement` | Inherited from `AutomationPeer`; gets the menu item associated with the peer. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `GetProperties()` | `IReadOnlyDictionary<SemanticsProperty, object?>` | Returns enabled, focused, item-count, and expanded semantic state. |
| `CreateNode(IReadOnlyList<SemanticsNode> children)` | `SemanticsNode` | Inherited from `AutomationPeer`; creates a semantics node using the peer role, name, properties, and child nodes. |

## Applies to

Cerneala UI accessibility semantics for `Cerneala.UI.Controls.MenuItem`.

## See also

- `Cerneala.UI.Accessibility.AccessibleName`
- `Cerneala.UI.Accessibility.AutomationPeer`
- `Cerneala.UI.Accessibility.MenuAutomationPeer`
- `Cerneala.UI.Controls.MenuItem`
