# IItemsVirtualizingPanel Interface

## Definition
Namespace: `Cerneala.UI.Layout.Virtualization`  
Assembly/Project: `Cerneala`  
Source: `UI/Layout/Virtualization/IItemsVirtualizingPanel.cs`

Defines the viewport contract through which an items panel opts into automatic container virtualization.

```csharp
public interface IItemsVirtualizingPanel
```

## Examples

```xml
<ItemsControl ItemsSource="$DataContext.Rows">
    <ItemsControl.ItemsPanel>
        <VirtualizingStackPanel />
    </ItemsControl.ItemsPanel>
</ItemsControl>
```

## Remarks

`ScrollViewer` supplies viewport and offset changes through the owning `ItemsControl`. The panel computes a `RealizationWindow`; the framework creates and recycles only containers inside that window. Panels that do not implement this interface realize all items.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `RealizationWindow` | `RealizationWindow` | Current range of source indices that should be realized. |
| `TotalExtent` | `float` | Current measured or estimated scrolling extent. |

## Methods

| Name | Description |
| --- | --- |
| `UpdateViewport(ItemsVirtualizationViewport)` | Supplies the item count, viewport extent, scroll offset, and cache size. |

## Applies to

Project: `Cerneala`

## See also

- `ItemsControl`
- `VirtualizingStackPanel`
- `ItemsVirtualizationViewport`
