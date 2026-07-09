# Selector Class

## Definition
Namespace: `Cerneala.UI.Controls.Primitives`  
Assembly/Project: `Cerneala`  
Source: `UI/Controls/Primitives/Selector.cs`

Provides single-index selection behavior for item controls, including selected item lookup and mouse-up selection of realized item containers.

```csharp
public class Selector : ItemsControl
```

Inheritance:  
`object` -> `UiObject` -> `UIElement` -> `Control` -> `ItemsControl` -> `Selector`

Derived:  
`ListBox`

## Examples

```csharp
using Cerneala.UI.Controls.Primitives;

Selector selector = new();
selector.SetItems(new object?[] { "Ink", "Brush", "Paper" });

selector.SelectedIndex = 1;

object? selected = selector.SelectedItem; // "Brush"
bool hasSelection = selector.SelectionModel.HasSelection; // true

selector.SelectionModel.Clear();
```

## Remarks

`Selector` builds on `ItemsControl` and keeps a private `SelectionModel` as the selection source of truth. Setting `SelectedIndex` calls `SelectionModel.Select`; when the model changes, `Selector` updates `SelectedIndexProperty`, invalidates the previously selected and newly selected realized containers, and re-prepares those containers so `ISelectableItemContainer` state is refreshed.

Selection is single-index only. The valid no-selection value is `-1`; lower values are rejected by `SelectionModel.Select`.

During item container preparation, `Selector` installs a left-button `MouseUp` handler on the container. The handler selects the container's current item index by calling `SelectContainer`. Container handler registrations are stored in a `ConditionalWeakTable`, so re-preparing a container does not duplicate handlers. When a container is cleared, the handler owner is detached only if it still belongs to that `Selector`, which keeps reused containers from selecting an old owner.

`SelectedItem` reads from the inherited `Items` collection when `SelectedIndex` is in range and returns `null` otherwise.

## Constructors

| Name | Description |
| --- | --- |
| `Selector()` | Initializes a selector and subscribes to its internal `SelectionModel.SelectionChanged` event. |

## Fields

| Name | Description |
| --- | --- |
| `SelectedIndexProperty` | Identifies the `SelectedIndex` UI property. The default value is `-1`, and values must be greater than or equal to `-1`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `SelectedIndex` | `int` | Gets or sets the selected item index. Setting the property selects that index through `SelectionModel`. |
| `SelectedItem` | `object?` | Gets the selected object from `Items` when `SelectedIndex` is valid; otherwise, `null`. |
| `SelectionModel` | `SelectionModel` | Gets the selector's internal single-selection model. |

## Methods

| Name | Description |
| --- | --- |
| `IsItemSelected(int index)` | Returns whether the internal `SelectionModel` currently selects `index`. |
| `PrepareItemContainer(UIElement container, int index, object? item)` | Prepares the inherited item container state and associates a left-button mouse-up selection handler with the container. |
| `ClearItemContainer(UIElement container)` | Clears inherited item container state and detaches this selector as the handler owner for the container. |
| `SelectContainer(UIElement container)` | Selects the item index currently associated with `container`, when the container maps to a valid item index. |
| `OnPropertyChanged(UiPropertyChangedEventArgs args)` | Synchronizes direct `SelectedIndexProperty` changes back into the internal `SelectionModel`. |

## Property Information

| Property | Identifier field | Default value | Metadata/options | Validation |
| --- | --- | --- | --- | --- |
| `SelectedIndex` | `SelectedIndexProperty` | `-1` | `UiPropertyOptions.None` | Value must be `>= -1`. |

## Applies to

`Cerneala` targeting `net8.0`.

## See also

- `Cerneala.UI.Controls.ItemsControl`
- `Cerneala.UI.Controls.SelectionModel`
- `Cerneala.UI.Controls.ListBox`
- `Cerneala.UI.Controls.ISelectableItemContainer`
