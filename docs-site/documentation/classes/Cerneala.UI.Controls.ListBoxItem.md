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
| `UIElement` | Hosted directly as the item's logical and visual child, as `ContentControl` hosts it. |
| `string` | Shown by an internal `ContentPresenter` child, which creates a `TextBlock` with the text. The `TextBlock` uses the item's `FontFamily`, `FontSize`, and `Foreground`. |
| Another value | Given to the same internal `ContentPresenter`. It shows a child only when a content template matches the value; otherwise the item shows only its background. |
| `null` | Given to the same internal `ContentPresenter`. It shows a child only when a content template creates one for `null`, for example the `ItemTemplate` of the owning `ListBox`; otherwise the item shows only its background. |

For example, `new ListBoxItem { Content = "Ink" }` has one `ContentPresenter` child, and that presenter has one `TextBlock` child whose `Text` is `"Ink"`. The item measures at least the size of that text plus `Padding` and `BorderThickness`. `Content` stays the string `"Ink"`.

When the item is a row of a `ListBox`, the `ListBox` passes its `ItemTemplate`, `ItemTemplateKey`, and `Templates` to the internal presenter, so the template creates the row's visual from the item. The `ItemTemplate` runs for a `null` item too, as a WPF `ContentPresenter` applies its `ContentTemplate` to `null` content, and as `ItemsControl` rows do. For example, a `ListBox` with `ItemsSource = new string?[] { null }` and an `ItemTemplate` that returns an element of 80 x 10 has one row whose template element is 80 x 10, whether the row is new or recycled from an earlier item. A `ListBoxItem` that is its own container (an item of the list that is already a `ListBoxItem`) does not use the `ListBox` item template, as in WPF. Without a `ListBox`, the presenter resolves content templates through the aspects of the item's root.

When the item has a `ComponentTemplate` or a `ComponentTemplateKey`, the template presents `Content`, and the item creates no internal presenter.

## Constructors

| Name | Description |
| --- | --- |
| `ListBoxItem()` | Initializes a new `ListBoxItem` with `ItemIndex` set to `-1`, `Item` set to `null`, and `IsSelected` set to `false`. The new item already has its internal `ContentPresenter`, which presents the `null` content. |

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
| `IsSelected` | `bool` | Gets or sets whether this container represents the selected item. Backed by `IsSelectedProperty`. |

## Methods

This class does not declare additional public methods.

## Protected Members

| Name | Return Type | Description |
| --- | --- | --- |
| `MeasureCore(MeasureContext context)` | `LayoutSize` | Measures the internal content presenter inside `Padding + BorderThickness` when the item presents non-element `Content`; otherwise uses `ContentControl` measurement. |
| `ArrangeCore(ArrangeContext context)` | `LayoutRect` | Arranges the internal content presenter inside `Padding + BorderThickness` when the item presents non-element `Content`; otherwise uses `ContentControl` arrangement. |
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
