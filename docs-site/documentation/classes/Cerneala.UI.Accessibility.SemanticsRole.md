# SemanticsRole Enum

## Definition
Namespace: `Cerneala.UI.Accessibility`

Assembly/Project: `Cerneala`

Source: `UI/Accessibility/SemanticsRole.cs`

Identifies the accessible role exposed by a node in the Cerneala semantics tree.

```csharp
public enum SemanticsRole
```

## Examples

Use an automation peer to obtain the role associated with a control.

```csharp
using Cerneala.UI.Accessibility;
using Cerneala.UI.Controls;

MenuBar menuBar = new();
SemanticsRole role = AutomationPeer.Create(menuBar).Role;

bool isMenuBar = role == SemanticsRole.MenuBar;
```

## Remarks

Automation peers assign a `SemanticsRole` when they create a `SemanticsNode`. Menu roots use `Menu` or `MenuBar`, while every menu entry uses `MenuItem`, including entries that own a submenu.

## Fields

| Name | Description |
| --- | --- |
| `None` | Indicates that no semantic role is assigned. |
| `Root` | Identifies the root of a semantics tree. |
| `Group` | Identifies a generic group or element. |
| `Button` | Identifies an activatable button. |
| `EditableText` | Identifies an editable text control. |
| `List` | Identifies a list of items. |
| `ListItem` | Identifies an item in a list. |
| `Text` | Identifies read-only text. |
| `Image` | Identifies an image. |
| `Menu` | Identifies a vertical menu. |
| `MenuBar` | Identifies a menu bar. |
| `MenuItem` | Identifies an item in a menu or menu bar. |

## Applies to

Cerneala UI accessibility semantics.

## See also

- `Cerneala.UI.Accessibility.AutomationPeer`
- `Cerneala.UI.Accessibility.SemanticsNode`
- `Cerneala.UI.Accessibility.SemanticsProperty`
