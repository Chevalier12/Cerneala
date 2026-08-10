# AutomationProperties Class

## Definition
Namespace: `Cerneala.UI.Automation`
Assembly/Project: `Cerneala`
Source: `UI/Automation/AutomationProperties.cs`

Provides stable automation metadata for retained UI elements.

```csharp
public static class AutomationProperties
```

## Examples
```csharp
AutomationProperties.SetAutomationId(searchBox, "search-box");
```

## Remarks
Whitespace-only identifiers become `null`; surrounding whitespace is trimmed. Identifiers should be unique inside an automation session.

## Fields
| Name | Description |
| --- | --- |
| `AutomationIdProperty` | Attached UI property that stores the identifier. |

## Methods
| Name | Description |
| --- | --- |
| `GetAutomationId(UIElement)` | Reads the identifier. |
| `SetAutomationId(UIElement, string?)` | Sets or clears the identifier. |

## Applies to
All `UIElement` instances.
