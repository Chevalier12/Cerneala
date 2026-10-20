namespace Cerneala.UI.Layout.Virtualization;

public interface IItemsVirtualizingPanel
{
    RealizationWindow RealizationWindow { get; }

    float TotalExtent { get; }

    void UpdateViewport(ItemsVirtualizationViewport viewport);

    // The reverse of UpdateViewport: the panel stops virtualizing and realizes every item.
    void ClearViewport();
}
