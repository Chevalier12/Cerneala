# VirtualizingStackPanel Class

## Definition
Namespace: `Cerneala.UI.Layout.Panels`

Assembly/Project: `Cerneala`

Source: `UI/Layout/Panels/VirtualizingStackPanel.cs`

Stacks visual children vertically while measuring and arranging only the children inside a `RealizationWindow`.

```csharp
public class VirtualizingStackPanel : Panel, IItemsVirtualizingPanel
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Panel` -> `VirtualizingStackPanel`

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

`VirtualizationContext` remains available for fixed-extent hosts such as specialized controls. Automatic variable-height virtualization starts with the first `UpdateViewport` call. When `VirtualizationContext` is `null` and `UpdateViewport` was never called, the panel realizes and stacks every child at its natural height, whatever the value of `FirstRealizedIndex`: `RealizationWindow` then covers the item indices from `FirstRealizedIndex` up to, but not including, `FirstRealizedIndex + VisualChildren.Count`. For example, with `FirstRealizedIndex = 29` and 10 children, the window is items 29 to 38, and all 10 children are measured. Item indices are never negative and stop at `int.MaxValue`, so `RealizationWindow` reports only the part of that range that is a valid item range, while every child is still measured and stacked. With `FirstRealizedIndex = -3` and 10 children, `RealizationWindow` is `(0, 7)`. With `FirstRealizedIndex = int.MaxValue - 5` and 10 children, `FirstRealizedIndex + VisualChildren.Count` does not fit in an `int`, and `RealizationWindow` is `(int.MaxValue - 5, int.MaxValue)`. In both cases the panel measures all 10 children and its desired height is the sum of their heights. When both are set, the realization window comes from `VirtualizationContext` while arrange positions use the automatic height estimates.

During measure, the panel maps each visual child to an item index by adding `FirstRealizedIndex` to the child's visual-child position. When `VirtualizationContext` is set or `UpdateViewport` was called, children whose item index is outside `RealizationWindow` are not measured and receive `LayoutSize.Zero` as their desired size. Otherwise every child is realized. Realized children are measured with the available width and an infinite available height.

When the panel stacks its children at their natural heights (no automatic viewport, and either no `VirtualizationContext` or one whose items are all realized), its desired height is the sum of the children's desired heights. With layout rounding enabled in the `MeasureContext`, that sum adds device pixels, as in `StackPanel` (see `LayoutRounding`): at scale 1.75, children with `Height = 32.5` (57 device pixels) and `Height = 0.5` (1 device pixel) give a desired height of `58 / 1.75f`, 58 device pixels, not 59.

During arrange, automatic mode positions each child using the measured or estimated prefix extent of all preceding items. Fixed mode uses `ItemExtent`. When every item is realized without virtualization, the panel stacks children using their measured heights.

In automatic mode, `TotalExtent` combines measured heights with the current estimate for unknown items. Without either automatic viewport data or a fixed `VirtualizationContext`, it reports the measured desired height.

A realized item's measured height of zero replaces any previously recorded positive height. This includes an item measured after `Height` becomes zero or `Visibility` becomes `Collapsed`; the item no longer contributes to `TotalExtent` or subsequent item offsets. Unknown items continue to use a positive estimate learned from positive measured heights. If no positive samples remain, the panel retains its previous estimate (initially 28) rather than treating unmeasured items as zero-height.

## Constructors

| Name | Description |
| --- | --- |
| `VirtualizingStackPanel()` | Initializes a new `VirtualizingStackPanel` instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `VirtualizationContext` | `VirtualizationContext?` | Gets or sets the item count, item extent, viewport extent, scroll offset, and cache size used to compute virtualization metrics. |
| `RealizationWindow` | `RealizationWindow` | Gets the active realized item-index range from fixed context, automatic viewport state, or all current children (`FirstRealizedIndex` to `FirstRealizedIndex + VisualChildren.Count`, limited to indices from 0 to `int.MaxValue`). |
| `TotalExtent` | `float` | Gets the measured, fixed, or variable-height estimated total extent. |
| `FirstRealizedIndex` | `int` | Gets or sets the item index represented by the first visual child in `VisualChildren`. |

## Layout Behavior

| Phase | Behavior |
| --- | --- |
| Measure | With a `VirtualizationContext` or an automatic viewport, measures only children whose computed item index is inside `RealizationWindow`; skipped children receive `LayoutSize.Zero`. Without either, measures every child. |
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
