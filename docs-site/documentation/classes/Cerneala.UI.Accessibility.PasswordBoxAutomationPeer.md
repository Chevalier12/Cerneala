# PasswordBoxAutomationPeer Class

## Definition
Namespace: `Cerneala.UI.Accessibility`

Assembly/Project: `Cerneala`

Source: `UI/Accessibility/PasswordBoxAutomationPeer.cs`

Provides password-safe editable-text accessibility semantics for `PasswordBox`.

```csharp
public sealed class PasswordBoxAutomationPeer : AutomationPeer
```

## Examples

```csharp
PasswordBoxAutomationPeer peer = new(new PasswordBox { Password = "secret" });
```

## Remarks

The peer reports `SemanticsRole.EditableText` but always returns `null` for `SemanticsProperty.Value`. The password is never included in the semantics tree.

## Constructors

| Name | Description |
| --- | --- |
| `PasswordBoxAutomationPeer(PasswordBox)` | Initializes a peer for the supplied password box. |

## Properties

| Name | Description |
| --- | --- |
| `Role` | Gets `SemanticsRole.EditableText`. |

## Methods

| Name | Description |
| --- | --- |
| `GetProperties()` | Returns base semantic properties with a suppressed value. |

## Applies to

Project: `Cerneala`

## See also

- `TextBoxAutomationPeer`
- `PasswordBox`
