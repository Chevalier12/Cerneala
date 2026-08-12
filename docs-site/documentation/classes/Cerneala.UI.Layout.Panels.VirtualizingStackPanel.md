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

Use `VirtualizingStackPanel` as the direct items panel for automatic variable-height virtualization inside a `ScrollViewer`.

```csharp
ItemsControl list = new()
{
    ItemsPanel = new VirtualizingStackPanel()
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

`VirtualizingStackPanel` implements `IItemsVirtualizingPanel`. An owning `ItemsControl` receives viewport and offset updates from its `ScrollViewer` and forwards them to the panel. The panel estimates unknown row heights, records actual heights during measure, updates its average estimate, and corrects the realization window as measurements become available.

`VirtualizationContext` remains available for fixed-extent hosts such as specialized controls. Automatic variable-height virtualization is used when that property is `null`.

During measure, the panel maps each visual child to an item index by adding `FirstRealizedIndex` to the child's visual-child position. Children whose item index is outside `RealizationWindow` are not measured and receive `LayoutSize.Zero` as their desired size. Realized children are measured with the available width and an infinite available height.

During arrange, automatic mode positions each child using the measured or estimated prefix extent of all preceding items. Fixed mode uses `ItemExtent`. When every item is realized without virtualization, the panel stacks children using their measured heights.

In automatic mode, `TotalExtent` combines measured heights with the current estimate for unknown items. Without either automatic viewport data or a fixed `VirtualizationContext`, it reports the measured desired height.

## Constructors

| Name | Description |
| --- | --- |
| `VirtualizingStackPanel()` | Initializes a new `VirtualizingStackPanel` instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `VirtualizationContext` | `VirtualizationContext?` | Gets or sets the item count, item extent, viewport extent, scroll offset, and cache size used to compute virtualization metrics. |
| `RealizationWindow` | `RealizationWindow` | Gets the active realized item-index range from fixed context, automatic viewport state, or all current children. |
| `TotalExtent` | `float` | Gets the measured, fixed, or variable-height estimated total extent. |
| `FirstRealizedIndex` | `int` | Gets or sets the item index represented by the first visual child in `VisualChildren`. |

## Layout Behavior

| Phase | Behavior |
| --- | --- |
| Measure | Measures only children whose computed item index is inside `RealizationWindow`; skipped children receive `LayoutSize.Zero`. |
| Arrange | Arranges skipped children into zero-sized bounds and arranges realized children vertically. |
| Partially realized extent | Uses `VirtualizationContext.ItemExtent` for child height and content-coordinate Y placement when it is finite and greater than zero. |
| Fully realized extent | Uses each child's `DesiredSize.Height`, allowing an auto-sized host to fit all realized content up to its external size constraint. |
| Variable item extent | Learns realized heights and estimates unknown items, then corrects offsets and the realization window. |

## Applies to

`Cerneala` UI layout panels.

## See also

- `Panel`
- `ItemsPresenter`
- `IItemsVirtualizingPanel`
- `VirtualizationContext`
- `RealizationWindow`
