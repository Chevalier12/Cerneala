namespace Cerneala.UI.Layout.Virtualization;

public readonly record struct ItemsVirtualizationViewport(
    int ItemCount,
    float ViewportExtent,
    float ScrollOffset,
    int CacheItems = 1);
