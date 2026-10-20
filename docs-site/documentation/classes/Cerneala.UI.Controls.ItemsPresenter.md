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

Setting `ItemsOwner` to a different value, including `null`, immediately gives back what the presenter realized for the previous owner: it removes every child from its panel root, removes the panel root from the presenter, and clears the `VirtualizationContext` copy it wrote to a `VirtualizingStackPanel`. The presenter's own `VirtualizationContext` value is kept, and the next measure realizes items again for the new owner. Setting the same owner again changes nothing. This lets another presenter of the same `ItemsControl` host the owner's panel and containers. For example, a `ComboBox` uses one shared `VirtualizingStackPanel` for its template presenter and its fallback presenter; after the drop-down (`MaxDropDownHeight = 120`) realized only the visible items of 200 and `ComponentTemplate` is set to `null`, the template presenter's `ItemsOwner` becomes `null`, the panel has no parent, no children, and a `null` `VirtualizationContext`, and the next frame hosts it in the fallback presenter with all 200 items. The containers that the previous owner keeps realized stay realized, so the next presenter reuses them. A previous container that a failed refresh kept in the panel although no index takes it any more (see the failed-refresh paragraph below) represents no item, so the same setter also gives it back to the previous owner's generator at once: the owner's `ClearItemContainer` runs for it and a generated container goes to `RecyclePool`. It is not detached again, because it already left the panel, so an element that is no longer an item keeps a parent that user code gives it afterwards. For example, an `ItemsControl` with a `StackPanel` shows the `UIElement` `x`, which is its own container, and `"1"`. After `source[0] = "0b"`, a measure in which `OnItemContainerPrepared` throws keeps `x` in the panel. Setting `ItemsOwner = null` removes `x` from the panel; when user code then adds `x` to another panel and sets `ItemsOwner` back, the next measure shows `"0b"` and `"1"`, and `x` stays the visual and logical child of the other panel.

Changing `Items`, `ItemTemplate`, or `ItemsPanel` marks the presentation dirty and refreshes the panel content. Setting `VirtualizationContext` to a different value, including `null`, calls `MarkItemsDirty()` too, like an `AffectsMeasure` property, so the next layout realizes the new window without another call; setting an equal value does nothing. For example, an `ItemsControl` with 60 rows inside a `ScrollViewer` realizes all 60; after its presenter's `VirtualizationContext = new VirtualizationContext(60, 20, 200, 0, 0)`, the next frame realizes rows `0` to `9`, and the presenter's `DesiredSize`, its panel's `DesiredSize`, and the viewer's `ExtentHeight` are the height of those 10 rows. When the realization window moves, the presenter synchronizes children by reference: overlapping containers remain attached, exiting containers are removed, and entering containers are inserted or moved into order. `MarkItemsDirty()` explicitly marks the presenter dirty, increments layout and render versions, and invalidates measure, arrange, render, and hit testing.

Fixed-extent virtualization can be driven explicitly through `VirtualizationContext`, which wins over the automatic viewports below. When owned by an `ItemsControl`, a panel implementing `IItemsVirtualizingPanel` instead receives automatic viewport updates from a `ScrollViewer`; `VirtualizingStackPanel` uses that path for variable-height items. As in WPF, two shapes give the viewport:

- The `ScrollViewer` whose content is the owning `ItemsControl`. The viewport starts below the control's top `BorderThickness` and `Padding`, where the rows start.
- The `ScrollViewer` whose content is this presenter, such as `PART_ScrollViewer` of the `ListBox` template, while that `ScrollViewer` was measured with a finite height. A `ScrollContentPresenter` that is not in a `ScrollViewer` uses its own available height. When both shapes apply, this one wins; when it stops, for example because the `ScrollViewer` is measured with an unbounded height, the first shape applies again.

```csharp
// First shape: the viewer's content is the ItemsControl.
ScrollViewer viewer = new()
{
    Height = 200,
    Content = new ItemsControl { ItemsPanel = new VirtualizingStackPanel() }
};

// Second shape: the ListBox template's ScrollViewer has the ItemsPresenter as content.
ListBox listBox = new() { Height = 200, ItemsPanel = new VirtualizingStackPanel() };
```

With 2,000 rows 20 pixels high, the 200-pixel viewport shows 10 rows, and the `ListBox` above realizes 11 to 13 rows while the user scrolls with the mouse wheel and presses End, instead of all 2,000.

When the presenter refreshes items with a non-null `VirtualizationContext` and its resolved panel is a `VirtualizingStackPanel`, it copies the context to that panel's `VirtualizationContext`. The panel keeps that copy only while the presenter supplies it. Setting the presenter's `VirtualizationContext` to `null` immediately sets the panel's `VirtualizationContext` to `null` too, and the panel returns to its own realization: it follows an automatic viewport when one was pushed, and otherwise realizes and stacks every item. For example, a presenter with 100 items and the context `new VirtualizationContext(100, 10, 30, 0, CacheItems: 1)` realizes 4 children in a `VirtualizingStackPanel` with no `ScrollViewer`; after `VirtualizationContext = null` and the next measure, the panel's `VirtualizationContext` is `null` and it realizes all 100. When the resolved panel changes, for example after `ItemsPanel` changes, the next refresh clears the previous panel's copy in the same way, whether the new panel is another `VirtualizingStackPanel` or a non-virtualizing panel such as `StackPanel`. For example, after the presenter above changes `ItemsPanel` from that `VirtualizingStackPanel` to a `StackPanel` and measures again, the old panel's `VirtualizationContext` is `null`, and the old panel placed in another presenter with no context and 2000 px of height realizes all 100 items. A context assigned directly to a panel that the presenter never wrote to is left unchanged.

