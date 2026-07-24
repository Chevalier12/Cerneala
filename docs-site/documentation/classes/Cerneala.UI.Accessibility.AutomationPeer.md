# AutomationPeer Class

## Definition
Namespace: `Cerneala.UI.Accessibility`

Assembly/Project: `Cerneala`

Source: `UI/Accessibility/AutomationPeer.cs`

Creates semantic accessibility data for a `UIElement`.

```csharp
public class AutomationPeer
```

Inheritance:
`Object` -> `AutomationPeer`

Derived:
`ButtonAutomationPeer`, `ItemsControlAutomationPeer`, `PasswordBoxAutomationPeer`, `TextBoxAutomationPeer`

## Examples

Create a peer for an element and convert it to a `SemanticsNode`:

```csharp
using Cerneala.UI.Accessibility;
using Cerneala.UI.Controls;

Button button = new() { Content = "Save", IsKeyboardFocused = true };

AutomationPeer peer = AutomationPeer.Create(button);
SemanticsNode node = peer.CreateNode([]);

bool isFocused = node.GetProperty<bool>(SemanticsProperty.IsFocused);
```

The factory returns `ButtonAutomationPeer` for `Button`, so the node role is `SemanticsRole.Button` and the name can come from the button content when no explicit accessible name is set.

## Remarks

`AutomationPeer` is the base peer used by the Cerneala semantics tree. `SemanticsProvider` builds visible child nodes first, then calls `AutomationPeer.Create(element).CreateNode(children)` for each visible `UIElement`.

The base implementation stores the owning element, exposes a role, resolves an accessible name through `AccessibleName.GetName(Owner)`, and reports common semantic properties:

| Property | Value source |
| --- | --- |
| `SemanticsProperty.IsEnabled` | `Owner.IsEnabled` |
| `SemanticsProperty.IsFocused` | `Owner.IsKeyboardFocused` |

The default role is `SemanticsRole.Root` for `UIRoot` and `SemanticsRole.Group` for other elements. The factory specializes known element types:

| Element type | Peer returned | Role behavior |
| --- | --- | --- |
| `Button` | `ButtonAutomationPeer` | Uses `SemanticsRole.Button`; name can fall back to content text. |
| `TextBox` | `TextBoxAutomationPeer` | Uses `SemanticsRole.EditableText` and adds the current text as `SemanticsProperty.Value`. |
| `PasswordBox` | `PasswordBoxAutomationPeer` | Uses `SemanticsRole.EditableText` without exposing a semantic value. |
| `ItemsControl` | `ItemsControlAutomationPeer` | Uses `SemanticsRole.List`; adds `SemanticsProperty.ItemCount`. |
| `TextBlock` | `AutomationPeer` | Uses `SemanticsRole.Text` through the factory override path. |
| Any other `UIElement` | `AutomationPeer` | Uses the base role rules. |

This type feeds Cerneala's own `SemanticsNode` model. It is not a platform UI Automation adapter by itself.

## Constructors

| Name | Description |
| --- | --- |
| `AutomationPeer(UIElement owner)` | Initializes a peer for `owner`. Throws `ArgumentNullException` when `owner` is `null`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Owner` | `UIElement` | Gets the element represented by this peer. |
| `Role` | `SemanticsRole` | Gets the semantic role. The base implementation returns `Root` for `UIRoot`; otherwise `Group`. |
| `Name` | `string?` | Gets the accessible name from `AccessibleName.GetName(Owner)`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Create(UIElement element)` | `AutomationPeer` | Creates the base peer or a specialized peer for known controls. Throws `ArgumentNullException` when `element` is `null`. |
| `CreateNode(IReadOnlyList<SemanticsNode> children)` | `SemanticsNode` | Creates a semantic node using `Owner.ElementId`, the effective role, `Name`, `GetProperties()`, and the supplied child nodes. |
| `GetProperties()` | `IReadOnlyDictionary<SemanticsProperty, object?>` | Returns the base semantic property dictionary containing enabled and focused state. Derived peers can add or replace entries. |

## Protected Members

| Name | Type | Description |
| --- | --- | --- |
| `OverrideRole` | `SemanticsRole?` | Optional init-only role override used by the base `CreateNode` path. |
| `EffectiveRole` | `SemanticsRole` | Returns `OverrideRole` when set; otherwise returns `Role`. |

## Applies To

Cerneala UI semantics and accessibility infrastructure.

## See Also

- `Cerneala.UI.Accessibility.SemanticsProvider`
- `Cerneala.UI.Accessibility.SemanticsNode`
- `Cerneala.UI.Accessibility.AccessibleName`
- `Cerneala.UI.Accessibility.ButtonAutomationPeer`
- `Cerneala.UI.Accessibility.TextBoxAutomationPeer`
- `Cerneala.UI.Accessibility.PasswordBoxAutomationPeer`
- `Cerneala.UI.Accessibility.ItemsControlAutomationPeer`
