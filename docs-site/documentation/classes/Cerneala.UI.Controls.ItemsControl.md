# ItemsControl Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/ItemsControl.cs`

Displays generated item containers from a local collection or external enumerable source.

```csharp
public class ItemsControl : Control
```

## Examples

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Items;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

var people = new[] { new { Name = "Bucharest" } };
ItemsControl control = new()
{
    ItemsSource = people,
    DisplayMemberPath = "Name",
    ItemsPanel = new ItemsPanelTemplate(() => new StackPanel()),
    ItemContainerAspect = new ElementAspect(
    [
        new ElementAspectValue(UIElement.MarginProperty, new Thickness(3))
    ])
};
```

## Remarks

Without a component template, `ItemsControl` uses its built-in `ItemsPresenter`. A derived templated control can activate an `ItemsPresenter` supplied by its template; the fallback remains available when that template is removed.

`ItemTemplate` has priority for item visuals. Otherwise, `DisplayMemberPath` resolves a public readable property path. Paths may contain dots, an intermediate `null` produces an empty/default presentation, and an invalid segment throws `InvalidOperationException`. An empty path uses the item itself and therefore `ToString()` for textual presentation. Accessor chains are cached per item type and path.

`ItemsSource` takes precedence over `Items`. Observable sources are subscribed while attached. Replacing or clearing the local collection, or receiving an observable reset/clear notification, immediately clears containers associated with the previous item snapshot even when the presenter is not currently measured. Incremental add and replace notifications preserve compatible realized containers. Generated containers, recycling, and virtualization remain managed by `ItemContainerGenerator` and `ItemsPresenter`.

`ItemContainerAspect` is applied to every prepared container at the `AspectBase` value source and removed when the container is cleared or recycled. An aspect assigned directly to an item container has higher precedence. Changing the property refreshes realized containers.

## Constructors

| Name | Description |
| --- | --- |
| `ItemsControl()` | Creates the local collection, generator, and fallback presenter. |

## Fields

| Name | Description |
| --- | --- |
| `DisplayMemberPathProperty` | Identifies `DisplayMemberPath`; default empty string. |
| `ItemTemplateProperty` | Identifies `ItemTemplate`; default `null`. |
| `ItemTemplateKeyProperty` | Identifies `ItemTemplateKey`; default `null`. |
| `ItemContainerAspectProperty` | Identifies `ItemContainerAspect`; default `null`. |
| `ItemsPanelProperty` | Identifies `ItemsPanel`; default `null`. |
| `ItemsSourceProperty` | Identifies `ItemsSource`; default `null`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `DisplayMemberPath` | `string` | Dotted public property path used by default presentation. |
| `Items` | `ItemCollection` | Local items used when `ItemsSource` is null. |
| `ItemsSource` | `IEnumerable?` | External item source. |
| `ItemCount` | `int` | Number of items in the active source. |
| `ItemTemplate` | `ContentTemplate?` | Explicit visual template with priority over `DisplayMemberPath`. |
| `ItemContainerAspect` | `ElementAspect?` | Aspect applied to generated or prepared item containers. |
| `ContentTemplateRegistry` | `ContentTemplateRegistry` | Registry used when resolving generated content templates; cannot be `null`. |
| `ItemsPanel` | `ItemsPanelTemplate?` | Template used to create the items panel. |
| `ItemsPresenter` | `ItemsPresenter` | Currently active presenter. |
| `ItemContainerGenerator` | `ItemContainerGenerator` | Generator and realized-container registry. |

## Methods

| Name | Description |
| --- | --- |
| `SetItems(IEnumerable?)` | Replaces the local item collection. |
| `GetItemAt(int)` | Returns an item from the active source. |
| `SetVirtualizationContext(VirtualizationContext?)` | Assigns presenter virtualization state. |
| `UpdateVirtualizationFromScrollInfo(...)` | Updates the realization window from scroll metrics. |

## Protected Members

| Name | Description |
| --- | --- |
| `PrepareItemContent(...)` | Applies default content/template presentation and can be overridden by specialized containers. |
| `ActivateItemsPresenter(ItemsPresenter?)` | Activates a template presenter or restores the fallback presenter. |

## Applies to

Project: `Cerneala`

## See also

- `ItemsPresenter`
- `ItemContainerGenerator`
- `ComboBox`
