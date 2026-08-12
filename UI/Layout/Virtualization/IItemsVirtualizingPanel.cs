namespace Cerneala.UI.Layout.Virtualization;

public interface IItemsVirtualizingPanel
{
    RealizationWindow RealizationWindow { get; }

    float TotalExtent { get; }

    void UpdateViewport(ItemsVirtualizationViewport viewport);
}
