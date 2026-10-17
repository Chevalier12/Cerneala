# ListBox Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/ListBox.cs`

Displays an item list with single selection behavior and `ListBoxItem` containers.

```csharp
public class ListBox : Selector
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Control` -> `ItemsControl` -> `Selector` -> `ListBox`

## Examples

```csharp
using Cerneala.UI.Controls;

ListBox listBox = new();
listBox.SetItems(new[] { "Draft", "Review", "Published" });

listBox.SelectedIndex = 1;

object? selected = listBox.SelectedItem;
```

```csharp
using Cerneala.UI.Controls;

ListBox listBox = new()
{
    ItemsSource = new[] { "One", "Two", "Three" }
};
```

Each row shows its item through `ItemTemplate`. Here the rows read `[Alpha]` and `[Beta]`, and each row's `Content` stays the item string.

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;

ListBox listBox = new()
{
    ItemsSource = new List<string> { "Alpha", "Beta" },
    ItemTemplate = new ContentTemplate<string>("Bracketed", key: null, priority: 0, context =>
        new TextBlock { Text = "[" + context.Data + "]" })
};
```

## Remarks

`ListBox` is a concrete `Selector` that configures item presentation for a vertical list. Its constructor assigns a retained vertical `StackPanel` directly to `ItemsPanel`.

Each data item is wrapped in a `ListBoxItem` container. If an item is already a `ListBoxItem`, that same element is used as its own container. Selection is inherited from `Selector`: setting `SelectedIndex` updates the selection model, and a left mouse button release on a prepared item container selects that item. A release on a button in the row, such as a `CheckBox` from the `ItemTemplate`, only clicks that button and does not select the row (see `Selector`).

`ListBoxItem` receives `ItemIndex`, `Item`, and `IsSelected` during container preparation. When a selected realized container is invalidated, the container render and input visual state are refreshed.

Row content follows `ItemsControl`: without `ItemTemplate`, a row's `Content` is the item's `DisplayMemberPath` value (the item itself when the path is empty); with `ItemTemplate`, it is the item. The row shows a `string` value as text, and `ListBox` passes `ItemTemplate`, `ItemTemplateKey`, and `Templates` to the row, so a template creates the row's visual. For example, `SetItems(new[] { "Apple" })` creates a row whose `Content` is `"Apple"` and whose text reads `Apple`; the row is as tall as that text. See [`ListBoxItem`](Cerneala.UI.Controls.ListBoxItem.md#content-presentation) for every content kind. An item that is already a `ListBoxItem` is its own row and does not use the item template. Any other `UIElement` item is wrapped in a `ListBoxItem` row: with `ItemTemplate`, the row shows the element that the template creates for it (the item is `context.Data` and its index is `context.Index`), as in WPF; without `ItemTemplate`, the row hosts the element directly.

## Constructors

| Name | Description |
| --- | --- |
| `ListBox()` | Initializes a new list box and sets the default items panel to a `StackPanel`. |

## Fields

| Name | Description |
| --- | --- |
| `SelectedIndexProperty` | Inherited from `Selector`. Identifies the `SelectedIndex` UI property. |
| `ItemTemplateProperty` | Inherited from `ItemsControl`. Identifies the `ItemTemplate` UI property. |
| `ItemsPanelProperty` | Inherited from `ItemsControl`. Identifies the `ItemsPanel` UI property. |
| `ItemTemplateKeyProperty` | Inherited from `ItemsControl`. Identifies the `ItemTemplateKey` UI property. |
| `ItemsSourceProperty` | Inherited from `ItemsControl`. Identifies the `ItemsSource` UI property. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `SelectedIndex` | `int` | Inherited from `Selector`. Gets or selects the current item index. The default is `-1`; values below `-1` are rejected by the selection model/property validation. |
| `SelectedItem` | `object?` | Inherited from `Selector`. Gets the item at `SelectedIndex`, or `null` when the selected index is outside the current item range. |
| `SelectionModel` | `SelectionModel` | Inherited from `Selector`. Gets the single-selection model used by the selector. |
| `Items` | `ItemCollection` | Inherited from `ItemsControl`. Gets the local item collection used when `ItemsSource` is not set. |
| `ItemsSource` | `IEnumerable?` | Inherited from `ItemsControl`. Gets or sets an external item source. |
| `ItemCount` | `int` | Inherited from `ItemsControl`. Gets the count of the `ItemsSource` snapshot when `ItemsSource` is set; otherwise the count of `Items`. |
| `ItemTemplate` | `ContentTemplate?` | Inherited from `ItemsControl`. Gets or sets the template used to display non-container items. `ListBox` applies it inside each `ListBoxItem` row. |
| `ItemTemplateKey` | `string?` | Inherited from `ItemsControl`. Gets or sets the template key used by content presentation. |
| `ItemsPanel` | `Panel?` | Inherited from `ItemsControl`. Gets or sets the retained panel used by the items presenter. `ListBox` initializes it to a vertical `StackPanel`. |

## Events

| Name | Event Type | Description |
| --- | --- | --- |
| `SelectionChanged` | `EventHandler<SelectionChangedEventArgs>` | Inherited from `Selector`. Bubbling routed event raised after the selected index changes. |

## Methods

| Name | Description |
| --- | --- |
| `SetItems(IEnumerable? items)` | Inherited from `ItemsControl`. Replaces the local `Items` collection. |
| `GetItemAt(int index)` | Inherited from `ItemsControl`. Gets an item from the `ItemsSource` snapshot when `ItemsSource` is set; otherwise from `Items`. |

## Protected Members

| Name | Description |
| --- | --- |
| `DefaultContainerType` | Returns `typeof(ListBoxItem)`. |
| `GetContainerTypeForItem(object? item)` | Returns the item's own type when the item is a `ListBoxItem`; otherwise `typeof(ListBoxItem)`. |
| `CreateItemContainer(int index, object? item)` | Returns the item itself when it is a `ListBoxItem`; otherwise a new `ListBoxItem`. |
| `PrepareItemContent(UIElement container, int index, object? item)` | Passes `ItemTemplate`, `ItemTemplateKey`, `Templates`, and the item index to a `ListBoxItem` row, then sets its `Content` as `ItemsControl` does. |
| `ClearItemContainer(UIElement container)` | Clears the container as `ItemsControl` does, then removes the item template from a `ListBoxItem` row. |

## Applies to

`Cerneala` UI controls.

## See also

- `Cerneala.UI.Controls.Primitives.Selector`
- `Cerneala.UI.Controls.ItemsControl`
- `Cerneala.UI.Controls.ListBoxItem`
