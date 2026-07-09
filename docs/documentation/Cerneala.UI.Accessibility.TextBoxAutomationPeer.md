# TextBoxAutomationPeer Class

## Definition
Namespace: `Cerneala.UI.Accessibility`
Assembly/Project: `Cerneala`
Source: `UI/Accessibility/TextBoxAutomationPeer.cs`

Provides accessibility semantics for text box controls.

```csharp
public sealed class TextBoxAutomationPeer : AutomationPeer
```

Inheritance:
`AutomationPeer` -> `TextBoxAutomationPeer`

## Examples

```csharp
using Cerneala.UI.Accessibility;
using Cerneala.UI.Controls;

var textBox = new TextBox();
var peer = new TextBoxAutomationPeer(textBox);
```

## Remarks

`TextBoxAutomationPeer` reports `SemanticsRole.EditableText` for text input controls. Its property snapshot includes `SemanticsProperty.Value` for normal text boxes and suppresses that value for `PasswordBox` instances.

The peer keeps a reference to the supplied `TextBoxBase` so it can read the current text when `GetProperties` is called.

## Constructors

| Name | Description |
| --- | --- |
| `TextBoxAutomationPeer(TextBoxBase)` | Initializes a peer for the supplied text box control. |

## Properties

| Name | Description |
| --- | --- |
| `Role` | Gets `SemanticsRole.EditableText`. |

## Methods

| Name | Description |
| --- | --- |
| `GetProperties()` | Returns the base semantic properties plus a value entry for non-password text boxes. |

## Applies to

Project: `Cerneala`

## See also

- Source: `UI/Accessibility/TextBoxAutomationPeer.cs`
- `AutomationPeer`
- `TextBoxBase`
- `PasswordBox`
