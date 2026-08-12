using System.Collections;
using Cerneala.UI.Core;
using Cerneala.UI.Controls.Items;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;
using Cerneala.UI.Layout.Virtualization;

namespace Cerneala.UI.Controls;

public class ItemsPresenter : Control
{
    private readonly Layout.Panels.Panel defaultItemsPanel = new StackPanel();
    private Layout.Panels.Panel? panelRoot;
    private bool itemsDirty = true;
    private RealizationWindow? lastRealizationWindow;

    public static readonly UiProperty<IEnumerable?> ItemsProperty = UiProperty<IEnumerable?>.Register(
        nameof(Items),
        typeof(ItemsPresenter),
        new UiPropertyMetadata<IEnumerable?>(
            null,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<ContentTemplate?> ItemTemplateProperty = UiProperty<ContentTemplate?>.Register(
        nameof(ItemTemplate),
        typeof(ItemsPresenter),
        new UiPropertyMetadata<ContentTemplate?>(
            null,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<Layout.Panels.Panel?> ItemsPanelProperty = UiProperty<Layout.Panels.Panel?>.Register(
        nameof(ItemsPanel),
        typeof(ItemsPresenter),
        new UiPropertyMetadata<Layout.Panels.Panel?>(
            null,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsArrange | UiPropertyOptions.AffectsRender));

    public IEnumerable? Items
    {
        get => GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public ContentTemplate? ItemTemplate
    {
        get => GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public Layout.Panels.Panel? ItemsPanel
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
        int itemCount = ItemsOwner?.ViewItemCount ?? Items?.Cast<object?>().Count() ?? 0;
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

    internal bool UpdateAutomaticVirtualization(float viewportExtent, float scrollOffset)
    {
        if (VirtualizationContext is not null ||
            (ItemsOwner?.ItemsPanel ?? ItemsPanel) is not IItemsVirtualizingPanel virtualizingPanel)
        {
            return false;
        }

        RealizationWindow previous = virtualizingPanel.RealizationWindow;
        virtualizingPanel.UpdateViewport(new ItemsVirtualizationViewport(
            ItemsOwner?.ViewItemCount ?? Items?.Cast<object?>().Count() ?? 0,
            viewportExtent,
            scrollOffset));
        RealizationWindow next = virtualizingPanel.RealizationWindow;
        bool changed = itemsDirty || previous != next;
        if (changed)
        {
            MarkItemsDirty();
        }

        return changed;
    }

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        RefreshItems();

        RealizationWindow? windowBeforeMeasure = GetRealizationWindow();
        LayoutSize desired = panelRoot?.Measure(context) ?? LayoutSize.Zero;

        if (windowBeforeMeasure != GetRealizationWindow())
        {
            MarkItemsDirty();
        }

        ProcessInheritedAndAspectForSubtree(panelRoot);
        RemoveMeasureWorkForSubtree(panelRoot);
        RemoveMeasureWorkForLayoutScope();
        RemoveInheritedAndAspectWorkForLayoutScope();

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

        Layout.Panels.Panel nextPanel = ItemsPanel ?? defaultItemsPanel;
        ApplyVirtualizationContext(nextPanel, VirtualizationContext, nextWindow);

        List<UIElement> nextChildren = [.. CreateItemChildren(nextWindow)];
        Layout.Panels.Panel? oldPanel = panelRoot;
        List<UIElement> oldChildren = oldPanel is null ? [] : [.. oldPanel.VisualChildren];
        bool reusesPanel = ReferenceEquals(oldPanel, nextPanel);
        if (reusesPanel)
        {
            SynchronizePanelChildren(nextPanel, nextChildren);
            panelRoot = nextPanel;
            return;
        }

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
        Layout.Panels.Panel nextPanel = ItemsPanel ?? ItemsOwner?.ItemsPanel ?? defaultItemsPanel;
        ApplyVirtualizationContext(nextPanel, VirtualizationContext, nextWindow);

        Layout.Panels.Panel? oldPanel = panelRoot;
        List<UIElement> oldChildren = oldPanel is null ? [] : [.. oldPanel.VisualChildren];
        List<UIElement> nextChildren = [.. CreateItemChildren(nextWindow)];
        bool reusesPanel = ReferenceEquals(oldPanel, nextPanel);
        if (reusesPanel)
        {
            SynchronizePanelChildren(nextPanel, nextChildren);
            panelRoot = nextPanel;
            return;
        }

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

    private static void SynchronizePanelChildren(Layout.Panels.Panel panel, IReadOnlyList<UIElement> desiredChildren)
    {
        HashSet<UIElement> desired = new(desiredChildren, ReferenceEqualityComparer.Instance);
        for (int index = panel.VisualChildren.Count - 1; index >= 0; index--)
        {
            UIElement child = panel.VisualChildren[index];
            if (desired.Contains(child))
            {
                continue;
            }

            panel.VisualChildren.Remove(child);
            panel.LogicalChildren.Remove(child);
        }

        for (int desiredIndex = 0; desiredIndex < desiredChildren.Count; desiredIndex++)
        {
            UIElement desiredChild = desiredChildren[desiredIndex];
            if (desiredIndex < panel.VisualChildren.Count &&
                ReferenceEquals(panel.VisualChildren[desiredIndex], desiredChild))
            {
                continue;
            }

            int existingIndex = IndexOfReference(panel.VisualChildren, desiredChild, desiredIndex + 1);
            if (existingIndex >= 0)
            {
                panel.LogicalChildren.Move(existingIndex, desiredIndex);
                panel.VisualChildren.Move(existingIndex, desiredIndex);
                continue;
            }

            panel.LogicalChildren.Insert(desiredIndex, desiredChild);
            try
            {
                panel.VisualChildren.Insert(desiredIndex, desiredChild);
            }
            catch
            {
                panel.LogicalChildren.Remove(desiredChild);
                throw;
            }
        }
    }

    private static int IndexOfReference(
        IReadOnlyList<UIElement> children,
        UIElement candidate,
        int startIndex)
    {
        for (int index = startIndex; index < children.Count; index++)
        {
            if (ReferenceEquals(children[index], candidate))
            {
                return index;
            }
        }

        return -1;
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

            UIElement? child = item as UIElement ?? ItemTemplate?.Create(new ContentTemplateContext(item, index: index));
            if (child is not null)
            {
                yield return child;
            }

            index++;
        }
    }

    private RealizationWindow? GetRealizationWindow()
    {
        if (VirtualizationContext is not null)
        {
            return VirtualizationContext.Value.GetRealizationWindow();
        }

        return (ItemsOwner?.ItemsPanel ?? ItemsPanel) is IItemsVirtualizingPanel virtualizingPanel
            ? virtualizingPanel.RealizationWindow
            : null;
    }

    private static void ApplyVirtualizationContext(Layout.Panels.Panel? panel, VirtualizationContext? context, RealizationWindow? window)
    {
        if (panel is VirtualizingStackPanel virtualizingPanel && context is VirtualizationContext virtualizationContext)
        {
            virtualizingPanel.VirtualizationContext = virtualizationContext;
            virtualizingPanel.FirstRealizedIndex = window?.StartIndex ?? 0;
        }

        else if (panel is VirtualizingStackPanel automaticPanel && window is RealizationWindow automaticWindow)
        {
            automaticPanel.FirstRealizedIndex = automaticWindow.StartIndex;
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

    private static void ProcessInheritedAndAspectForSubtree(UIElement? element)
    {
        if (element?.Root is not UIRoot root)
        {
            return;
        }

        root.InheritedPropertyPropagator.PropagateFrom(element);
        foreach (UIElement current in ElementTreeWalker.PreOrder(element, ElementChildRole.Visual))
        {
            root.AspectProcessor.Process(current);
            root.InheritedPropertyQueue.Remove(current);
            root.AspectQueue.Remove(current);
            current.DirtyState.Clear(InvalidationFlags.Inherited | InvalidationFlags.Aspect);
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

    private void RemoveInheritedAndAspectWorkForLayoutScope()
    {
        if (Root is not UIRoot root)
        {
            return;
        }

        for (UIElement? current = this; current is not null; current = current.VisualParent)
        {
            root.InheritedPropertyQueue.Remove(current);
            root.AspectQueue.Remove(current);
            current.DirtyState.Clear(InvalidationFlags.Inherited | InvalidationFlags.Aspect);
        }
    }
}
