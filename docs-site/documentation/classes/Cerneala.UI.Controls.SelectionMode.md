# SelectionMode Enum

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/SelectionMode.cs`

Specifies how many items the user can select in a `ListBox`, and how clicks and keys select them.

```csharp
public enum SelectionMode
```

## Examples

```csharp
using Cerneala.UI.Controls;

ListBox listBox = new() { SelectionMode = SelectionMode.Multiple };
listBox.SetItems(new[] { "Apple", "Banana", "Cherry" });
```

The user clicks `Apple` and then `Cherry`: `SelectedItems` is `["Apple", "Cherry"]` and `SelectedIndex` is `0`. A second click on `Apple` deselects it: `SelectedItems` is `["Cherry"]` and `SelectedIndex` is `2`.

## Remarks

`ListBox` uses this enum for `ListBox.SelectionMode`, whose default is `Single`, as WPF's `System.Windows.Controls.SelectionMode`. The value decides what the user's clicks, arrow keys, Space, and Ctrl+A select, and whether `ListBox.SelectAll` is supported. See [`ListBox`](Cerneala.UI.Controls.ListBox.md#selection-modes) for every click and key per mode.

Changing `ListBox.SelectionMode` to `Single` while several items are selected keeps only the first selected item. For example, when `SelectedItems` is `["Cherry", "Apple"]`, setting `SelectionMode = SelectionMode.Single` leaves `SelectedItems` `["Cherry"]`.

## Values

| Name | Value | Click | Arrow keys | `SelectAll` and Ctrl+A |
| --- | --- | --- | --- | --- |
| `Single` | `0` | Selects only the clicked item. Ctrl+click on the selected item deselects it. | Move the focus and select the new item. | `SelectAll` throws `NotSupportedException`; Ctrl+A does nothing. |
| `Multiple` | `1` | Selects or deselects the clicked item and keeps the others. | Move only the focus; Space selects or deselects the focused item. | Select every item. |
| `Extended` | `2` | Selects only the clicked item. Ctrl+click selects or deselects one item; Shift+click selects the items from the anchor to the clicked item. | Select only the new item; with Shift, the items from the anchor to it; with Ctrl, move only the focus. | Select every item. |

## Applies to

`Cerneala.UI.Controls.ListBox`.

## See also

- `Cerneala.UI.Controls.ListBox`
- `Cerneala.UI.Controls.Selection.SelectionModel`
- `Cerneala.UI.Controls.Primitives.Selector`
