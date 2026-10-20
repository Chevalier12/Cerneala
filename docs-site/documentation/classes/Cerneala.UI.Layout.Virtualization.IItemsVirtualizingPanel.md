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

`ClearViewport()` is the reverse of `UpdateViewport`: after it the panel stops virtualizing and realizes every item, and its `RealizationWindow` covers all items. The `ItemsPresenter` calls it when the only `ScrollViewer` that gave a viewport stops giving one, and no `ScrollViewer` around the owner gives another. For example, a `ListBox` with `Height = 100` in a `StackPanel` gets the 100-pixel viewport of the `ScrollViewer` in its template; after `listBox.Height = float.NaN` that `ScrollViewer` is measured with an unbounded height, the panel gets `ClearViewport()`, and all 100 rows of the list are realized. The presenter refreshes the items when `RealizationWindow` differs after the call. This method was added to the interface in this version, so an existing implementation must add it.

A minimal implementation drops the stored viewport and invalidates its layout:

```csharp
public void ClearViewport()
{
    viewport = null;
    Invalidate(InvalidationFlags.Measure | InvalidationFlags.Arrange, "Viewport cleared");
}
```

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `RealizationWindow` | `RealizationWindow` | Current range of source indices that should be realized. |
| `TotalExtent` | `float` | Current measured or estimated scrolling extent. |

## Methods

| Name | Description |
| --- | --- |
| `UpdateViewport(ItemsVirtualizationViewport)` | Supplies the item count, viewport extent, scroll offset, and cache size. |
| `ClearViewport()` | The reverse of `UpdateViewport`: the panel stops virtualizing and realizes every item. |

## Applies to

Project: `Cerneala`

## See also

- `ItemsControl`
- `VirtualizingStackPanel`
- `ItemsVirtualizationViewport`
