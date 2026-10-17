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

During measure, the panel maps each visual child to an item index by adding `FirstRealizedIndex` to the child's visual-child position. When `VirtualizationContext` is set or `UpdateViewport` was called, children whose item index is outside `RealizationWindow` are not measured and receive `LayoutSize.Zero` as their desired size. Otherwise every child is realized. Realized children are measured with the available width and an infinite available height. This also holds in a `UIRoot` frame for a panel used directly, without `ItemsPresenter`: the frame does not measure such a child or its descendants on its own, even when the child was just added or one of its own measure-affecting properties changed. For example, after `UpdateViewport(new ItemsVirtualizationViewport(30, 400, 0, 0))` and 30 added children of height 28, `ProcessFrame()` runs `MeasureCore` only for items 0 to 14; setting `Height` on item 20 afterwards does not measure it either. The panel measures it once it enters `RealizationWindow`. The `LayoutSize.Zero` of a skipped child is not a measure result: when the child is realized again, its measure cache does not return that zero, even for the same available size as before, and the child reports its real desired size. For example, a row with `Height = 40` that was skipped while the window was items 3 to 4 desires height 40 again, not 0, once the window returns to items 0 to 1.

With an automatic viewport, the heights measured during a measure update the estimate, and the panel recalculates `RealizationWindow` before the measure ends. When the corrected window contains children that this measure has not measured yet, the panel measures them and corrects the window again, until a round adds no child. Arrange therefore never places a realized child that was not measured, and a caller that uses the panel directly needs only one `Measure` and `Arrange` after `UpdateViewport`. A child that this measure measured before the corrected window moved past it keeps its measured desired size; the next measure in which it is still outside the window gives it `LayoutSize.Zero`. For example, with 30 children that are 50 pixels tall, `UpdateViewport(new ItemsVirtualizationViewport(30, 400, 280, 0))` first realizes items 10 to 25 with the initial estimate of 28. Their measured height of 50 moves the window to items 5 to 13, so the same measure also measures items 5 to 9. After `Arrange`, items 5 to 13 are arranged at Y 250, 300, …, 650 with height 50, and items 14 to 24 keep a desired height of 50.

`UpdateViewport` recalculates `RealizationWindow` immediately. When the new viewport changes `RealizationWindow` or the item count, `UpdateViewport` also invalidates the panel's measure and arrange caches, so the next `Measure` runs even with the same available size and measures the children that entered the window. A new `ScrollOffset` that keeps the same window and item count keeps both caches, so a `Measure` and `Arrange` with the same size and rectangle as before do no work. `UpdateViewport` does not schedule a layout pass: a caller that uses the panel directly calls `Measure` and `Arrange` itself, and inside an `ItemsControl` the `ItemsPresenter` invalidates its own layout when the window changes. For example, with 30 children that are 50 pixels tall, `UpdateViewport(new ItemsVirtualizationViewport(30, 400, 280, 0))` followed by `Measure` with `new LayoutSize(100, 400)` and `Arrange` realizes items 5 to 13. `UpdateViewport(new ItemsVirtualizationViewport(30, 400, 0, 0))` moves the window to items 0 to 7, the next `Measure` with the same `new LayoutSize(100, 400)` measures items 0 to 4, and `Arrange` places items 0 to 7 at Y 0, 50, …, 350 with height 50. Scroll offsets 10 and then 20 with the same viewport both realize items 0 to 8, so the `Measure` after the second `UpdateViewport` returns the cached desired size without measuring any child.

When the panel stacks its children at their natural heights (no automatic viewport, and either no `VirtualizationContext` or one whose items are all realized), its desired height is the sum of the children's desired heights. With layout rounding enabled in the `MeasureContext`, that sum adds device pixels, as in `StackPanel` (see `LayoutRounding`): at scale 1.75, children with `Height = 32.5` (57 device pixels) and `Height = 0.5` (1 device pixel) give a desired height of `58 / 1.75f`, 58 device pixels, not 59.

