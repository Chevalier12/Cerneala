# AutomationElement Class

## Definition
Namespace: `Cerneala.UI.Automation`
Assembly/Project: `Cerneala`
Source: `UI/Automation/AutomationElement.cs`

Represents one UI element selected by an automation session.

```csharp
public sealed class AutomationElement
```

## Examples
```csharp
AutomationElement editor = automation.FindByXPath("//TextBox[@AutomationId='editor']");
editor.Click().PressKey(InputKey.A, AutomationModifiers.Control).SendText("new text");
```

## Remarks
Action methods call the session's input driver and return the same automation element for fluent scenarios. They do not assign control values directly.

## Properties
| Name | Description |
| --- | --- |
| `Element` | Underlying retained `UIElement`. |
| `AutomationId` | Current automation identifier. |
| `TypeName` | Runtime control type name. |

## Methods
| Name | Description |
| --- | --- |
| `Click()` | Clicks the center of the element. |
| `PressKey(InputKey, AutomationModifiers)` | Sends a key chord to current keyboard focus. |
| `SendText(string)` | Sends text to current keyboard focus. |

## Applies to
Automation sessions.
