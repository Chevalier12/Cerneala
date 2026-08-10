# AutomationModifiers Enum

## Definition
Namespace: `Cerneala.UI.Automation`
Assembly/Project: `Cerneala`
Source: `UI/Automation/AutomationModifiers.cs`

Specifies modifier keys held during an automation key press.

```csharp
[Flags]
public enum AutomationModifiers
```

## Examples
```csharp
automation.PressKey(InputKey.A, AutomationModifiers.Control);
```

## Remarks
Values can be combined. The retained driver maps them to left Shift, Control, and Alt keys.

## Fields
| Name | Description |
| --- | --- |
| `None` | No modifier. |
| `Shift` | Holds Shift. |
| `Control` | Holds Control. |
| `Alt` | Holds Alt. |

## Applies to
Automation key input.
