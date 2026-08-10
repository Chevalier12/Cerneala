# AutomationSession Class

## Definition
Namespace: `Cerneala.UI.Automation`
Assembly/Project: `Cerneala`
Source: `UI/Automation/AutomationSession.cs`

Queries a retained UI tree and coordinates user-like input and API screenshots.

```csharp
public sealed class AutomationSession
```

## Examples
```csharp
AutomationSession automation = window.CreateAutomationSession();
automation.FindByAutomationId("search-box").Click();
automation.PressKey(InputKey.A, AutomationModifiers.Control);
automation.SendText("replacement");
automation.SaveScreenshot("captures/search.png");
```

## Remarks
`FindByAutomationId` requires a unique identifier. `FindByXPath` evaluates standard XPath against an XML projection of the current visual tree. XML element names are control type names; available attributes include `AutomationId`, `Name`, `Type`, `IsEnabled`, and `Visibility`.

Input is delegated to `IAutomationInputDriver`. Sessions created by `Window.CreateAutomationSession` use the retained input pipeline and route screenshots through `Window.SaveScreenshot`.

## Constructors
| Name | Description |
| --- | --- |
| `AutomationSession(UIElement, IAutomationInputDriver, Action<string>?)` | Creates a session for a tree root, input driver, and optional screenshot provider. |

## Properties
| Name | Description |
| --- | --- |
| `Input` | Configured input driver. |

## Methods
| Name | Description |
| --- | --- |
| `FindByAutomationId(string)` | Returns the unique element with the identifier. |
| `FindByXPath(string)` | Returns the unique element matched by XPath. |
| `FindAllByXPath(string)` | Returns all elements matched by XPath. |
| `PressKey(InputKey, AutomationModifiers)` | Sends one key chord to the focused element. |
| `SendText(string)` | Sends text input to the focused element. |
| `SaveScreenshot(string)` | Captures through the configured application screenshot provider. |

## Applies to
Retained UI tests and opt-in application automation.
