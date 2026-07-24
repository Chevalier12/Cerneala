# TextBoxAutomationPeer Class

## Definition
Namespace: `Cerneala.UI.Accessibility`

Assembly/Project: `Cerneala`

Source: `UI/Accessibility/TextBoxAutomationPeer.cs`

Provides editable-text accessibility semantics for `TextBox`.

```csharp
public sealed class TextBoxAutomationPeer : AutomationPeer
```

## Examples

```csharp
TextBoxAutomationPeer peer = new(new TextBox { Text = "query" });
```

## Remarks

The peer reports `SemanticsRole.EditableText` and publishes the current `TextBox.Text` value. Password controls use `PasswordBoxAutomationPeer` instead.

## Constructors

| Name | Description |
| --- | --- |
| `TextBoxAutomationPeer(TextBox)` | Initializes a peer for the supplied text box. |

## Properties

| Name | Description |
| --- | --- |
| `Role` | Gets `SemanticsRole.EditableText`. |

## Methods

| Name | Description |
| --- | --- |
| `GetProperties()` | Returns base semantic properties plus the current text value. |

## Applies to

Project: `Cerneala`

## See also

- `PasswordBoxAutomationPeer`
- `TextBox`
