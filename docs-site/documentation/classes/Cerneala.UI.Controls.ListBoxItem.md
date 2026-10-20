# ListBoxItem Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/ListBoxItem.cs`

Represents the default item container used by `ListBox`, with content presentation, selection metadata, and selected-state rendering.

```csharp
public class ListBoxItem : ContentControl, ISelectableItemContainer
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Control` -> `ContentControl` -> `ListBoxItem`

Implements:
`ISelectableItemContainer`

## Examples

Create a selected list box item with content.

```csharp
using Cerneala.UI.Controls;

ListBoxItem item = new()
{
    Content = "Ink",
    ItemIndex = 0,
    Item = "Ink",
    IsSelected = true
};
```

`ListBox` creates `ListBoxItem` containers for non-container items.

```csharp
using Cerneala.UI.Controls;

ListBox listBox = new();
listBox.SetItems(new[] { "one", "two" });

listBox.SelectedIndex = 1;
```

## Remarks

`ListBoxItem` is the default container type returned by `ListBox` for ordinary item values. If an item is already a `ListBoxItem`, `ListBox` reuses that element as the container; otherwise it creates a new `ListBoxItem`.

The class implements `ISelectableItemContainer` through the public `ItemIndex`, `Item`, and `IsSelected` properties. These values are used by item container generation and selector logic to associate the realized container with its source item and current selection state.

`IsSelected` is backed by `IsSelectedProperty`. Its metadata affects render, input visuals, and aspect processing. When selected, `OnRender` fills the item bounds with a solid `Color(80, 130, 220)` brush. When not selected, it uses the inherited `Background` brush. Rendering is skipped when the chosen brush is `null` or the render rectangle has no positive width or height.

Because `ListBoxItem` derives from `ContentControl`, it also supports the inherited `Content` property and content-hosting behavior.

### Content presentation

`ListBoxItem` shows every kind of `Content`, as the `ContentPresenter` in the template of a WPF `ListBoxItem` does:

| Content | Result |
| --- | --- |
| `UIElement` | Hosted directly as the item's logical and visual child, as `ContentControl` hosts it. In a `ListBox` row, a template is tried first, in the order of `ContentPresenter`: the `ItemTemplate` of the `ListBox`, then a template of its `Templates` that matches the element and the `ItemTemplateKey`. When one applies, the internal `ContentPresenter` shows the template's element instead, and the `UIElement` is only the template's data. |
| `string` | Shown by an internal `ContentPresenter` child, which creates a `TextBlock` with the text. The `TextBlock` uses the item's `FontFamily`, `FontSize`, and `Foreground`. |
| Another value | Given to the same internal `ContentPresenter`. It shows a child only when a content template matches the value; otherwise the item shows only its background. |
| `null` | Given to the same internal `ContentPresenter`. It shows a child only when a content template creates one for `null`, for example the `ItemTemplate` of the owning `ListBox`; otherwise the item shows only its background. |

For example, `new ListBoxItem { Content = "Ink" }` has one `ContentPresenter` child, and that presenter has one `TextBlock` child whose `Text` is `"Ink"`. The item measures at least the size of that text plus `Padding` and `BorderThickness`. `Content` stays the string `"Ink"`.

When the item is a row of a `ListBox`, the `ListBox` passes its `ItemTemplate`, `ItemTemplateKey`, and `Templates` to the internal presenter, so the template creates the row's visual from the item. This includes an item that is a `UIElement` but not a `ListBoxItem`, as in WPF, where the `ContentPresenter` of the row tries its `ContentTemplate` before `UIElement` content. For example, with `ItemTemplate = new ContentTemplate("Row", typeof(object), key: null, priority: 0, context => new TextBlock { Text = "Row " + context.Index })` and `SetItems(new object[] { new Border() })`, the row's `Content` is the `Border`, the row shows a `TextBlock` that reads `Row 0`, and the `Border` has no visual parent. Without `ItemTemplate`, the row tries a template of `Templates` for the element, as a WPF `ContentPresenter` looks for a `DataTemplate` of the item's type before it shows `UIElement` content itself. With `ItemTemplateKey = "card"`, only a template whose key is `"card"` can apply; without a key, only a template without a key can apply, and its data type must match the element. For example, with `Templates.Add(new ContentTemplate<Border>("card", key: "card", priority: 0, context => new TextBlock { Text = "Card" }))`, `ItemTemplateKey = "card"`, and `SetItems(new UIElement[] { new Border(), new TextBlock() })`, the first row shows a `TextBlock` that reads `Card` and the `Border` has no visual parent, while the second row hosts the `TextBlock` item directly, because no template matches a `TextBlock`. A row recycled from a templated item to an item that no template matches hosts the new item directly, and the other way around. The `ItemTemplate` runs for a `null` item too, as a WPF `ContentPresenter` applies its `ContentTemplate` to `null` content, and as `ItemsControl` rows do. For example, a `ListBox` with `ItemsSource = new string?[] { null }` and an `ItemTemplate` that returns an element of 80 x 10 has one row whose template element is 80 x 10, whether the row is new or recycled from an earlier item. A `ListBoxItem` that is its own container (an item of the list that is already a `ListBoxItem`) does not use the `ListBox` item template, as in WPF. Without a `ListBox`, the presenter resolves content templates through the aspects of the item's root.

When the item has a `ComponentTemplate` or a `ComponentTemplateKey`, the template presents `Content`, and the item creates no internal presenter.

### Keyboard focus on a click

