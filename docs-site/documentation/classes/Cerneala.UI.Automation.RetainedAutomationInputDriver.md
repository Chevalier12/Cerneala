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
await automation.FindByAutomationId("slider")
    .DragAsync(0.1f, 0.5f, 0.9f, 0.5f, steps: 24);
```

## Remarks
Clicks use the arranged center of the selected element. Drags use normalized coordinates in the target's arranged bounds and send pointer movement, left-button press, interpolated movement, and release frames. Native window sessions process each drag frame through a rendered application frame. Key chords send modifier-down, key-down, key-up, and modifier-up frames. `SendText` emits each Unicode text element through the routed text input pipeline.

## Constructors
| Name | Description |
| --- | --- |
| `RetainedAutomationInputDriver(UiHost)` | Creates a driver for one host and UI root. |

## Methods
| Name | Description |
| --- | --- |
| `Click(UIElement)` | Sends pointer movement, press, and release frames. |
| `DragAsync(UIElement, float, float, float, float, int, CancellationToken)` | Sends a left-button drag through the retained pointer pipeline. |
| `PressKey(InputKey, AutomationModifiers)` | Sends a complete key chord. |
| `SendText(string)` | Sends routed text input frames. |

## Applies to
In-process retained UI automation.
