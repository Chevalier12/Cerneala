# ISelectableItemContainer Interface

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/ItemsControl.cs`

Marks an item container that receives its item index, its item, and its selection state from an `ItemsControl`.

```csharp
public interface ISelectableItemContainer
```

## Examples

A minimal custom item container:

```csharp
using Cerneala.UI.Controls;

public sealed class TagChip : ContentControl, ISelectableItemContainer
{
    public int ItemIndex { get; set; } = -1;

    public object? Item { get; set; }

    public bool IsSelected { get; set; }
}
```

## Remarks

`ItemsControl` uses this interface in two places:

- `PrepareItemContainer` sets `ItemIndex` to the item index, `Item` to the item, and `IsSelected` to the result of `IsItemSelected(index)`.
- `ClearItemContainer` resets `ItemIndex` to `-1`, `Item` to `null`, and `IsSelected` to `false` when the container is recycled or cleared.

For example, when a `ListBox` with items `"A"`, `"B"`, `"C"` and `SelectedIndex = 1` prepares the container for `"B"`, that `ListBoxItem` gets `ItemIndex = 1`, `Item = "B"`, and `IsSelected = true`.

The aspect engine also reads this interface: an element that implements it with `IsSelected == true` gets the `AspectState.Selected` state, so aspect rules with `AspectCondition.State(AspectState.Selected)` match it.

`ListBoxItem` and `TabItem` implement the interface; `ComboBoxItem` inherits it from `ListBoxItem`. In those types `IsSelected` is a UI property whose change invalidates render, input visual, and aspect state and raises the `Selected` or `Unselected` routed event. `ItemIndex` and `Item` are plain properties; setting them invalidates nothing. A custom implementation, like the example, decides for itself what a change does.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `ItemIndex` | `int` | Gets or sets the index of the item the container represents. `ItemsControl` sets `-1` when it clears the container. |
| `Item` | `object?` | Gets or sets the item the container represents. `ItemsControl` sets `null` when it clears the container. |
| `IsSelected` | `bool` | Gets or sets whether the represented item is selected. |

## Applies to

Item containers generated or prepared by `Cerneala.UI.Controls.ItemsControl` and its derived controls.

## See also

- `Cerneala.UI.Controls.ItemsControl`
- `Cerneala.UI.Controls.ListBoxItem`
- `Cerneala.UI.Controls.TabItem`
- `Cerneala.UI.Controls.Primitives.Selector`
