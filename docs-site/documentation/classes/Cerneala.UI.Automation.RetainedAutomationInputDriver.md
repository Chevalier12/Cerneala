# RetainedAutomationInputDriver Class

## Definition
Namespace: `Cerneala.UI.Automation`
Assembly/Project: `Cerneala`
Source: `UI/Automation/RetainedAutomationInputDriver.cs`

Drives user-like pointer, keyboard, and text frames through a `UiHost`.

```csharp
public sealed class RetainedAutomationInputDriver : IAutomationInputDriver
```

## Examples
```csharp
var driver = new RetainedAutomationInputDriver(host);
var automation = new AutomationSession(root, driver);
```

## Remarks
Clicks use the arranged center of the selected element. Key chords send modifier-down, key-down, key-up, and modifier-up frames. `SendText` emits each Unicode text element through the routed text input pipeline.

## Constructors
| Name | Description |
| --- | --- |
| `RetainedAutomationInputDriver(UiHost)` | Creates a driver for one host and UI root. |

## Methods
| Name | Description |
| --- | --- |
| `Click(UIElement)` | Sends pointer movement, press, and release frames. |
| `PressKey(InputKey, AutomationModifiers)` | Sends a complete key chord. |
| `SendText(string)` | Sends routed text input frames. |

## Applies to
In-process retained UI automation.
