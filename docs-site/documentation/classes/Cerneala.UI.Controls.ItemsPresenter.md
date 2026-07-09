# ItemsPresenter Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/ItemsPresenter.cs`

Materializes an item sequence into child elements hosted by an items panel.

```csharp
public class ItemsPresenter : Control
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Control` -> `ItemsPresenter`

## Examples

Create a standalone presenter that materializes string items through a data template:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

ItemsPresenter presenter = new()
{
    Items = new[] { "one", "two" },
    ItemTemplate = new DataTemplate<string>(value => new RowElement(value))
};

presenter.Measure(new MeasureContext(new LayoutSize(100, 100)));

UIElement firstChild = presenter.PanelRoot!.VisualChildren[0];

public sealed class RowElement(string value) : UIElement
{
    public string Value { get; } = value;
}
```

Use a virtualizing panel and a fixed realization context:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;
using Cerneala.UI.Layout.Virtualization;

ItemsPresenter presenter = new()
{
    Items = new[] { "zero", "one", "two", "three", "four" },
    ItemTemplate = new DataTemplate<string>(value => new TextBlock { Text = value }),
    ItemsPanel = new ItemsPanelTemplate(() => new VirtualizingStackPanel()),
    VirtualizationContext = new VirtualizationContext(
        ItemCount: 5,
        ItemExtent: 10,
        ViewportExtent: 20,
        ScrollOffset: 20)
};

presenter.Measure(new MeasureContext(new LayoutSize(100, 20)));

RealizationWindow window = presenter.CurrentRealizationWindow; // StartIndex: 2, EndIndexExclusive: 4
```

## Remarks

`ItemsPresenter` is the realization surface used by `ItemsControl`, and it can also be used directly. In direct mode, it reads `Items`, creates one child per realized item, and either hosts `UIElement` items directly or asks `ItemTemplate` to create an element for non-element data items. Items that are not `UIElement` instances and do not have a matching `ItemTemplate` do not produce children.

The presenter owns a single panel root. If `ItemsPanel` is set, that template creates the panel; otherwise the presenter uses an internal default template that creates `Cerneala.UI.Controls.Panel`. `PanelRoot` returns the panel only when it is the controls-facing `Panel` type. `LayoutPanelRoot` exposes the underlying `Cerneala.UI.Layout.Panels.Panel`, which is useful for custom panels such as `VirtualizingStackPanel`.

When `ItemsOwner` is set, the presenter delegates item realization to the owner's `ItemContainerGenerator`. In that mode, `ItemsControl` owns item source selection, container preparation, item templates, content template keys, selection state, and recycling policy. The presenter's `ItemsPanel` overrides the owner's `ItemsPanel`; if it is not set, the owner panel template is used before the default panel.

Changing `Items`, `ItemTemplate`, or `ItemsPanel` marks the presentation dirty and refreshes the panel content. `MarkItemsDirty()` explicitly marks the presenter dirty, increments layout and render versions, and invalidates measure, arrange, render, and hit testing.

Virtualization is driven by `VirtualizationContext`. The current realization window is cached in `CurrentRealizationWindow`; when no context has produced a window, it returns `RealizationWindow.Empty`. `UpdateVirtualizationFromScrollInfo` creates a new context from `IScrollInfo.ViewportHeight`, `IScrollInfo.VerticalOffset`, item extent, item count, and optional cache size. When the window or virtualization shape changes, the presenter marks items dirty. If the active panel is a `VirtualizingStackPanel`, the presenter also applies the context and first realized index to that panel.

`MeasureCore` and `ArrangeCore` refresh dirty items before layout. During measure, the presenter also processes inherited properties and aspects for the realized subtree and removes completed layout work from the root queues.

## Constructors

| Name | Description |
| --- | --- |
| `ItemsPresenter()` | Creates an items presenter with no items, no item template, no explicit items panel, and an empty realization window. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `ItemsProperty` | `UiProperty<IEnumerable?>` | Identifies the `Items` UI property. The default value is `null`; metadata affects measure and render. |
| `ItemTemplateProperty` | `UiProperty<DataTemplate?>` | Identifies the `ItemTemplate` UI property. The default value is `null`; metadata affects measure and render. |
| `ItemsPanelProperty` | `UiProperty<ItemsPanelTemplate?>` | Identifies the `ItemsPanel` UI property. The default value is `null`; metadata affects measure and render. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Items` | `IEnumerable?` | Gets or sets the standalone item sequence to materialize when `ItemsOwner` is not set. |
| `ItemTemplate` | `DataTemplate?` | Gets or sets the template used to create child elements for non-`UIElement` standalone items. |
| `ItemsPanel` | `ItemsPanelTemplate?` | Gets or sets the panel template used for the presenter's panel root. Overrides the owner panel template when `ItemsOwner` is set. |
| `PanelRoot` | `Panel?` | Gets the current panel root as `Cerneala.UI.Controls.Panel`, or `null` when the root is another layout panel type. |
| `LayoutPanelRoot` | `Cerneala.UI.Layout.Panels.Panel?` | Gets the current underlying layout panel root, including custom panel types. |
| `ItemsOwner` | `ItemsControl?` | Gets or sets the owning `ItemsControl`. When set, owner item container generation is used. |
| `VirtualizationContext` | `VirtualizationContext?` | Gets or sets the context used to compute the realized item window. |
| `CurrentRealizationWindow` | `RealizationWindow` | Gets the last computed realization window, or `RealizationWindow.Empty` before one is available. |

## Methods

| Name | Description |
| --- | --- |
| `MarkItemsDirty()` | Marks realized items dirty, increments layout and render versions, and invalidates measure, arrange, render, and hit testing. |
| `UpdateVirtualizationFromScrollInfo(IScrollInfo, float, int)` | Updates `VirtualizationContext` from scroll information, item extent, and optional cached item count; throws `ArgumentNullException` when `scrollInfo` is `null`. |

## Protected Methods

| Name | Description |
| --- | --- |
| `MeasureCore(MeasureContext)` | Refreshes realized items, measures the panel root, processes inherited and aspect state for the realized subtree, and returns the desired size. |
| `ArrangeCore(ArrangeContext)` | Refreshes realized items, arranges the panel root, removes completed arrange work for the subtree, and returns the final layout rectangle. |
| `OnPropertyChanged(UiPropertyChangedEventArgs)` | Marks items dirty and refreshes presentation when `Items`, `ItemTemplate`, or `ItemsPanel` changes. |

## Applies To

`Cerneala` retained UI controls, item presentation, item container generation, and layout virtualization.

## See Also

- `Cerneala.UI.Controls.ItemsControl`
- `Cerneala.UI.Controls.Items.ItemContainerGenerator`
- `Cerneala.UI.Controls.Items.ItemsPanelTemplate`
- `Cerneala.UI.Controls.Templates.DataTemplate`
- `Cerneala.UI.Layout.Panels.VirtualizingStackPanel`
- `Cerneala.UI.Layout.Virtualization.VirtualizationContext`
- `Cerneala.UI.Layout.Virtualization.RealizationWindow`
