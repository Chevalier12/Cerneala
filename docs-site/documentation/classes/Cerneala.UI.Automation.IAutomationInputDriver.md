# IAutomationInputDriver Interface

## Definition
Namespace: `Cerneala.UI.Automation`
Assembly/Project: `Cerneala`
Source: `UI/Automation/IAutomationInputDriver.cs`

Defines the replaceable input transport used by automation sessions.

```csharp
public interface IAutomationInputDriver
```

## Examples
```csharp
var session = new AutomationSession(root, customDriver);
```

## Remarks
Implementations should model user input rather than mutate target control values. `RetainedAutomationInputDriver` is the built-in implementation.

## Methods
| Name | Description |
| --- | --- |
| `Click(UIElement)` | Clicks a target element. |
| `PressKey(InputKey, AutomationModifiers)` | Sends a key chord. |
| `SendText(string)` | Sends text input. |

## Applies to
Custom automation transports and test doubles.
