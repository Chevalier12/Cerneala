# MenuAutomationPeer Class

## Definition
Namespace: `Cerneala.UI.Accessibility`

Assembly/Project: `Cerneala`

Source: `UI/Accessibility/MenuAutomationPeer.cs`

Provides the automation peer used to expose `Menu` and `MenuBar` controls in the semantics tree.

```csharp
public sealed class MenuAutomationPeer : AutomationPeer
```

Inheritance:
`object` -> `AutomationPeer` -> `MenuAutomationPeer`

## Examples

The same peer reports the role that matches its menu owner and includes the current item count.

```csharp
using System.Collections.Generic;
using Cerneala.UI.Accessibility;
using Cerneala.UI.Controls;

MenuBar menuBar = new();
menuBar.Items.Add(new MenuItem { Header = "File" });

MenuAutomationPeer peer = new(menuBar);
IReadOnlyDictionary<SemanticsProperty, object?> properties = peer.GetProperties();

SemanticsRole role = peer.Role; // SemanticsRole.MenuBar
int itemCount = (int)properties[SemanticsProperty.ItemCount]!; // 1
```

## Remarks

`AutomationPeer.Create(UIElement)` creates a `MenuAutomationPeer` for both `Menu` and `MenuBar` before applying the generic `ItemsControl` fallback.

`Role` returns `SemanticsRole.MenuBar` when the owner is a `MenuBar`; otherwise it returns `SemanticsRole.Menu`. `GetProperties()` adds `SemanticsProperty.ItemCount` to the inherited enabled and focused state.

The inherited `Name` property uses an explicit accessible name assigned through `AccessibleName`.

## Constructors

| Name | Description |
| --- | --- |
| `MenuAutomationPeer(Menu menu)` | Initializes a peer for the specified `Menu` or `MenuBar`. A `null` argument is rejected by the base constructor. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Role` | `SemanticsRole` | Gets `Menu` for a `Menu` owner or `MenuBar` for a `MenuBar` owner. |
| `Name` | `string?` | Inherited from `AutomationPeer`; gets the explicit accessible name for the owner. |
| `Owner` | `UIElement` | Inherited from `AutomationPeer`; gets the menu associated with the peer. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `GetProperties()` | `IReadOnlyDictionary<SemanticsProperty, object?>` | Returns enabled, focused, and item-count semantic state. |
| `CreateNode(IReadOnlyList<SemanticsNode> children)` | `SemanticsNode` | Inherited from `AutomationPeer`; creates a semantics node using the peer role, name, properties, and child nodes. |

## Applies to

Cerneala UI accessibility semantics for `Cerneala.UI.Controls.Menu` and `Cerneala.UI.Controls.MenuBar`.

## See also

- `Cerneala.UI.Accessibility.AutomationPeer`
- `Cerneala.UI.Accessibility.MenuItemAutomationPeer`
- `Cerneala.UI.Controls.Menu`
- `Cerneala.UI.Controls.MenuBar`
