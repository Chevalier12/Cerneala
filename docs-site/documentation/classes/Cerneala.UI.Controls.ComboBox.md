# ComboBox Class

## Definition
Namespace: `Cerneala.UI.Controls`
Assembly/Project: `Cerneala`
Source: `UI/Controls/ComboBox.cs`

Represents an items control with shared selector state for choosing one item by index or item container input.

```csharp
public class ComboBox : Selector
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Control` -> `ItemsControl` -> `Selector` -> `ComboBox`

## Examples

```csharp
using Cerneala.UI.Controls;

var comboBox = new ComboBox();
comboBox.SetItems(new[] { "one", "two" });

comboBox.SelectedIndex = 1;
object? selected = comboBox.SelectedItem; // "two"
```

## Remarks

`ComboBox` is currently a thin concrete control over `Selector`. It does not declare additional members in `ComboBox.cs`; its item and selection behavior comes from `ItemsControl` and `Selector`.

Selection is single-index based. Setting `SelectedIndex` delegates to the shared `SelectionModel`, and `SelectedItem` resolves the selected item from `Items` when the index is in range. The `SelectedIndex` UI property defaults to `-1` and rejects values lower than `-1`.

Realized item containers participate in retained input routing through `Selector`: a left mouse button release on a realized container selects that container's item index.

## Constructors

| Name | Description |
| --- | --- |
| `ComboBox()` | Initializes a `ComboBox` using the inherited `Selector` and `ItemsControl` setup. |

## Fields

| Name | Description |
| --- | --- |
| `SelectedIndexProperty` | Inherited from `Selector`. Identifies the `SelectedIndex` UI property; default value is `-1` and valid values are `-1` or greater. |
| `ItemTemplateProperty` | Inherited from `ItemsControl`. Identifies the template used to render item content. |
| `ItemTemplateKeyProperty` | Inherited from `ItemsControl`. Identifies the template key used to resolve item content templates. |
| `ItemsPanelProperty` | Inherited from `ItemsControl`. Identifies the panel template used to lay out item containers. |
| `ItemsSourceProperty` | Inherited from `ItemsControl`. Identifies the enumerable source used to provide items. |

## Properties

| Name | Description |
| --- | --- |
| `SelectedIndex` | Inherited from `Selector`. Gets or sets the selected item index. Setting it updates the shared `SelectionModel`. |
| `SelectedItem` | Inherited from `Selector`. Gets the item at `SelectedIndex`, or `null` when there is no valid selection. |
| `SelectionModel` | Inherited from `Selector`. Gets the selection model that stores the selected index and raises selection changes. |
| `Items` | Inherited from `ItemsControl`. Gets the local item collection used when `ItemsSource` is not set. |
| `ItemsSource` | Inherited from `ItemsControl`. Gets or sets an enumerable source for items. |
| `ItemCount` | Inherited from `ItemsControl`. Gets the number of items from the observable source, `ItemsSource`, or `Items`. |
| `ItemTemplate` | Inherited from `ItemsControl`. Gets or sets the data template applied to item content. |
| `ItemTemplateKey` | Inherited from `ItemsControl`. Gets or sets the key used to resolve an item content template. |
| `ContentTemplateRegistry` | Inherited from `ItemsControl`. Gets or sets the registry used by generated content presenters. |
| `ItemsPanel` | Inherited from `ItemsControl`. Gets or sets the panel template used by the items presenter. |
| `ItemContainerGenerator` | Inherited from `ItemsControl`. Gets the generator that realizes and tracks item containers. |
| `ItemsPresenter` | Inherited from `ItemsControl`. Gets the presenter responsible for displaying realized items. |

## Methods

| Name | Description |
| --- | --- |
| `SetItems(IEnumerable?)` | Inherited from `ItemsControl`. Replaces the local `Items` collection. |
| `GetItemAt(int)` | Inherited from `ItemsControl`. Returns the item at the requested index from the observable source, `ItemsSource`, or `Items`. |
| `SetVirtualizationContext(VirtualizationContext?)` | Inherited from `ItemsControl`. Sets the virtualization context used by the inherited items presenter. |
| `UpdateVirtualizationFromScrollInfo(IScrollInfo, float, int)` | Inherited from `ItemsControl`. Updates item virtualization from scroll information, item extent, and optional cache item count. |

## Applies to

Project: `Cerneala`

## See also

- Source: `UI/Controls/ComboBox.cs`
- `Selector`
- `ItemsControl`
- `SelectionModel`