`MeasureCore` and `ArrangeCore` refresh dirty items before layout. During measure, the presenter also processes inherited properties and aspects for the realized subtree and removes completed layout work from the root queues. It completes that work the way the frame scheduler does: each element whose queued measure or arrange entry it removes also loses the matching `Measure` or `Arrange` dirty flag, and the specialized flags such as `Semantics` once no concrete work flag remains. For example, a `UIRoot` with a `StackPanel` that holds an `ItemsControl` with one string item has no element with dirty flags once `Scheduler.HasWork` is `false`, after the first layout and after `Invalidate(InvalidationFlags.Measure, ...)` on the `ItemsControl`. When the refresh in `ArrangeCore` changes the realized items, for example because a `ScrollViewer` clamps its offset during arrange after the item source shrank, the presenter first measures the panel root again with the panel's last available size, so newly realized items are arranged with their measured size instead of an empty one.

A refresh that throws, for example because the `ItemTemplate` factory or the owner's container preparation throws, rethrows the exception from the setter, `MeasureCore`, or `ArrangeCore` that started it. The presenter creates the new children before it removes the old ones, so it keeps the previous panel root and its children. It also calls `MarkItemsDirty()`, so the presentation stays dirty and the next measure or arrange retries the refresh. For example, a presenter in a `UIRoot` shows `"one"` and `"two"`; setting `Items = new[] { "x", "y", "z" }` while the template throws raises the exception and keeps `"one"` and `"two"`; each later `ProcessFrame()` throws again while the template still throws, and the first `ProcessFrame()` after the template stops throwing shows `"x"`, `"y"`, and `"z"` without an explicit `MarkItemsDirty()` call. A failed `ItemsPanel` change works the same way: the old panel stays the `LayoutPanelRoot` until a later refresh succeeds and places the new panel. When `ItemsOwner` is set, the owner's containers outside the new realization window are recycled only after the new window is placed in the panel and the panel received its `VirtualizationContext`, so a `PrepareItemContainer` or `OnItemContainerPrepared` override that throws also leaves the previous containers realized, prepared for their items, and in the panel. For example, an `ItemsControl` with 100 items, a `VirtualizingStackPanel`, and the context `new VirtualizationContext(100, 10, 30, 0, CacheItems: 1)` shows the containers of items `0` to `3`. After the context changes to scroll offset `20` and `PrepareItemContainer` throws for item `4`, each measure throws, and the panel still holds the containers of items `0`, `1`, `2`, and `3` with `FirstRealizedIndex` `0` and the previous `VirtualizationContext`. The first measure after the override stops throwing shows items `1` to `5` with `FirstRealizedIndex` `1`. A previous container that an index of the new window no longer takes is kept the same way, until a refresh succeeds or `ItemsOwner` changes: for example the `ContentPresenter` of a replaced item whose new item is a `UIElement` that is its own container, or the `ContentPresenter` at the index that such an element moves to. It leaves `ItemContainerGenerator.RealizedContainers`, but it is not cleared, not detached, and not in `RecyclePool`. For example, an `ItemsControl` with a `StackPanel` shows `"0"`, `"1"`, `"2"`, and `"3"`. After `source[2] = element` and `source[3] = "3b"`, with a `PrepareItemContainer` override that throws for `"3b"`, each measure throws, the panel still holds the containers of `"0"`, `"1"`, `"2"`, and `"3"` in that order, and `RecyclePool.Count` is `0`. The first measure after the override stops throwing shows `"0"`, `"1"`, `element`, and `"3b"`, and the container that showed `"2"` is then cleared and in `RecyclePool`. A `ClearItemContainer` override that throws while those outside containers are recycled is different, because the new window is already placed: the measure throws, the panel keeps items `1` to `5` with `FirstRealizedIndex` `1`, and a later refresh that fails while it places its children restores items `1` to `5`, not items `0` to `3`.

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
| `ItemsOwner` | `ItemsControl?` | Gets or sets the owning `ItemsControl`. When set, owner item container generation is used. Changing it removes the realized children and the panel root, clears the panel's `VirtualizationContext` copy, and gives back to the previous owner's generator, without detaching them, the containers a failed refresh kept in the panel for no index. |
| `VirtualizationContext` | `VirtualizationContext?` | Gets or sets the context used to compute the realized item window. Setting a different value marks the items dirty and invalidates layout, as `MarkItemsDirty()` does; setting an equal value does nothing. Setting `null` also clears the copy this presenter wrote to its `VirtualizingStackPanel`. |
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
