# VirtualizationContext Struct

## Definition
Namespace: `Cerneala.UI.Layout.Virtualization`

Assembly/Project: `Cerneala`

Source: `UI/Layout/Virtualization/VirtualizationContext.cs`

Describes the item and viewport metrics used to compute the realized item range for virtualized layout.

```csharp
public readonly record struct VirtualizationContext(
    int ItemCount,
    float ItemExtent,
    float ViewportExtent,
    float ScrollOffset,
    int CacheItems = 0)
```

Inheritance:
`ValueType` -> `VirtualizationContext`

## Examples

Create a context for 100 fixed-height items, with a viewport showing 30 layout units at scroll offset 20 and one cached item on each side.

```csharp
using Cerneala.UI.Layout.Virtualization;

VirtualizationContext context = new(
    ItemCount: 100,
    ItemExtent: 10,
    ViewportExtent: 30,
    ScrollOffset: 20,
    CacheItems: 1);

RealizationWindow window = context.GetRealizationWindow();

// window is [1, 6): items 1 through 5 are realized.
float totalExtent = context.TotalExtent; // 1000
```

## Remarks

`VirtualizationContext` is a value type that converts scroll state and fixed item sizing into a `RealizationWindow`. It is used by item presentation and virtualizing panels to decide which item indices should have realized UI elements.

The context expects a fixed, positive, finite `ItemExtent`. If `ItemCount` is zero or negative, or if `ItemExtent` is zero, negative, `NaN`, or infinite, `TotalExtent` returns `0` and `GetRealizationWindow` returns `RealizationWindow.Empty`.

`ScrollOffset` and `ViewportExtent` are sanitized before window calculation. Non-finite or non-positive values are treated as `0`. `CacheItems` is clamped to zero or greater, then subtracted from the first visible index and added to the exclusive end index. The final range is clamped to `[0, ItemCount]`.

`TotalExtent` multiplies `ItemCount` by `ItemExtent` using `double` precision and caps the result at `float.MaxValue` if the computed extent is larger.

## Constructors

| Name | Description |
| --- | --- |
| `VirtualizationContext(int, float, float, float, int)` | Initializes a context with item count, fixed item extent, viewport extent, scroll offset, and optional cache item count. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `CacheItems` | `int` | Gets the requested number of extra items to include before and after the visible range. Negative values are treated as `0` during window calculation. |
| `ItemCount` | `int` | Gets the total number of items in the virtualized collection. |
| `ItemExtent` | `float` | Gets the fixed extent of one item along the scrolling axis. Must be positive and finite to produce a non-empty window. |
| `ScrollOffset` | `float` | Gets the current scroll offset along the virtualized axis. Non-positive or non-finite values are treated as `0` during window calculation. |
| `TotalExtent` | `float` | Gets the total scrollable extent, calculated as `ItemCount * ItemExtent`, or `0` when the item count or item extent is invalid. |
| `ViewportExtent` | `float` | Gets the viewport extent along the virtualized axis. Non-positive or non-finite values are treated as `0` during window calculation. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `GetRealizationWindow()` | `RealizationWindow` | Computes the clamped half-open item-index range that should be realized for the current offset, viewport, and cache settings. |

## Applies to

- `Cerneala.UI.Layout.Virtualization.VirtualizationContext`

## See also

- `Cerneala.UI.Layout.Virtualization.RealizationWindow`
- `Cerneala.UI.Layout.Panels.VirtualizingStackPanel`
- `Cerneala.UI.Controls.ItemsPresenter`
