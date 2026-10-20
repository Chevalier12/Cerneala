# TextSearch Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/TextSearch.cs`

Provides attached UI properties that select the primary text used by `ComboBox` search and `ListBox` type-ahead search.

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

`Text` overrides the fallback text for an individual item. `TextPath` names a dotted public-property path evaluated for each item in an owning `ComboBox` or `ListBox`. If neither attached property supplies text, the owner resolves `DisplayMemberPath` on the item (an empty path means the item itself). The value that `TextPath` or `DisplayMemberPath` gives is converted to text as in WPF: a content element such as a `ComboBoxItem` gives the text of its `Content`, a `TextBlock` gives its `Text`, and any other value gives its `ToString()`. For example, the item `new ComboBoxItem { Content = "C#" }` without `Text` has the primary text `"C#"`, not its type name, and `new ComboBoxItem { Content = new TextBlock { Text = "F#" } }` has `"F#"`. A non-empty `TextPath` or `DisplayMemberPath` that resolves to `null` gives an empty string; there is no fallback to the item's own `ToString()`. For example, `TextPath = "Nickname"` on an item whose `Nickname` is `null` gives `""`.

These properties define the text to search; they do not enable search by themselves. `ComboBox.IsTextSearchEnabled` and `ListBox.IsTextSearchEnabled` control matching, and `ComboBox.IsTextSearchCaseSensitive` and `ListBox.IsTextSearchCaseSensitive` control comparison behavior. For example, a `ListBox` that shows `"Apple"`, `"Banana"`, and `"Cherry"` selects `"Banana"` when the user types `b` while it has the keyboard focus; see [`ListBox`](Cerneala.UI.Controls.ListBox.md#type-ahead) for the type-ahead rules. Changing `TextPath` on a `ListBox` restarts its type-ahead prefix.

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
- `ListBox`
- `ItemsControl.DisplayMemberPath`
