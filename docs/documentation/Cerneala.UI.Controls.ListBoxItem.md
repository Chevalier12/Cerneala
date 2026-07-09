# ListBoxItem Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/ListBoxItem.cs`

Represents the default item container used by `ListBox`, with content hosting, selection metadata, and selected-state rendering.

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

`IsSelected` is backed by `IsSelectedProperty`. Its metadata affects render, input visuals, and aspect processing. When selected, `OnRender` fills the item bounds with `DrawColor(80, 130, 220)`. When not selected, it uses the inherited `Background` color. Rendering is skipped when the chosen color is transparent or the render rectangle has no positive width or height.

Because `ListBoxItem` derives from `ContentControl`, it also supports the inherited `Content` property and content-hosting behavior.

## Constructors

| Name | Description |
| --- | --- |
| `ListBoxItem()` | Initializes a new `ListBoxItem` with `ItemIndex` set to `-1`, `Item` set to `null`, and `IsSelected` set to `false`. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `IsSelectedProperty` | `UiProperty<bool>` | Identifies the `IsSelected` UI property. The default value is `false`; metadata affects render, input visuals, and aspects. |

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
| `OnRender(RenderContext context)` | `void` | Renders the selected highlight or inherited background inside the current render bounds. |

## Events

This class does not declare additional public events.

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
- `ISelectableItemContainer`
