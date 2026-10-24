# ButtonAutomationPeer Class

## Definition
Namespace: `Cerneala.UI.Accessibility`

Assembly/Project: `Cerneala`

Source: `UI/Accessibility/ButtonAutomationPeer.cs`

Provides the semantics automation peer used for `Cerneala.UI.Controls.Button` instances.

```csharp
public class ButtonAutomationPeer : AutomationPeer
```

Inheritance:
`object` -> `AutomationPeer` -> `ButtonAutomationPeer`

Derived:
`ToggleButtonAutomationPeer`

## Examples

The peer exposes button semantics and resolves the accessible name from the button content when no explicit accessible name is set.

```csharp
using System.Collections.Generic;
using Cerneala.UI.Accessibility;
using Cerneala.UI.Controls;

Button button = new() { Content = "Save", IsEnabled = false };
ButtonAutomationPeer peer = new(button);

SemanticsRole role = peer.Role; // SemanticsRole.Button
string? name = peer.Name; // "Save"
IReadOnlyDictionary<SemanticsProperty, object?> properties = peer.GetProperties();
bool isEnabled = (bool)properties[SemanticsProperty.IsEnabled]!;
bool invoked = peer.Invoke(); // false because the button is disabled
```

An explicit accessible name takes precedence over the button content.

```csharp
using Cerneala.UI.Accessibility;
using Cerneala.UI.Controls;

Button button = new() { Content = "Save" };
AccessibleName.SetName(button, "Store");

ButtonAutomationPeer peer = new(button);
string? name = peer.Name; // "Store"
```

## Remarks

`ButtonAutomationPeer` is the specialized `AutomationPeer` created by `AutomationPeer.Create` for `Button` and `RepeatButton` elements. `ToggleButton`, `CheckBox`, and `RadioButton` also derive from `Button`, but the factory gives them the derived `ToggleButtonAutomationPeer`, which adds their checked state and, for `CheckBox` and `RadioButton`, their own role. It keeps the `Button` passed to the constructor as its semantic owner through the base `AutomationPeer` constructor.

The peer reports `SemanticsRole.Button`; the class is not sealed, and `ToggleButtonAutomationPeer` overrides `Role`. Its `Name` first uses `AccessibleName.GetName(button)`, which returns a non-blank explicit accessible name when one is set. If there is no explicit name, the name is derived from supported button content text, including non-blank `string` content, `TextBlock.Text`, nested `Button.Content`, `ContentControl.Content`, and `ContentPresenter.Content`.

Inherited `AutomationPeer` behavior still applies. `GetProperties()` returns the owner's enabled and keyboard focus state, and `CreateNode(IReadOnlyList<SemanticsNode> children)` builds a `SemanticsNode` for the owner element, the effective role, the resolved name, peer properties, and the supplied child nodes.

`Invoke()` calls `IInputActivatable.Activate` on the button, the same call keyboard activation uses. That runs the button's click logic, which a pointer click also reaches. It returns `false` without raising `Click` when the button is disabled.

## Constructors

| Name | Description |
| --- | --- |
| `ButtonAutomationPeer(Button button)` | Initializes a new peer for `button` and passes it to the base `AutomationPeer` owner. A `null` button is rejected by the base constructor. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Role` | `SemanticsRole` | Gets `SemanticsRole.Button`. |
| `Name` | `string?` | Gets the explicit accessible name for the button, or text derived from the button content. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Invoke()` | `bool` | Activates the enabled button and returns `true`; returns `false` when the button is disabled. |

## Inherited Properties

| Name | Type | Description |
| --- | --- | --- |
| `Owner` | `UIElement` | Gets the `Button` instance passed to the constructor, exposed as the base `UIElement` owner. |

## Inherited Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `GetProperties()` | `IReadOnlyDictionary<SemanticsProperty, object?>` | Returns semantic properties for the owner, including `SemanticsProperty.IsEnabled` and `SemanticsProperty.IsFocused`. |
| `CreateNode(IReadOnlyList<SemanticsNode> children)` | `SemanticsNode` | Creates a semantics node for the owner using the effective role, resolved name, properties, and child nodes. |

## Applies to

`Cerneala` UI accessibility semantics for `Cerneala.UI.Controls.Button`.

## See also

- `Cerneala.UI.Accessibility.AutomationPeer`
- `Cerneala.UI.Accessibility.AccessibleName`
- `Cerneala.UI.Accessibility.SemanticsRole`
- `Cerneala.UI.Accessibility.ToggleButtonAutomationPeer`
- `Cerneala.UI.Controls.Button`
