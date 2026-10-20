# ToggleButtonAutomationPeer Class

## Definition
Namespace: `Cerneala.UI.Accessibility`

Assembly/Project: `Cerneala`

Source: `UI/Accessibility/ToggleButtonAutomationPeer.cs`

Provides the semantics automation peer used for `Cerneala.UI.Controls.Primitives.ToggleButton` instances, including `CheckBox` and `RadioButton`. It adds the checked state to the button semantics.

```csharp
public sealed class ToggleButtonAutomationPeer : ButtonAutomationPeer
```

Inheritance:
`object` -> `AutomationPeer` -> `ButtonAutomationPeer` -> `ToggleButtonAutomationPeer`

## Examples

A checked `CheckBox` produces a node with role `CheckBox`, the content text as name, and `IsChecked` set to `true`.

```csharp
using System.Collections.Generic;
using Cerneala.UI.Accessibility;
using Cerneala.UI.Controls;

CheckBox checkBox = new() { Content = "Bold", IsChecked = true };
AutomationPeer peer = AutomationPeer.Create(checkBox);

SemanticsRole role = peer.Role; // SemanticsRole.CheckBox
string? name = peer.Name; // "Bold"
IReadOnlyDictionary<SemanticsProperty, object?> properties = peer.GetProperties();
bool isChecked = (bool)properties[SemanticsProperty.IsChecked]!; // true
```

`IsChecked` is present even when the toggle is not checked. `Invoke()` activates the toggle, so on a `CheckBox` it flips `IsChecked`.

```csharp
using Cerneala.UI.Accessibility;
using Cerneala.UI.Controls;

CheckBox checkBox = new() { Content = "Bold" };
ToggleButtonAutomationPeer peer = new(checkBox);

bool invoked = peer.Invoke(); // true
bool isChecked = checkBox.IsChecked; // true
bool reported = (bool)peer.GetProperties()[SemanticsProperty.IsChecked]!; // true
```

## Remarks

`AutomationPeer.Create(UIElement)` creates this peer for `ToggleButton`, `CheckBox`, and `RadioButton`. The factory checks `ToggleButton` before `Button`, so these controls no longer get a plain `ButtonAutomationPeer`. `Button` and `RepeatButton` still get `ButtonAutomationPeer`, with role `Button` and no `IsChecked` entry.

`Role` depends on the type of the owner:

| Owner | Role |
| --- | --- |
| `CheckBox` (or a type derived from it) | `SemanticsRole.CheckBox` |
| `RadioButton` (or a type derived from it) | `SemanticsRole.RadioButton` |
| Any other `ToggleButton` | `SemanticsRole.Button` |

A plain `ToggleButton` keeps the `Button` role, which matches the control type WPF UI Automation reports for it.

`GetProperties()` returns the properties of `ButtonAutomationPeer` (`IsEnabled`, `IsFocused`) plus `SemanticsProperty.IsChecked`, whose value is the current `ToggleButton.IsChecked` as a `bool`. The key is always present, also when the value is `false`.

`Name` and `Invoke()` are inherited from `ButtonAutomationPeer`. The name comes from the explicit accessible name or from the button content text. `Invoke()` calls `IInputActivatable.Activate` on an enabled toggle, which is the same activation a click runs. On a `CheckBox` or `ToggleButton` it toggles `IsChecked`; on a `RadioButton` it only checks the button and never unchecks it. It returns `false` and does nothing when the toggle is disabled.

`ToggleButton.IsCheckedProperty` is registered with `UiPropertyOptions.AffectsSemantics`. Changing `IsChecked` therefore marks the cached semantics tree dirty, and the next `UIRoot.GetSemanticsTree()` or Servo query reads the new value without waiting for a frame.

## Constructors

| Name | Description |
| --- | --- |
| `ToggleButtonAutomationPeer(ToggleButton toggleButton)` | Initializes a new peer for `toggleButton` and passes it to the `ButtonAutomationPeer` and base `AutomationPeer` owner. A `null` toggle button is rejected by the base constructor. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Role` | `SemanticsRole` | Gets `CheckBox` for a `CheckBox`, `RadioButton` for a `RadioButton`, and `Button` for any other `ToggleButton`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `GetProperties()` | `IReadOnlyDictionary<SemanticsProperty, object?>` | Returns the enabled and focused state plus `SemanticsProperty.IsChecked` with the current `ToggleButton.IsChecked` value. |

## Inherited Properties

| Name | Type | Description |
| --- | --- | --- |
| `Name` | `string?` | Inherited from `ButtonAutomationPeer`; gets the explicit accessible name, or text derived from the button content. |
| `Owner` | `UIElement` | Inherited from `AutomationPeer`; gets the toggle button passed to the constructor. |

## Inherited Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Invoke()` | `bool` | Inherited from `ButtonAutomationPeer`; activates an enabled toggle (a click) and returns `true`, or returns `false` when it is disabled. |
| `CreateNode(IReadOnlyList<SemanticsNode> children)` | `SemanticsNode` | Inherited from `AutomationPeer`; creates a semantics node using the role, name, properties, and child nodes. |

## Applies to

`Cerneala` UI accessibility semantics for `Cerneala.UI.Controls.Primitives.ToggleButton`, `Cerneala.UI.Controls.CheckBox`, and `Cerneala.UI.Controls.RadioButton`.

## See also

- `Cerneala.UI.Accessibility.AutomationPeer`
- `Cerneala.UI.Accessibility.ButtonAutomationPeer`
- `Cerneala.UI.Accessibility.SemanticsRole`
- `Cerneala.UI.Accessibility.SemanticsProperty`
- `Cerneala.UI.Controls.Primitives.ToggleButton`
- `Cerneala.UI.Controls.CheckBox`
- `Cerneala.UI.Controls.RadioButton`