A `ListBoxItem` is focusable: its constructor sets `Focusable` to `true`, as a WPF `ListBoxItem` is a `Control` whose `Focusable` defaults to `true`. A left press on the item moves keyboard focus to the item, as WPF's `ListBoxItem.OnMouseLeftButtonDown` focuses the item. `IsTabStop` stays `false`, so the Tab key does not stop on every row, as a WPF `ListBox` lets the Tab key into its list only once.

For example, a `Button` named `other` has the focus, and the user clicks the first row of a `ListBox`. After the click, the row's `IsKeyboardFocused` is `true`, the `ListBox`'s `IsKeyboardFocusWithin` is `true`, and `other.IsKeyboardFocused` is `false`. A Space press after the click therefore does not click `other`.

A press on an element inside the row that can take the focus itself, such as a `TextBox` or `CheckBox` from the `ItemTemplate`, focuses that element instead, as before.

With `Focusable = false`, a press on the row leaves the focus where it was. The row owns the press, so no focusable element above it, such as a `ListBox` with `Focusable = true`, takes the focus either. `ComboBoxItem` derives from `ListBoxItem` but sets `Focusable` back to `false`; see `ComboBoxItem`.

A click changes the selection of its `ListBox` only when the item can take the keyboard focus, as WPF's `ListBoxItem` notifies its `ListBox` of the click only when its `Focus()` succeeds. What the click then selects depends on `ListBox.SelectionMode`; see `ListBox`. For example, an item with `Focusable = false` in a `ListBox` whose `SelectedIndex` is `-1`: after a click on the item, `IsSelected` is still `false`, `SelectedIndex` is still `-1`, and the focus is still on the element that had it. The same item with `Focusable = true` (the default) is selected and focused by the click. The item's focusability is read at the press, so an item whose `GotFocus` handler sets `Focusable = false` is still selected when the button is released. A `ComboBoxItem` is the exception: its `ComboBox` selects it on a click although it is not focusable.

### Keyboard focus from keys

The keys of a focused `ListBox` move the keyboard focus from row to row, as in a WPF `ListBox`. The arrow keys, Home, End, Page Up, Page Down, and type-ahead text focus the row they reach, and Space acts on the focused row. For example, in a `ListBox` with `SelectionMode.Multiple` that shows `"A"`, `"B"`, and `"C"`, after a click on the row `"A"`, a Down press focuses the row `"B"`: its `IsKeyboardFocused` is `true`, and `SelectionModel.SelectedIndices` stays `[0]`. A Space press then selects `"B"` too. In `SelectionMode.Single`, the same Down press focuses and selects `"B"`. See [`ListBox`](Cerneala.UI.Controls.ListBox.md#keyboard) for every key.

## Constructors

| Name | Description |
| --- | --- |
| `ListBoxItem()` | Initializes a new `ListBoxItem` with `ItemIndex` set to `-1`, `Item` set to `null`, `IsSelected` set to `false`, and `Focusable` set to `true` (`IsTabStop` stays `false`). The new item already has its internal `ContentPresenter`, which presents the `null` content. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `IsSelectedProperty` | `UiProperty<bool>` | Identifies the `IsSelected` UI property. The default value is `false`; metadata affects render, input visuals, and aspects. |
| `SelectedEvent` | `RoutedEvent` | Identifies the bubbling `Selected` event. Added as an owner of `Selector.SelectedEvent`, so it is the same routed event. |
| `UnselectedEvent` | `RoutedEvent` | Identifies the bubbling `Unselected` event. Added as an owner of `Selector.UnselectedEvent`, so it is the same routed event. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `ItemIndex` | `int` | Gets or sets the source item index associated with this container. Defaults to `-1`. |
| `Item` | `object?` | Gets or sets the source item associated with this container. Defaults to `null`. |
| `IsSelected` | `bool` | Gets or sets whether this container represents a selected item. In a `ListBox` that selects several items, every row of a selected item has `IsSelected == true`. Backed by `IsSelectedProperty`. |

## Methods

This class does not declare additional public methods.

## Protected Members

| Name | Return Type | Description |
| --- | --- | --- |
| `MeasureCore(MeasureContext context)` | `LayoutSize` | Measures the internal content presenter inside `Padding + BorderThickness` when the item presents non-element `Content` or applies the `ListBox` item template; otherwise uses `ContentControl` measurement. |
| `ArrangeCore(ArrangeContext context)` | `LayoutRect` | Arranges the internal content presenter inside `Padding + BorderThickness` when the item presents non-element `Content` or applies the `ListBox` item template; otherwise uses `ContentControl` arrangement. |
| `OnRender(RenderContext context)` | `void` | Renders the selected highlight or inherited background inside the current render bounds. The content child renders on top of it. |

## Events

| Name | Event Type | Description |
| --- | --- | --- |
| `Selected` | `RoutedEventHandler` | Raised by the item (`Source` = the item) when `IsSelected` changes to `true`. Bubbles; uses `SelectedEvent`, which shares the `Selector.SelectedEvent` identity. |
| `Unselected` | `RoutedEventHandler` | Raised by the item (`Source` = the item) when `IsSelected` changes to `false`. Bubbles; uses `UnselectedEvent`, which shares the `Selector.UnselectedEvent` identity. |

## Property Information

| Property | Identifier field | Default value | Metadata/options |
| --- | --- | --- | --- |
| `IsSelected` | `IsSelectedProperty` | `false` | `AffectsRender`, `AffectsInputVisual`, `AffectsAspect` |

## Applies To

Project: `Cerneala`

UI area: retained controls, item containers, content presentation, and selector state.

## See Also

- `ListBox`
- `ItemsControl`
- `ContentControl`
- `ContentPresenter`
- `ISelectableItemContainer`
