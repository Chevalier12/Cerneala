using System.Collections;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;
using Cerneala.UI.Layout.Virtualization;

namespace Cerneala.UI.Controls;

public class ItemsPresenter : Control
{
    private static readonly ItemsPanelTemplate DefaultItemsPanelTemplate = new(() => new Panel());
    private Layout.Panels.Panel? panelRoot;
    private bool itemsDirty = true;
    private RealizationWindow? lastRealizationWindow;

    public static readonly UiProperty<IEnumerable?> ItemsProperty = UiProperty<IEnumerable?>.Register(
        nameof(Items),
        typeof(ItemsPresenter),
        new UiPropertyMetadata<IEnumerable?>(
            null,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<DataTemplate?> ItemTemplateProperty = UiProperty<DataTemplate?>.Register(
        nameof(ItemTemplate),
        typeof(ItemsPresenter),
        new UiPropertyMetadata<DataTemplate?>(
            null,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<ItemsPanelTemplate?> ItemsPanelProperty = UiProperty<ItemsPanelTemplate?>.Register(
        nameof(ItemsPanel),
        typeof(ItemsPresenter),
        new UiPropertyMetadata<ItemsPanelTemplate?>(
            null,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender));

    public IEnumerable? Items
    {
        get => GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public DataTemplate? ItemTemplate
    {
        get => GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public ItemsPanelTemplate? ItemsPanel
    {
        get => GetValue(ItemsPanelProperty);
        set => SetValue(ItemsPanelProperty, value);
    }

    public Panel? PanelRoot => panelRoot as Panel;

    public Layout.Panels.Panel? LayoutPanelRoot => panelRoot;

    public ItemsControl? ItemsOwner { get; set; }

    public VirtualizationContext? VirtualizationContext { get; set; }

    public RealizationWindow CurrentRealizationWindow => lastRealizationWindow ?? RealizationWindow.Empty;

    public void MarkItemsDirty()
    {
        itemsDirty = true;
        IncrementLayoutVersion();
        IncrementRenderVersion();
        Invalidate(
            InvalidationFlags.Measure | InvalidationFlags.Arrange | InvalidationFlags.Render | InvalidationFlags.HitTest,
            "Items presenter items changed");
    }

    public void UpdateVirtualizationFromScrollInfo(IScrollInfo scrollInfo, float itemExtent, int cacheItems = 0)
    {
        UpdateVirtualizationFromScrollInfoCore(scrollInfo, itemExtent, cacheItems);
    }

    internal bool UpdateVirtualizationFromScrollInfoCore(IScrollInfo scrollInfo, float itemExtent, int cacheItems = 0)
    {
        ArgumentNullException.ThrowIfNull(scrollInfo);
        int itemCount = ItemsOwner?.ItemCount ?? Items?.Cast<object?>().Count() ?? 0;
        VirtualizationContext? previousContext = VirtualizationContext;
        VirtualizationContext nextContext = new(itemCount, itemExtent, scrollInfo.ViewportHeight, scrollInfo.VerticalOffset, cacheItems);
        RealizationWindow nextWindow = nextContext.GetRealizationWindow();
        bool virtualizationShapeChanged =
            previousContext is not VirtualizationContext previous ||
            previous.ItemCount != nextContext.ItemCount ||
            previous.ItemExtent != nextContext.ItemExtent ||
            previous.ViewportExtent != nextContext.ViewportExtent ||
            previous.CacheItems != nextContext.CacheItems;
        bool needsItemsRefresh = itemsDirty || virtualizationShapeChanged || lastRealizationWindow != nextWindow;

        VirtualizationContext = nextContext;
        if (!needsItemsRefresh)
        {
            ApplyVirtualizationContext(panelRoot, nextContext, nextWindow);
            return false;
        }

        MarkItemsDirty();
        return true;
    }

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        RefreshItems();
        LayoutSize desired = panelRoot?.Measure(context) ?? LayoutSize.Zero;
        ProcessInheritedAndStyleForSubtree(panelRoot);
        RemoveMeasureWorkForSubtree(panelRoot);
        RemoveMeasureWorkForLayoutScope();
        RemoveInheritedAndStyleWorkForLayoutScope();
        return desired;
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        RefreshItems();
        panelRoot?.Arrange(context);
        RemoveArrangeWorkForSubtree(panelRoot);
        return context.FinalRect;
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, ItemsProperty) ||
            ReferenceEquals(args.Property, ItemTemplateProperty) ||
            ReferenceEquals(args.Property, ItemsPanelProperty))
        {
            itemsDirty = true;
            RefreshItems();
        }
    }

    private void RefreshItems()
    {
        RealizationWindow? nextWindow = GetRealizationWindow();
        if (!itemsDirty && nextWindow == lastRealizationWindow)
        {
            return;
        }

        itemsDirty = false;
        lastRealizationWindow = nextWindow;
        if (ItemsOwner is not null)
        {
            RefreshOwnerItems(nextWindow);
            return;
        }

        Layout.Panels.Panel nextPanel = (ItemsPanel ?? DefaultItemsPanelTemplate).CreateLayoutPanel();
        ApplyVirtualizationContext(nextPanel, VirtualizationContext, nextWindow);

        List<UIElement> nextChildren = [.. CreateItemChildren(nextWindow)];
        Layout.Panels.Panel? oldPanel = panelRoot;
        List<UIElement> oldChildren = oldPanel is null ? [] : [.. oldPanel.VisualChildren];
        if (oldPanel is not null)
        {
            ClearPanelChildren(oldPanel);
            VisualChildren.Remove(oldPanel);
            LogicalChildren.Remove(oldPanel);
            panelRoot = null;
        }

        try
        {
            foreach (UIElement child in nextChildren)
            {
                AddPanelChild(nextPanel, child);
            }

            AddPanelRoot(nextPanel);
            panelRoot = nextPanel;
        }
        catch
        {
            ClearPanelChildren(nextPanel);
            RemovePanelRoot(nextPanel);
            panelRoot = null;
            if (oldPanel is not null)
            {
                foreach (UIElement child in oldChildren)
                {
                    AddPanelChild(oldPanel, child);
                }

                AddPanelRoot(oldPanel);
                panelRoot = oldPanel;
            }

            throw;
        }
    }

    private void RefreshOwnerItems(RealizationWindow? nextWindow)
    {
        Layout.Panels.Panel nextPanel = (ItemsPanel ?? ItemsOwner?.ItemsPanel ?? DefaultItemsPanelTemplate).CreateLayoutPanel();
        ApplyVirtualizationContext(nextPanel, VirtualizationContext, nextWindow);

        Layout.Panels.Panel? oldPanel = panelRoot;
        if (oldPanel is not null)
        {
            ClearPanelChildren(oldPanel);
            VisualChildren.Remove(oldPanel);
            LogicalChildren.Remove(oldPanel);
            panelRoot = null;
        }

        try
        {
            foreach (UIElement child in CreateItemChildren(nextWindow))
            {
                AddPanelChild(nextPanel, child);
            }

            AddPanelRoot(nextPanel);
            panelRoot = nextPanel;
        }
        catch
        {
            ClearPanelChildren(nextPanel);
            RemovePanelRoot(nextPanel);
            panelRoot = null;
            throw;
        }
    }

    private static void AddPanelChild(Layout.Panels.Panel panel, UIElement child)
    {
        panel.LogicalChildren.Add(child);
        try
        {
            panel.VisualChildren.Add(child);
        }
        catch
        {
            panel.LogicalChildren.Remove(child);
            throw;
        }
    }

    private void AddPanelRoot(Layout.Panels.Panel panel)
    {
        LogicalChildren.Add(panel);
        try
        {
            VisualChildren.Add(panel);
        }
        catch
        {
            LogicalChildren.Remove(panel);
            throw;
        }
    }

    private void RemovePanelRoot(Layout.Panels.Panel panel)
    {
        VisualChildren.Remove(panel);
        LogicalChildren.Remove(panel);
    }

    private static void ClearPanelChildren(Layout.Panels.Panel panel)
    {
        while (panel.VisualChildren.Count > 0)
        {
            panel.VisualChildren.Remove(panel.VisualChildren[panel.VisualChildren.Count - 1]);
        }

        while (panel.LogicalChildren.Count > 0)
        {
            panel.LogicalChildren.Remove(panel.LogicalChildren[panel.LogicalChildren.Count - 1]);
        }
    }

    private IEnumerable<UIElement> CreateItemChildren(RealizationWindow? window)
    {
        if (ItemsOwner is ItemsControl owner)
        {
            foreach (UIElement container in owner.ItemContainerGenerator.Realize(window))
            {
                owner.OnItemContainerPrepared(container, ItemContainerGenerator.GetItemIndex(container));
                yield return container;
            }

            yield break;
        }

        if (Items is null)
        {
            yield break;
        }

        if (window is { IsEmpty: true })
        {
            yield break;
        }

        int index = 0;
        foreach (object? item in Items)
        {
            if (window is { } realizationWindow)
            {
                if (index < realizationWindow.StartIndex)
                {
                    index++;
                    continue;
                }

                if (index >= realizationWindow.EndIndexExclusive)
                {
                    yield break;
                }
            }

            UIElement? child = item as UIElement ?? ItemTemplate?.CreateElement(item);
            if (child is not null)
            {
                yield return child;
            }

            index++;
        }
    }

    private RealizationWindow? GetRealizationWindow()
    {
        return VirtualizationContext?.GetRealizationWindow();
    }

    private static void ApplyVirtualizationContext(Layout.Panels.Panel? panel, VirtualizationContext? context, RealizationWindow? window)
    {
        if (panel is VirtualizingStackPanel virtualizingPanel && context is VirtualizationContext virtualizationContext)
        {
            virtualizingPanel.VirtualizationContext = virtualizationContext;
            virtualizingPanel.FirstRealizedIndex = window?.StartIndex ?? 0;
        }
    }

    private static void RemoveMeasureWorkForSubtree(UIElement? element)
    {
        if (element?.Root is not UIRoot root)
        {
            return;
        }

        foreach (UIElement current in ElementTreeWalker.PreOrder(element, ElementChildRole.Visual))
        {
            root.LayoutQueue.RemoveMeasure(current);
        }
    }

    private static void RemoveArrangeWorkForSubtree(UIElement? element)
    {
        if (element?.Root is not UIRoot root)
        {
            return;
        }

        foreach (UIElement current in ElementTreeWalker.PreOrder(element, ElementChildRole.Visual))
        {
            root.LayoutQueue.RemoveArrange(current);
        }
    }

    private static void ProcessInheritedAndStyleForSubtree(UIElement? element)
    {
        if (element?.Root is not UIRoot root)
        {
            return;
        }

        root.InheritedPropertyPropagator.PropagateFrom(element);
        foreach (UIElement current in ElementTreeWalker.PreOrder(element, ElementChildRole.Visual))
        {
            root.StyleProcessor.Process(current);
            root.InheritedPropertyQueue.Remove(current);
            root.StyleQueue.Remove(current);
            current.DirtyState.Clear(InvalidationFlags.Inherited | InvalidationFlags.Style);
        }
    }

    private void RemoveMeasureWorkForLayoutScope()
    {
        if (Root is not UIRoot root)
        {
            return;
        }

        for (UIElement? current = this; current is not null; current = current.VisualParent)
        {
            root.LayoutQueue.RemoveMeasure(current);
        }
    }

    private void RemoveInheritedAndStyleWorkForLayoutScope()
    {
        if (Root is not UIRoot root)
        {
            return;
        }

        for (UIElement? current = this; current is not null; current = current.VisualParent)
        {
            root.InheritedPropertyQueue.Remove(current);
            root.StyleQueue.Remove(current);
            current.DirtyState.Clear(InvalidationFlags.Inherited | InvalidationFlags.Style);
        }
    }
}
