# TabItem Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/TabItem.cs`

Represents a selectable tab container with separate `Header` and `Content` surfaces.

```csharp
public class TabItem : ContentControl, ISelectableItemContainer
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Control` -> `ContentControl` -> `TabItem`

Implements:
`ISelectableItemContainer`

## Examples

Create a tab item directly and select it.

```csharp
using Cerneala.UI.Controls;

TabItem tabItem = new()
{
    Header = "Settings",
    Content = "Preferences",
    IsSelected = true
};
```

Use a `TabItem` as an item in a `TabControl`.

```csharp
using Cerneala.UI.Controls;

TabControl tabControl = new();
TabItem tabItem = new()
{
    Header = "General",
    Content = "General settings"
};

tabControl.SetItems(new[] { tabItem });
tabControl.SelectedIndex = 0;
tabControl.ItemContainerGenerator.Realize();

TabItem? selected = tabControl.SelectedTabItem;
```

## Remarks

`TabItem` derives from `ContentControl`, so its main body content is stored in the inherited `Content` property. The class adds a `Header` property for the tab header and an `IsSelected` property used by selection controls.

`TabControl` uses `TabItem` as its default item container. When containers are prepared through `ItemsControl`, the `ISelectableItemContainer` members are updated with the realized item index, source item, and selection state. Clearing the container resets `ItemIndex` to `-1`, `Item` to `null`, and `IsSelected` to `false`.

When `Header` is a `UIElement` and the control has no template, `TabItem` hosts that header directly in its logical and visual children. In that direct-hosting mode, layout measures and arranges the header inside the control insets, while the content element is measured and arranged with zero size. When a template is present, layout is delegated to the base/template path.

Assigning a `UIElement` to `Header` uses the same ownership validation as `ContentControl.Content`: the element cannot be self-owned, cannot be an ancestor, cannot already have a logical or visual parent, and cannot belong to a different root.

## Constructors

| Name | Description |
| --- | --- |
| `TabItem()` | Initializes a new `TabItem` instance. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `HeaderProperty` | `UiProperty<object?>` | Identifies the `Header` UI property. The default value is `null`; metadata affects measure and render. |
| `IsSelectedProperty` | `UiProperty<bool>` | Identifies the `IsSelected` UI property. The default value is `false`; metadata affects render, input visuals, and aspect. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Header` | `object?` | `null` | Gets or sets the tab header content. A `UIElement` header can be hosted directly when no template is applied. |
| `IsSelected` | `bool` | `false` | Gets or sets whether the tab item is selected. |
| `Item` | `object?` | `null` | Gets or sets the item associated with this generated or prepared container. |
| `ItemIndex` | `int` | `-1` | Gets or sets the item index associated with this generated or prepared container. |

## Methods

`TabItem` does not declare public methods. It overrides protected layout and property-change members to coordinate header hosting, template changes, and direct layout.

## Applies to

Cerneala UI controls.

## See also

- `ContentControl`
- `TabControl`
- `ISelectableItemContainer`
