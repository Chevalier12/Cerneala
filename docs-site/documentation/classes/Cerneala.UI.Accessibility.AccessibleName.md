# AccessibleName Class

## Definition
Namespace: `Cerneala.UI.Accessibility`
Assembly/Project: `Cerneala`
Source: `UI/Accessibility/AccessibleName.cs`

Provides helpers for storing and resolving the accessible name of a `UIElement`.

```csharp
public static class AccessibleName
```

## Examples

Set an explicit accessible name on a button:

```csharp
using Cerneala.UI.Accessibility;
using Cerneala.UI.Controls;

Button button = new() { Content = "Save" };

AccessibleName.SetName(button, "Save document");

string? name = AccessibleName.GetName(button);
// name == "Save document"
```

Use content text as the fallback name:

```csharp
using Cerneala.UI.Accessibility;
using Cerneala.UI.Controls;

Button button = new() { Content = "Cancel" };

string? name = AccessibleName.GetName(button);
// name == "Cancel"
```

## Remarks

`AccessibleName` stores the explicit name in the `NameProperty` UI property. The property default is `null`, uses `UiPropertyOptions.AffectsSemantics`, and coerces `null`, empty, or whitespace-only values to `null`.

`GetName(UIElement)` returns the explicit name when one exists. If the explicit name is missing or whitespace, it falls back to `GetContentText(object?)` for the same element.

`GetContentText(object?)` extracts text from supported content shapes only:

| Content type | Result |
| --- | --- |
| `string` | Returns the string when it is not null, empty, or whitespace. |
| `TextBlock` | Returns `TextBlock.Text` when it is not null, empty, or whitespace. |
| `Button` | Recursively reads `Button.Content`. |
| `ContentControl` | Recursively reads `ContentControl.Content`. |
| `ContentPresenter` | Recursively reads `ContentPresenter.Content`. |
| Other content | Returns `null`. |

`AutomationPeer.Name` uses `AccessibleName.GetName(Owner)`. `ButtonAutomationPeer.Name` also falls back to the button content text.

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `NameProperty` | `UiProperty<string?>` | Stores the explicit accessible name. The registered UI property name is `"AccessibleName"`, the default value is `null`, whitespace-only values are coerced to `null`, and the property affects semantics. |

## Methods

| Name | Return type | Description |
| --- | --- | --- |
| `GetName(UIElement element)` | `string?` | Returns the element's explicit accessible name, or readable content text when no explicit name is available. Throws `ArgumentNullException` when `element` is `null`. |
| `SetName(UIElement element, string? name)` | `void` | Sets the element's explicit accessible name. Throws `ArgumentNullException` when `element` is `null`. |
| `GetContentText(object? content)` | `string?` | Returns readable text from a supported content object, recursively unwrapping `Button`, `ContentControl`, and `ContentPresenter` content. Returns `null` for unsupported content or whitespace-only text. |

## Applies to

Project: `Cerneala`

## See also

- `UI/Accessibility/AutomationPeer.cs`
- `UI/Accessibility/ButtonAutomationPeer.cs`
