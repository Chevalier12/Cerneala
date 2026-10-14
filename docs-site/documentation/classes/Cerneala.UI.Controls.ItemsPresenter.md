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
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

ItemsPresenter presenter = new()
{
    Items = new[] { "one", "two" },
    ItemTemplate = new ContentTemplate<string>("Row", key: null, priority: 0, context => new RowElement(context.Data!))
};

presenter.Measure(new MeasureContext(new LayoutSize(100, 100)));

// The default panel is a Cerneala.UI.Controls.StackPanel, so read it through LayoutPanelRoot;
// PanelRoot is null here because that panel is not a Cerneala.UI.Controls.Panel.
UIElement firstChild = presenter.LayoutPanelRoot!.VisualChildren[0];

public sealed class RowElement(string value) : UIElement
{
    public string Value { get; } = value;
}
```

Use a virtualizing panel and a fixed realization context:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;
using Cerneala.UI.Layout.Virtualization;

ItemsPresenter presenter = new()
{
    Items = new[] { "zero", "one", "two", "three", "four" },
    ItemTemplate = new ContentTemplate<string>("Text", key: null, priority: 0, context => new TextBlock { Text = context.Data ?? string.Empty }),
    ItemsPanel = new VirtualizingStackPanel(),
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

`ItemsPresenter` is the realization surface used by `ItemsControl`, and it can also be used directly. In direct mode, it reads `Items`, creates one child per realized item, and either hosts `UIElement` items directly or asks `ItemTemplate` to create an element for non-element data items. The single `ItemTemplate` is used for every non-element item; no data-type or key matching happens in direct mode. A non-element item produces no child when `ItemTemplate` is `null` or when the template's factory returns `null`.

The presenter owns a single retained panel root. If `ItemsPanel` is set, that panel is used directly and retained across item refreshes; otherwise the presenter uses an internal vertical `Cerneala.UI.Controls.StackPanel`. `PanelRoot` returns the panel only when it is the controls-facing `Panel` type. `LayoutPanelRoot` exposes the underlying `Cerneala.UI.Layout.Panels.Panel`, including custom panels such as `VirtualizingStackPanel`.

When `ItemsOwner` is set, the presenter delegates item realization to the owner's container generator. In that mode, `ItemsControl` owns item source selection, container preparation, item templates, content template keys, selection state, and recycling policy. The presenter's `ItemsPanel` overrides the owner's panel; if it is not set, the owner panel is used before the default panel.

Changing `Items`, `ItemTemplate`, or `ItemsPanel` marks the presentation dirty and refreshes the panel content. When the realization window moves, the presenter synchronizes children by reference: overlapping containers remain attached, exiting containers are removed, and entering containers are inserted or moved into order. `MarkItemsDirty()` explicitly marks the presenter dirty, increments layout and render versions, and invalidates measure, arrange, render, and hit testing.

Fixed-extent virtualization can be driven explicitly through `VirtualizationContext`. When owned by an `ItemsControl`, a panel implementing `IItemsVirtualizingPanel` instead receives automatic viewport updates from the surrounding `ScrollViewer`; `VirtualizingStackPanel` uses that path for variable-height items.

When the presenter refreshes items with a non-null `VirtualizationContext` and its resolved panel is a `VirtualizingStackPanel`, it copies the context to that panel's `VirtualizationContext`. The panel keeps that copy only while the presenter supplies it. Setting the presenter's `VirtualizationContext` to `null` immediately sets the panel's `VirtualizationContext` to `null` too, and the panel returns to its own realization: it follows an automatic viewport when one was pushed, and otherwise realizes and stacks every item. For example, a presenter with 100 items and the context `new VirtualizationContext(100, 10, 30, 0, CacheItems: 1)` realizes 4 children in a `VirtualizingStackPanel` with no `ScrollViewer`; after `VirtualizationContext = null`, `MarkItemsDirty()`, and the next measure, the panel's `VirtualizationContext` is `null` and it realizes all 100. When the presenter copies its context to a different `VirtualizingStackPanel`, for example after `ItemsPanel` changes, the previous panel's copy is cleared in the same way. A context assigned directly to a panel that the presenter never wrote to is left unchanged.

`MeasureCore` and `ArrangeCore` refresh dirty items before layout. During measure, the presenter also processes inherited properties and aspects for the realized subtree and removes completed layout work from the root queues. When the refresh in `ArrangeCore` changes the realized items, for example because a `ScrollViewer` clamps its offset during arrange after the item source shrank, the presenter first measures the panel root again with the panel's last available size, so newly realized items are arranged with their measured size instead of an empty one.

## Constructors

| Name | Description |
| --- | --- |
| `ItemsPresenter()` | Creates an items presenter with no items, no item template, a vertical stack panel fallback, and an empty realization window. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `ItemsProperty` | `UiProperty<IEnumerable?>` | Identifies the `Items` UI property. The default value is `null`; metadata affects measure and render. |
| `ItemTemplateProperty` | `UiProperty<ContentTemplate?>` | Identifies the `ItemTemplate` UI property. The default value is `null`; metadata affects measure and render. |
| `ItemsPanelProperty` | `UiProperty<Panel?>` | Identifies the retained `ItemsPanel` UI property. The default value is `null`; metadata affects layout and render. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Items` | `IEnumerable?` | Gets or sets the standalone item sequence to materialize when `ItemsOwner` is not set. |
| `ItemTemplate` | `ContentTemplate?` | Gets or sets the template used to create child elements for non-`UIElement` standalone items. |
| `ItemsPanel` | `Panel?` | Gets or sets the retained panel root. Overrides the owner panel when `ItemsOwner` is set. |
| `PanelRoot` | `Panel?` | Gets the current panel root as `Cerneala.UI.Controls.Panel`, or `null` when the root is another layout panel type. |
| `LayoutPanelRoot` | `Cerneala.UI.Layout.Panels.Panel?` | Gets the current underlying layout panel root, including custom panel types. |
| `ItemsOwner` | `ItemsControl?` | Gets or sets the owning `ItemsControl`. When set, owner item container generation is used. |
| `VirtualizationContext` | `VirtualizationContext?` | Gets or sets the context used to compute the realized item window. Setting `null` also clears the copy this presenter wrote to its `VirtualizingStackPanel`. |
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
| `ArrangeCore(ArrangeContext)` | Refreshes realized items, measures the panel root again with its last available size when the refresh changed the realized items, arranges the panel root, removes completed arrange work for the subtree, and returns the final layout rectangle. |
| `OnPropertyChanged(UiPropertyChangedEventArgs)` | Marks items dirty and refreshes presentation when `Items`, `ItemTemplate`, or `ItemsPanel` changes. |

## Applies To

`Cerneala` retained UI controls, item presentation, item container generation, and layout virtualization.

## See Also

- `Cerneala.UI.Controls.ItemsControl`
- `Cerneala.UI.Controls.Items.ItemContainerGenerator`
- `Cerneala.UI.Layout.Virtualization.IItemsVirtualizingPanel`
- `Cerneala.UI.Controls.Templates.ContentTemplate`
- `Cerneala.UI.Layout.Panels.VirtualizingStackPanel`
- `Cerneala.UI.Layout.Virtualization.VirtualizationContext`
- `Cerneala.UI.Layout.Virtualization.RealizationWindow`
