# VirtualizingStackPanel Class

## Definition
Namespace: `Cerneala.UI.Layout.Panels`

Assembly/Project: `Cerneala`

Source: `UI/Layout/Panels/VirtualizingStackPanel.cs`

Stacks visual children vertically while measuring and arranging only the children inside a `RealizationWindow`.

```csharp
public class VirtualizingStackPanel : Panel
```

Inheritance:
`object` -> `UIElement` -> `Panel` -> `VirtualizingStackPanel`

## Examples

Use `VirtualizingStackPanel` as the items panel for an `ItemsControl` when the presenter supplies a virtualization context from scroll information.

```csharp
ItemsControl list = new()
{
    ItemsPanel = new ItemsPanelTemplate(() => new VirtualizingStackPanel())
};
```

The panel can also be configured directly in layout tests or custom presenters.

```csharp
VirtualizingStackPanel panel = new()
{
    VirtualizationContext = new VirtualizationContext(
        ItemCount: 100,
        ItemExtent: 24,
        ViewportExtent: 120,
        ScrollOffset: 48),
    FirstRealizedIndex = 2
};

panel.VisualChildren.Add(rowElement);
panel.Measure(new MeasureContext(new LayoutSize(300, 120)));
panel.Arrange(new ArrangeContext(new LayoutRect(0, 0, 300, 120)));
```

## Remarks

`VirtualizingStackPanel` is intended for item presenters that realize only a subset of an item collection. When an `ItemsPresenter` applies virtualization to this panel, it assigns `VirtualizationContext` and sets `FirstRealizedIndex` to the start of the current realization window.

During measure, the panel maps each visual child to an item index by adding `FirstRealizedIndex` to the child's visual-child position. Children whose item index is outside `RealizationWindow` are not measured and receive `LayoutSize.Zero` as their desired size. Realized children are measured with the available width and an infinite available height.

During arrange, unrealized children are arranged into zero-sized bounds. While only part of the collection is realized, a finite positive `ItemExtent` positions realized children in content coordinates at `FinalRect.Y + itemIndex * ItemExtent` and supplies their arranged height. When every item is realized, the panel instead stacks children using their measured heights so an auto-sized host reflects the actual item content.

Without a `VirtualizationContext`, or when every item is realized, `TotalExtent` reports the panel's measured desired height. Partially realized collections continue to use the estimated total extent from `VirtualizationContext`.

## Constructors

| Name | Description |
| --- | --- |
| `VirtualizingStackPanel()` | Initializes a new `VirtualizingStackPanel` instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `VirtualizationContext` | `VirtualizationContext?` | Gets or sets the item count, item extent, viewport extent, scroll offset, and cache size used to compute virtualization metrics. |
| `RealizationWindow` | `RealizationWindow` | Gets the active realized item-index range. Uses `VirtualizationContext.GetRealizationWindow()` when a context exists; otherwise covers all current visual children. |
| `TotalExtent` | `float` | Gets the measured natural height when every item is realized; otherwise gets the estimated total extent from `VirtualizationContext`. |
| `FirstRealizedIndex` | `int` | Gets or sets the item index represented by the first visual child in `VisualChildren`. |

## Layout Behavior

| Phase | Behavior |
| --- | --- |
| Measure | Measures only children whose computed item index is inside `RealizationWindow`; skipped children receive `LayoutSize.Zero`. |
| Arrange | Arranges skipped children into zero-sized bounds and arranges realized children vertically. |
| Partially realized extent | Uses `VirtualizationContext.ItemExtent` for child height and content-coordinate Y placement when it is finite and greater than zero. |
| Fully realized extent | Uses each child's `DesiredSize.Height`, allowing an auto-sized host to fit all realized content up to its external size constraint. |
| Variable item extent fallback | Uses each child's `DesiredSize.Height` and stacks realized children sequentially when no valid fixed item extent is available. |

## Applies to

`Cerneala` UI layout panels.

## See also

- `Panel`
- `ItemsPresenter`
- `ItemsPanelTemplate`
- `VirtualizationContext`
- `RealizationWindow`
