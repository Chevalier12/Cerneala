# ItemsControl Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/ItemsControl.cs`

Displays a retained list of item containers generated from either a local `ItemCollection` or an external `IEnumerable` item source.

```csharp
public class ItemsControl : Control
```

Inheritance:
`Object` -> `UIElement` -> `Control` -> `ItemsControl`

Derived:
`Selector`

## Examples

The following example creates an `ItemsControl` with an external item source, a data template, and a custom items panel.

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;

ItemsControl control = new()
{
    ItemsSource = new[] { "one", "two" },
    ItemTemplate = new ContentTemplate<string>("Item", key: null, priority: 0, _ => new UIElement()),
    ItemsPanel = new ItemsPanelTemplate(() => new Panel())
};
```

The local `Items` collection can be used when `ItemsSource` is not set.

```csharp
ItemsControl control = new();
control.Items.Add("one");
control.Items.Add("two");

object? first = control.GetItemAt(0);
```

## Remarks

`ItemsControl` owns an `ItemsPresenter` and adds it as both a logical and visual child during construction. When the control does not have a template child, measurement and arrangement are delegated directly to this presenter.

Items can come from two places:

| Source | Behavior |
| --- | --- |
| `ItemsSource` | Preferred when non-null. `ItemCount` and `GetItemAt` read from the source. |
| `Items` | Used when `ItemsSource` is null. Changes to the collection mark presenter items dirty and invalidate item layout/render state. |

If `ItemsSource` implements `IObservableList`, `ItemsControl` subscribes to its `Changed` event while attached, invalidates generated items when the source changes, and unsubscribes when detached or when a different source replaces it.

Incremental collection changes are UI-thread-only. An attached control rejects an off-thread `IObservableList.Changed` notification before it touches the presenter, retained queues, or UI properties. Marshal the complete mutation with `await root.Relay.InvokeAsync(() => items.Add(item), cancellationToken)`; the collection event itself is intentionally not auto-marshaled because the underlying mutable list is not thread-safe.

Item container generation is handled by `ItemContainerGenerator`. By default, non-`UIElement` items are wrapped in `ContentPresenter`. `UIElement` items are reused as their own containers unless `ItemTemplate` is set, in which case a `ContentPresenter` is used so the template can create the displayed child.

Changing `ItemTemplate`, `ItemTemplateKey`, `ItemsPanel`, `ItemsSource`, or `ContentTemplateRegistry` clears realized containers, marks presenter items dirty, and invalidates item-related layout/render state. `ItemsSourceProperty` also affects arrange, hit testing, and semantics.

`ContentTemplateRegistry` is assigned to generated `ContentPresenter` containers as `LocalTemplateRegistry`. An explicit `ItemTemplate` takes precedence over registry-based content templates.

Virtualization can be driven either by assigning a `VirtualizationContext` with `SetVirtualizationContext` or by calling `UpdateVirtualizationFromScrollInfo`. The presenter uses the resulting realization window to realize only the relevant item containers.

## Constructors

| Name | Description |
| --- | --- |
| `ItemsControl()` | Initializes `Items`, `ItemContainerGenerator`, and the owned `ItemsPresenter`. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `ItemTemplateProperty` | `UiProperty<ContentTemplate?>` | Identifies the `ItemTemplate` UI property. Default value is null. Affects measure and render. |
| `ItemsPanelProperty` | `UiProperty<ItemsPanelTemplate?>` | Identifies the `ItemsPanel` UI property. Default value is null. Affects measure and render. |
| `ItemTemplateKeyProperty` | `UiProperty<string?>` | Identifies the `ItemTemplateKey` UI property. Default value is null. Affects measure and render. |
| `ItemsSourceProperty` | `UiProperty<IEnumerable?>` | Identifies the `ItemsSource` UI property. Default value is null. Affects measure, arrange, render, hit testing, and semantics. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Items` | `ItemCollection` | Local retained item collection used when `ItemsSource` is null. |
| `ItemContainerGenerator` | `ItemContainerGenerator` | Generator that realizes, recycles, and tracks item containers for this control. |
| `ItemsPresenter` | `ItemsPresenter` | Presenter owned by this control and used to build the item panel. |
| `ItemsSource` | `IEnumerable?` | External item source. When non-null, it takes precedence over `Items`. |
| `ItemCount` | `int` | Number of items from the observable source, `ItemsSource`, or `Items`, in that order. |
| `ItemTemplate` | `ContentTemplate?` | Template used to create displayed content for each item. |
| `ItemTemplateKey` | `string?` | Optional key passed to `ContentPresenter` for registry-based template resolution. |
| `ContentTemplateRegistry` | `ContentTemplateRegistry` | Registry assigned to generated `ContentPresenter` containers for content template lookup. Cannot be set to null. |
| `ItemsPanel` | `ItemsPanelTemplate?` | Template used by the presenter to create the layout panel for item containers. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `SetItems(IEnumerable? items)` | `void` | Replaces the local `Items` collection with the supplied sequence. |
| `GetItemAt(int index)` | `object?` | Returns the item at `index` from the active item source. |
| `SetVirtualizationContext(VirtualizationContext? context)` | `void` | Assigns the presenter's virtualization context, marks items dirty, and invalidates item layout/render state. |
| `UpdateVirtualizationFromScrollInfo(IScrollInfo scrollInfo, float itemExtent, int cacheItems = 0)` | `void` | Updates virtualization from scroll metrics and invalidates items if the realization window changes. |

## Protected Members

| Name | Return Type | Description |
| --- | --- | --- |
| `DefaultContainerType` | `Type` | Returns the default item container type, `ContentPresenter`. |
| `GetContainerTypeForItem(object? item)` | `Type` | Returns the desired container type for an item. |
| `CreateItemContainer(int index, object? item)` | `UIElement` | Creates or returns the item container for an item. |
| `PrepareItemContainer(UIElement container, int index, object? item)` | `void` | Writes item metadata and applies content/template state to a container. |
| `ClearItemContainer(UIElement container)` | `void` | Clears metadata and content/template state from a container. |
| `IsItemSelected(int index)` | `bool` | Returns false by default; derived selector controls can override it. |
| `OnItemContainerPrepared(UIElement container, int index)` | `void` | Hook called after a container is realized by the presenter. |
| `MeasureCore(MeasureContext context)` | `LayoutSize` | Measures the template child through `Control` or measures the owned `ItemsPresenter`. |
| `ArrangeCore(ArrangeContext context)` | `LayoutRect` | Arranges the template child through `Control` or arranges the owned `ItemsPresenter`. |
| `OnPropertyChanged(UiPropertyChangedEventArgs args)` | `void` | Reacts to item policy and source changes by clearing containers and invalidating items. |
| `OnAttached()` | `void` | Subscribes to an observable item source when the control is attached. |
| `OnDetached()` | `void` | Unsubscribes from an observable item source before detaching. |

## Applies to

Cerneala retained UI controls.

## See also

- `ItemCollection`
- `ItemContainerGenerator`
- `ItemsPresenter`
- `ContentPresenter`
- `ContentTemplateRegistry`
- `VirtualizationContext`
