# SemanticsProperty Enum

## Definition
Namespace: `Cerneala.UI.Accessibility`

Assembly/Project: `Cerneala`

Source: `UI/Accessibility/SemanticsProperty.cs`

Identifies a state or value exposed by a node in the Cerneala semantics tree.

```csharp
public enum SemanticsProperty
```

## Examples

Read semantic state from an automation peer by using the enum value as the property key.

```csharp
using System.Collections.Generic;
using Cerneala.UI.Accessibility;
using Cerneala.UI.Controls;

MenuItem item = new() { Header = "Open" };
MenuItemAutomationPeer peer = new(item);
IReadOnlyDictionary<SemanticsProperty, object?> properties = peer.GetProperties();

bool isExpanded = (bool)properties[SemanticsProperty.IsExpanded]!;
```

## Remarks

`AutomationPeer.GetProperties()` returns values keyed by `SemanticsProperty`. The base peer supplies `IsEnabled` and `IsFocused`; specialized peers add properties such as selection, value, item count, or expanded state.

## Fields

| Name | Description |
| --- | --- |
| `IsEnabled` | Indicates whether the semantic owner is enabled. |
| `IsFocused` | Indicates whether the semantic owner has keyboard focus. |
| `IsSelected` | Indicates whether an item is selected. |
| `Value` | Contains the value exposed by a control. |
| `ItemCount` | Contains the number of items owned by a collection control. |
| `IsExpanded` | Indicates whether an expandable item is currently open. |

## Applies to

Cerneala UI accessibility semantics.

## See also

- `Cerneala.UI.Accessibility.AutomationPeer`
- `Cerneala.UI.Accessibility.SemanticsNode`
- `Cerneala.UI.Accessibility.SemanticsRole`
