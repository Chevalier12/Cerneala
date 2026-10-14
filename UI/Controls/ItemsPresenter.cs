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
    private ItemsVirtualizationViewport? automaticViewport;
    private VirtualizationContext? virtualizationContext;
    private VirtualizingStackPanel? contextPanel;

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

    public VirtualizationContext? VirtualizationContext
    {
        get => virtualizationContext;
        set
        {
            virtualizationContext = value;
            if (value is null)
            {
                ReleasePanelVirtualizationContext();
            }
        }
    }

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
            ResolveItemsPanel() is not IItemsVirtualizingPanel virtualizingPanel)
        {
            return false;
        }

        RealizationWindow previous = virtualizingPanel.RealizationWindow;
        automaticViewport = new ItemsVirtualizationViewport(GetItemCount(), viewportExtent, scrollOffset);
        virtualizingPanel.UpdateViewport(automaticViewport.Value);
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
        LayoutSize desired = MeasurePanelRoot(context);
        RemoveMeasureWorkForLayoutScope();
        RemoveInheritedAndAspectWorkForLayoutScope();

        return desired;
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        // An arrange-time window change, such as a scroll offset clamped after the extent shrank,
        // realizes items that were never measured. Measure them with the panel's last constraint
        // so the panel does not arrange them with an empty desired size.
        if (RefreshItems() && panelRoot?.LastMeasureAvailableSize is LayoutSize availableSize)
        {
            MeasurePanelRoot(new MeasureContext(availableSize, context.Rounding));
        }

        panelRoot?.Arrange(context);
        RemoveArrangeWorkForSubtree(panelRoot);
        return context.FinalRect;
    }

    private LayoutSize MeasurePanelRoot(MeasureContext context)
    {
        ProcessInheritedAndAspectForSubtree(panelRoot);

        RealizationWindow? windowBeforeMeasure = GetRealizationWindow();
        LayoutSize desired = panelRoot?.Measure(context) ?? LayoutSize.Zero;
        ProcessInheritedAndAspectForSubtree(panelRoot);
        InvalidateMeasureCacheAboveStaleElements(panelRoot);
        desired = panelRoot?.Measure(context) ?? LayoutSize.Zero;

        if (windowBeforeMeasure != GetRealizationWindow())
        {
            MarkItemsDirty();
        }

        RemoveMeasureWorkForSubtree(panelRoot);
        return desired;
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, ItemsProperty) ||
            ReferenceEquals(args.Property, ItemTemplateProperty) ||
            ReferenceEquals(args.Property, ItemsPanelProperty))
        {
            if (ReferenceEquals(args.Property, ItemsPanelProperty))
            {
                ItemsOwner?.OnItemsPresenterPanelChanged();
            }

            itemsDirty = true;
            RefreshItems();
        }
    }

    private bool RefreshItems()
    {
        if (itemsDirty)
        {
            ResetEnteringPanelItemExtents();
            RefreshAutomaticViewportItemCount();
        }

        RealizationWindow? nextWindow = GetRealizationWindow();
        if (!itemsDirty && nextWindow == lastRealizationWindow)
        {
            return false;
        }

        itemsDirty = false;
        lastRealizationWindow = nextWindow;
        if (ItemsOwner is not null)
        {
            RefreshOwnerItems(nextWindow);
            return true;
        }

        Layout.Panels.Panel nextPanel = ResolveItemsPanel();
        ApplyVirtualizationContext(nextPanel, VirtualizationContext, nextWindow);

        List<UIElement> nextChildren = [.. CreateItemChildren(nextWindow)];
        Layout.Panels.Panel? oldPanel = panelRoot;
        List<UIElement> oldChildren = oldPanel is null ? [] : [.. oldPanel.VisualChildren];
        bool reusesPanel = ReferenceEquals(oldPanel, nextPanel);
        if (reusesPanel)
        {
            SynchronizePanelChildren(nextPanel, nextChildren);
            panelRoot = nextPanel;
            return true;
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

        return true;
    }

    private void RefreshOwnerItems(RealizationWindow? nextWindow)
    {
        Layout.Panels.Panel nextPanel = ResolveItemsPanel();
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

    // An item change re-measures this presenter but not necessarily the scroll presenter
    // that pushes the viewport, so the panel must not keep realizing against a stale count.
    private void RefreshAutomaticViewportItemCount()
    {
        if (automaticViewport is not ItemsVirtualizationViewport viewport ||
            VirtualizationContext is not null ||
            ResolveItemsPanel() is not IItemsVirtualizingPanel virtualizingPanel)
        {
            return;
        }

        int itemCount = GetItemCount();
        if (itemCount == viewport.ItemCount)
        {
            return;
        }

        automaticViewport = viewport with { ItemCount = itemCount };
        virtualizingPanel.UpdateViewport(automaticViewport.Value);
    }

    // Collection changes reach only the current panel root, so a panel that is about to become
    // the root has missed them and its recorded row heights may belong to other items.
    private void ResetEnteringPanelItemExtents()
    {
        if (ResolveItemsPanel() is VirtualizingStackPanel enteringPanel &&
            !ReferenceEquals(enteringPanel, panelRoot))
        {
            enteringPanel.OnItemsReset();
        }
    }

    private int GetItemCount()
    {
        return ItemsOwner?.ViewItemCount ?? Items?.Cast<object?>().Count() ?? 0;
    }

    private RealizationWindow? GetRealizationWindow()
    {
        if (VirtualizationContext is not null)
        {
            return VirtualizationContext.Value.GetRealizationWindow();
        }

        // A VirtualizingStackPanel with no viewport and no fixed context realizes every child, and
        // its children are only the items realized here, so it realizes every item like a plain panel.
        return ResolveItemsPanel() is IItemsVirtualizingPanel virtualizingPanel and not VirtualizingStackPanel { IsVirtualizing: false }
            ? virtualizingPanel.RealizationWindow
            : null;
    }

    // The panel placed in the tree must be the panel that receives the viewport and supplies
    // the realization window, so every caller resolves it here.
    internal Layout.Panels.Panel ResolveItemsPanel()
    {
        return ItemsPanel ?? ItemsOwner?.ItemsPanel ?? defaultItemsPanel;
    }

    private void ApplyVirtualizationContext(Layout.Panels.Panel? panel, VirtualizationContext? context, RealizationWindow? window)
    {
        if (panel is VirtualizingStackPanel virtualizingPanel && context is VirtualizationContext suppliedContext)
        {
            if (!ReferenceEquals(contextPanel, virtualizingPanel))
            {
                ReleasePanelVirtualizationContext();
            }

            virtualizingPanel.VirtualizationContext = suppliedContext;
            virtualizingPanel.FirstRealizedIndex = window?.StartIndex ?? 0;
            contextPanel = virtualizingPanel;
        }

        else if (panel is VirtualizingStackPanel automaticPanel)
        {
            automaticPanel.FirstRealizedIndex = window?.StartIndex ?? 0;
        }
    }

    // A panel keeps the context written here only while this presenter supplies it to that panel.
    // A stale copy would otherwise override the panel's automatic or natural-height realization.
    private void ReleasePanelVirtualizationContext()
    {
        if (contextPanel is not null)
        {
            contextPanel.VirtualizationContext = null;
            contextPanel = null;
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
            root.AspectQueue.Remove(current);
            current.DirtyState.Clear(InvalidationFlags.Aspect);
        }

        root.InheritedPropertyPropagator.PropagateFrom(element);
        foreach (UIElement current in ElementTreeWalker.PreOrder(element, ElementChildRole.Visual))
        {
            root.InheritedPropertyQueue.Remove(current);
            current.DirtyState.Clear(InvalidationFlags.Inherited);
        }
    }

    // The Inherited and Aspect pass can change measure-affecting values of elements that the
    // first panel measure already measured. Attached invalidation does not move ancestor layout
    // versions, and this presenter removes the queued measure work afterwards, so the path from
    // each stale element to the panel must miss its measure cache in the second panel measure.
    private static void InvalidateMeasureCacheAboveStaleElements(UIElement? element)
    {
        if (element?.Root is null)
        {
            return;
        }

        foreach (UIElement current in ElementTreeWalker.PreOrder(element, ElementChildRole.Visual))
        {
            if (ReferenceEquals(current, element) || !HasStaleMeasure(current))
            {
                continue;
            }

            for (UIElement? ancestor = current.VisualParent; ancestor is not null; ancestor = ancestor.VisualParent)
            {
                if (HasStaleMeasure(ancestor))
                {
                    break;
                }

                ancestor.IncrementLayoutVersion();
                if (ReferenceEquals(ancestor, element))
                {
                    break;
                }
            }
        }
    }

    private static bool HasStaleMeasure(UIElement element)
    {
        return element.LastMeasureAvailableSize is not null &&
            element.LastMeasureLayoutVersion != element.LayoutVersion;
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
