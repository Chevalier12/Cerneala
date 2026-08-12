# ItemsVirtualizationViewport Structure

## Definition
Namespace: `Cerneala.UI.Layout.Virtualization`  
Assembly/Project: `Cerneala`  
Source: `UI/Layout/Virtualization/ItemsVirtualizationViewport.cs`

Describes the current scrolling viewport supplied to an items-virtualizing panel.

```csharp
public readonly record struct ItemsVirtualizationViewport(
    int ItemCount,
    float ViewportExtent,
    float ScrollOffset,
    int CacheItems = 1);
```

## Examples

```csharp
panel.UpdateViewport(new ItemsVirtualizationViewport(
    ItemCount: 100,
    ViewportExtent: 320,
    ScrollOffset: 640));
```

## Remarks

`ItemsControl` normally creates this value automatically from its `ScrollViewer`. Custom `IItemsVirtualizingPanel` implementations use it to calculate their realization window.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `ItemCount` | `int` | Number of items in the active view. |
| `ViewportExtent` | `float` | Visible extent along the virtualization axis. |
| `ScrollOffset` | `float` | Current offset along the virtualization axis. |
| `CacheItems` | `int` | Extra item count retained before and after the visible window. |

## Applies to

Project: `Cerneala`

## See also

- `IItemsVirtualizingPanel`
- `RealizationWindow`