During arrange, a skipped child (outside `RealizationWindow` while `VirtualizationContext` is set or `UpdateViewport` was called) is arranged into zero-sized bounds at the panel's origin. Zero-sized bounds do not clip the child's content, so the panel also stops the skipped child from taking part in rendering, input, and hit testing (see `UIElementVisibility`) until an arrange realizes it again. For example, with 30 children `Border { Height = 40, Child = new TextBlock { Text = "Row N" } }` and `UpdateViewport(new ItemsVirtualizationViewport(30, 200, 0, 0))`, `RealizationWindow` is `(0, 5)`: the panel draws `Row 0` to `Row 4`, and nothing of `Row 5` to `Row 29`. Removing a skipped child that sets `UIElement.Presence` detaches it at once, without exit motion, because it has nothing visible to animate out: after `panel.VisualChildren.Remove(panel.VisualChildren[20])` the panel still draws only `Row 0` to `Row 4`, and `PresenceCoordinator.GetState` reports `Detached` for row 20. A realized child that is removed still plays its exit. An owning `ItemsPresenter` keeps only realized containers as children, so it is not affected.

During arrange, automatic mode positions each child using the measured or estimated prefix extent of all preceding items. Fixed mode uses `ItemExtent`. When every item is realized without virtualization, the panel stacks children using their measured heights.

In automatic mode, `TotalExtent` combines measured heights with the current estimate for unknown items. The desired height in automatic mode is the same total extent. Without either automatic viewport data or a fixed `VirtualizationContext`, it reports the measured desired height.

With layout rounding enabled, the automatic-mode total extent and the arrange offset of each item also add device pixels (see `LayoutRounding`): a measured height on a device pixel counts as its whole number of device pixels, and any other height, such as the average estimate used for an unknown item, counts as `height * Scale`. The sum is divided by `Scale`. `TotalExtent` uses the rounding of the panel's last measure. For example, at scale 1.75, after `UpdateViewport(new ItemsVirtualizationViewport(2, 200, 0, 0))` with children `Height = 32.5` and `Height = 0.5`, the children are arranged at device pixels 0..57 and 57..58, and both `TotalExtent` and the desired height are `58 / 1.75f` (58 device pixels), the same result as without `UpdateViewport`.

The automatic realization window adds the item extents the same way, with the rounding of the panel's last measure, so `TotalExtent`, the arrange offsets and `RealizationWindow` agree: the scroll offset `TotalExtent - ViewportExtent` realizes the last items, and the last item ends at `TotalExtent`. The panel adds the device pixels in double precision and converts the sum to `float` once, so a long list does not drift. For example, with 1,000,000 items of height 33 at scale 1 and a viewport of 330, `TotalExtent` is 33,000,000, the scroll offset 32,999,670 realizes items 999,990 to 999,999, and the last item ends at 33,000,000 within the precision of a `float` at that size (2 pixels between 2^24 and 2^25).

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

## Methods

| Name | Description |
| --- | --- |
| `UpdateViewport(ItemsVirtualizationViewport)` | Starts or updates automatic variable-height virtualization with the item count, viewport extent, scroll offset, and cache size, and recalculates `RealizationWindow`. Invalidates the panel's measure and arrange caches when the window or the item count changes; does not schedule layout. |

## Layout Behavior

| Phase | Behavior |
| --- | --- |
| Measure | With a `VirtualizationContext` or an automatic viewport, measures only children whose computed item index is inside `RealizationWindow`; skipped children receive `LayoutSize.Zero`. With an automatic viewport, also measures the children that the window corrected by the measured heights adds, until the window adds none. Without either, measures every child. |
| Arrange | Arranges skipped children into zero-sized bounds, excludes them from rendering, input, and hit testing until they are realized again, and arranges realized children vertically. |
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
- `UIElementVisibility`
