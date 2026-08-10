# TextSearch Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/TextSearch.cs`

Provides attached UI properties that select the primary text used by `ComboBox` search.

```csharp
public static class TextSearch
```

## Examples

Use a property path that differs from the displayed member:

```csharp
ComboBox cities = new()
{
    IsEditable = true,
    DisplayMemberPath = "Country"
};
TextSearch.SetTextPath(cities, "Name");
```

Assign explicit search text to an item:

```csharp
ComboBoxItem item = new() { Content = "C#" };
TextSearch.SetText(item, "C Sharp");
```

## Remarks

`Text` overrides the fallback text for an individual item. `TextPath` names a dotted public-property path evaluated for each item in an owning `ComboBox`. If neither attached property supplies text, `ComboBox` uses `DisplayMemberPath` and then `ToString()`.

These properties define the text to search; they do not enable search by themselves. `ComboBox.IsTextSearchEnabled` controls matching, and `ComboBox.IsTextSearchCaseSensitive` controls comparison behavior.

The attached properties can be set through the static methods or through the regular `UiObject.SetValue` API.

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `TextProperty` | `UiProperty<string>` | Identifies the per-item primary search text. Defaults to an empty string. |
| `TextPathProperty` | `UiProperty<string>` | Identifies the owner-level dotted search path. Defaults to an empty string. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `GetText(UiObject)` | `string` | Gets explicit search text from an item. |
| `SetText(UiObject, string)` | `void` | Sets explicit search text on an item. |
| `GetTextPath(UiObject)` | `string` | Gets the dotted search path from an items control. |
| `SetTextPath(UiObject, string)` | `void` | Sets the dotted search path on an items control. |

## Applies to

Project: `Cerneala`

## See also

- `ComboBox`
- `ItemsControl.DisplayMemberPath`
