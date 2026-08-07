# ComboBox Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/ComboBox.cs`

Represents a single-selection control with an optional text editor and an in-root drop-down overlay.

```csharp
public class ComboBox : Selector
```

Inheritance:

`object` -> `UiObject` -> `UIElement` -> `Control` -> `ItemsControl` -> `Selector` -> `ComboBox`

## Examples

```csharp
var people = new[] { new { Name = "Bucharest" } };
ComboBox cities = new()
{
    DisplayMemberPath = "Name",
    IsEditable = true,
    MaxDropDownHeight = 240
};
cities.SetItems(people);
cities.DropDownOpened += (_, _) => System.Console.WriteLine("opened");
```

## Remarks

The default template uses an `Overlay`; opening the drop-down does not create a native window or a second `UIRoot`. The list is projected above normal root content and is removed from hit testing while closed. The drop-down matches the control width, and its background, border, item foreground, and item font follow the corresponding `ComboBox` properties.

The default `ItemContainerAspect` assigns `Padding="6"` to each `ComboBoxItem`. Assign another `ItemContainerAspect` to replace this default.

Selection updates `SelectedIndex`, `SelectedItem`, and `Text` immediately. In editable mode, text may differ from every item. A differing value clears selection without filtering or automatically matching items.

Without an `ItemTemplate`, each item is presented through its display text. An empty `DisplayMemberPath` uses `ToString()`, including for enum and other non-string values.

Keyboard commands include `F4`, `Alt+Down`, `Alt+Up`, `Escape`, `Enter`, arrow keys, `Home`, and `End`. The drop-down also closes after pointer selection, light-dismiss, composite focus exit, disabling, or detaching.

The template must provide `PART_SelectionPresenter`, `PART_EditableTextBox`, `PART_DropDownToggle`, `PART_DropDownOverlay`, and `PART_ItemsPresenter`.

## Constructors

| Name | Description |
| --- | --- |
| `ComboBox()` | Initializes the default template, vertical items panel, item-container padding, focus behavior, and keyboard commands. |

## Fields

| Name | Description |
| --- | --- |
| `IsDropDownOpenProperty` | Identifies `IsDropDownOpen`; default `false`. |
| `IsEditableProperty` | Identifies `IsEditable`; default `false`. |
| `TextProperty` | Identifies `Text`; default empty string. |
| `MaxDropDownHeightProperty` | Identifies `MaxDropDownHeight`; default positive infinity. |
| `DropDownOpenedEvent` | Identifies the bubbling open event. |
| `DropDownClosedEvent` | Identifies the bubbling close event. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `IsDropDownOpen` | `bool` | Gets or sets the requested drop-down state. |
| `IsEditable` | `bool` | Selects text-editor or selection-presenter mode. |
| `Text` | `string` | Gets or sets the displayed/editor text. |
| `MaxDropDownHeight` | `float` | Limits projected drop-down height. Must be positive and not `NaN`. |
| `DisplayMemberPath` | `string` | Inherited dotted path used for default text and visuals. |

## Events

| Name | Description |
| --- | --- |
| `DropDownOpened` | Raised once the overlay is actually projected. |
| `DropDownClosed` | Raised once the projected overlay is withdrawn. |
| `SelectionChanged` | Inherited event raised when immediate selection changes. |

## Applies to

Project: `Cerneala`

## See also

- `ComboBoxItem`
- `ItemsControl`
- `Overlay`
- `Selector`
