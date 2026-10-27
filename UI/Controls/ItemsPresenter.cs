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
    private RealizationWindow? presentedRealizationWindow;
    private ItemsVirtualizationViewport? ownerViewport;
    private ItemsVirtualizationViewport? hostViewport;
    private VirtualizationContext? virtualizationContext;
    private VirtualizingStackPanel? contextPanel;
    private ItemsControl? itemsOwner;

    // The elements ItemTemplate created for the presented items of a standalone presenter, by item index.
    private Dictionary<int, TemplatedItemElement> templatedElements = [];

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

    public ItemsControl? ItemsOwner
    {
        get => itemsOwner;
        set
        {
            if (ReferenceEquals(itemsOwner, value))
            {
                return;
            }

            ItemsControl? previousOwner = itemsOwner;
            itemsOwner = value;
            WithdrawRealizedItems(previousOwner);
        }
    }

    public VirtualizationContext? VirtualizationContext
    {
        get => virtualizationContext;
        set
        {
            // Like an AffectsMeasure property, a different context schedules the items refresh by itself (#708).
            if (virtualizationContext == value)
            {
                return;
            }

            virtualizationContext = value;
            if (value is null)
            {
                ReleasePanelVirtualizationContext();
            }

            MarkItemsDirty();
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

        // Written to the field, not through the setter: the setter marks the items dirty, and this method does that
        // itself only when a refresh is needed.
        virtualizationContext = nextContext;
        if (!needsItemsRefresh)
        {
            ApplyVirtualizationContext(panelRoot, nextContext, nextWindow);
            return false;
        }

        MarkItemsDirty();
        return true;
    }

    // The viewport of a scroll presenter whose content is the owner, with an offset that counts from the top of the items.
    internal bool UpdateAutomaticVirtualization(float viewportExtent, float scrollOffset)
    {
        ownerViewport = new ItemsVirtualizationViewport(GetItemCount(), viewportExtent, scrollOffset);
        return ApplyAutomaticViewport();
    }

    // The viewport of a scroll presenter whose content is this presenter, with an offset that counts from its top.
    internal bool UpdateHostVirtualization(float viewportExtent, float scrollOffset)
    {
        hostViewport = new ItemsVirtualizationViewport(GetItemCount(), viewportExtent, scrollOffset);
        return ApplyAutomaticViewport();
    }

    // The scroll presenter that pushed the owner viewport no longer drives these items, so a virtualizing
    // panel that enters later must not realize against that viewport. A host viewport keeps applying.
    internal bool ClearAutomaticViewport()
    {
        if (ownerViewport is null)
        {
            return false;
        }

        ownerViewport = null;
        return ApplyAutomaticViewport();
    }

    // The hosting scroll presenter no longer gives this presenter a finite viewport, for example because it was
    // measured with an unbounded height inside an outer viewer, so the owner viewport applies again. Without an owner
    // viewport no scroll presenter limits the items any more, so the panel stops virtualizing and realizes every item,
    // through the IItemsVirtualizingPanel contract, so a custom virtualizing panel does it like a VirtualizingStackPanel.
    internal bool ClearHostViewport()
    {
        if (hostViewport is null)
        {
            return false;
        }

        hostViewport = null;
        if (ownerViewport is not null)
        {
            return ApplyAutomaticViewport();
        }

        if (ResolveItemsPanel() is not IItemsVirtualizingPanel panel)
        {
            return false;
        }

        panel.ClearViewport();
        // The presenter's window, not the panel's, decides whether the items change: a non-virtualizing panel presents every item.
        if (GetRealizationWindow() == lastRealizationWindow)
        {
            return false;
        }

        MarkItemsDirty();
        return true;
    }

    // As WPF's ScrollContentPresenter, which makes the panel of an ItemsPresenter child its scroll info, the scroll
    // presenter that hosts this presenter drives its panel, so its viewport wins over the viewport of a scroll
    // presenter around the owner, for example an outer ScrollViewer around a ListBox with a fixed height.
    private ItemsVirtualizationViewport? AutomaticViewport => hostViewport ?? ownerViewport;

    private bool ApplyAutomaticViewport()
    {
        // The viewport is kept while a non-virtualizing panel or a fixed context presents the items, so a
        // virtualizing panel that replaces them later realizes only the current viewport's items in its first refresh.
        if (AutomaticViewport is not ItemsVirtualizationViewport viewport ||
            VirtualizationContext is not null ||
            ResolveItemsPanel() is not IItemsVirtualizingPanel virtualizingPanel)
        {
            return false;
        }

        RealizationWindow previous = virtualizingPanel.RealizationWindow;
        virtualizingPanel.UpdateViewport(viewport);
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
        RemoveInheritedWorkOfThisPresenter();

        return desired;
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        // An arrange-time window change, such as a scroll offset clamped after the extent shrank,
        // realizes items that were never measured. Measure them with the panel's last constraint
        // so the panel does not arrange them with an empty desired size.
        if (RefreshItems() && panelRoot?.LastMeasureAvailableSize is LayoutSize availableSize)
        {
            LayoutSize measuredPanelSize = panelRoot.DesiredSize;
            MeasurePanelRoot(new MeasureContext(availableSize, context.Rounding));

            // This presenter's desired size is still the panel's size from the last measure, so a panel size that changed
            // here is measured again by the next frame. That measure refreshes nothing, because the window it reads is
            // the one recorded above, and returns the panel's size, so the next arrange refreshes nothing and does not
            // invalidate again.
            if (panelRoot.DesiredSize != measuredPanelSize)
            {
                Invalidate(InvalidationFlags.Measure, "Items presenter panel size changed while arranging");
            }
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

            // An element the previous template created is not kept for the new one.
            if (ReferenceEquals(args.Property, ItemTemplateProperty))
            {
                templatedElements.Clear();
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
            RefreshAutomaticViewport();
        }

        RealizationWindow? nextWindow = GetRealizationWindow();
        if (!itemsDirty && nextWindow == lastRealizationWindow)
        {
            return false;
        }

        itemsDirty = false;
        lastRealizationWindow = nextWindow;
        try
        {
            if (ItemsOwner is not null)
            {
                RefreshOwnerItems(nextWindow);
            }
            else
            {
                RefreshPresenterItems(nextWindow);
            }
        }
        catch
        {
            // A refresh that throws leaves the previous items presented, so the presentation stays
            // dirty and the next measure or arrange retries it. A setter that threw here also skipped
            // its own measure invalidation, which this restores.
            MarkItemsDirty();
            throw;
        }

        presentedRealizationWindow = nextWindow;
        return true;
    }

    private void RefreshPresenterItems(RealizationWindow? nextWindow)
    {
        Layout.Panels.Panel nextPanel = ResolveItemsPanel();
        Dictionary<int, TemplatedItemElement> nextTemplatedElements = [];
        List<UIElement> nextChildren = [.. CreatePresenterItemChildren(nextWindow, nextTemplatedElements)];
        Layout.Panels.Panel? oldPanel = panelRoot;
        List<UIElement> oldChildren = oldPanel is null ? [] : [.. oldPanel.VisualChildren];
        bool reusesPanel = ReferenceEquals(oldPanel, nextPanel);
        if (reusesPanel)
        {
            SynchronizePanelChildren(nextPanel, nextChildren, oldChildren);
            panelRoot = nextPanel;
            templatedElements = nextTemplatedElements;
            ApplyVirtualizationContext(nextPanel, VirtualizationContext, nextWindow);
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
                nextPanel.Children.Add(child);
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
                    oldPanel.Children.Add(child);
                }

                AddPanelRoot(oldPanel);

                panelRoot = oldPanel;
            }

            throw;
        }

        templatedElements = nextTemplatedElements;
        ApplyVirtualizationContext(nextPanel, VirtualizationContext, nextWindow);
    }

    private void RefreshOwnerItems(RealizationWindow? nextWindow)
    {
        Layout.Panels.Panel nextPanel = ResolveItemsPanel();
        Layout.Panels.Panel? oldPanel = panelRoot;
        List<UIElement> oldChildren = oldPanel is null ? [] : [.. oldPanel.VisualChildren];
        List<UIElement> nextChildren;
        try
        {
            nextChildren = [.. CreateItemChildren(nextWindow)];
        }
        catch
        {
            ReleaseContainersOfFailedRealization(oldPanel);
            throw;
        }

        bool reusesPanel = ReferenceEquals(oldPanel, nextPanel);
        if (reusesPanel)
        {
            try
            {
                MovePanelChildrenTo(nextPanel, nextChildren);
            }
            catch
            {
                RestorePresentedOwnerItems(nextPanel, nextChildren, oldChildren);
                throw;
            }

            panelRoot = nextPanel;
            ApplyVirtualizationContext(nextPanel, VirtualizationContext, nextWindow);
            CommitPresentedOwnerWindow(nextWindow, nextChildren);
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
                nextPanel.Children.Add(child);
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
                    oldPanel.Children.Add(child);
                }

                AddPanelRoot(oldPanel);

                panelRoot = oldPanel;
                RestorePresentedOwnerItems(oldPanel, nextChildren, oldChildren);
            }
            else
            {
                ReleaseUnpresentedContainers(panel: null, nextChildren);
            }

            throw;
        }

        ApplyVirtualizationContext(nextPanel, VirtualizationContext, nextWindow);
        CommitPresentedOwnerWindow(nextWindow, nextChildren);
    }

    // The owner's containers outside the presented window are recycled only after the window is committed,
    // so a refresh that throws while it prepares or presents the window keeps the previous children prepared.
    // The window is recorded as presented first, so an owner's ClearItemContainer that throws while the rest
    // are recycled leaves a later failed refresh restoring the window the panel shows.
    private void CommitPresentedOwnerWindow(RealizationWindow? window, IReadOnlyList<UIElement> presentedChildren)
    {
        presentedRealizationWindow = window;
        RecycleUnpresentedContainers(presentedChildren);
    }

    private void RecycleUnpresentedContainers(IReadOnlyList<UIElement> presentedChildren)
    {
        ItemsOwner?.ItemContainerGenerator.RecycleAllExcept(presentedChildren);
    }

    // The previous children stay realized, but the failed realization can have recycled one of them and prepared it
    // for another item, for example when its item now needs another container type, so the previous children cannot
    // be put back as they are. The window the panel presented is realized again instead, after the containers the
    // panel did not take are given back without detaching them. The containers outside that window, which the failed
    // realization created or kept, are then recycled like after a committed refresh. A realization of that window that
    // throws leaves no restored children to present, so the panel gets back its previous children, which are still
    // prepared for their items, displaced or realized, and which the panel's first realized index and context still
    // describe, because a partial move of the retained panel can have removed some of them and inserted others. What
    // the failed realizations realized outside the panel is then given back, like after the failed realization of the
    // next window.
    private void RestorePresentedOwnerItems(
        Layout.Panels.Panel panel,
        IReadOnlyList<UIElement> failedChildren,
        IReadOnlyList<UIElement> previousChildren)
    {
        ReleaseUnpresentedContainers(panel, failedChildren);
        List<UIElement> restoredChildren;
        try
        {
            restoredChildren = [.. CreateItemChildren(presentedRealizationWindow)];
        }
        catch
        {
            MovePanelChildrenTo(panel, previousChildren);
            ReleaseContainersOfFailedRealization(panel);
            throw;
        }

        PresentRestoredOwnerItems(panel, restoredChildren);
        RecycleUnpresentedContainers(restoredChildren);
    }

    // The presented window is realized with the current items, so it can hold a container the panel cannot take
    // either, for example an item that is its own container and that another element still holds. The first restored
    // container the panel did not take is the one it rejected, because the panel takes the children in order. It is
    // given back like the others, and the panel presents the rest of the window. A failure that leaves every restored
    // container in the panel is not a rejected container, so it is rethrown.
    private void PresentRestoredOwnerItems(Layout.Panels.Panel panel, List<UIElement> restoredChildren)
    {
        while (true)
        {
            try
            {
                MovePanelChildrenTo(panel, restoredChildren);
                return;
            }
            catch
            {
                int rejectedIndex = restoredChildren.FindIndex(child => !ReferenceEquals(child.VisualParent, panel));
                if (rejectedIndex < 0)
                {
                    throw;
                }

                ItemsOwner!.ItemContainerGenerator.ReleaseUnpresented(restoredChildren[rejectedIndex]);
                restoredChildren.RemoveAt(rejectedIndex);
            }
        }
    }

    // A realization that throws has already realized the containers before the one that failed, and the generator
    // finishes realizing the window before any of them is passed to OnItemContainerPrepared. The kept panel never takes
    // them, so every realized container that panel does not present is given back like a container it did not take.
    private void ReleaseContainersOfFailedRealization(Layout.Panels.Panel? presentingPanel)
    {
        ReleaseUnpresentedContainers(presentingPanel, [.. ItemsOwner!.ItemContainerGenerator.RealizedContainers.Values]);
    }

    // Without a panel nothing is presented, so every container is given back.
    private void ReleaseUnpresentedContainers(Layout.Panels.Panel? panel, IEnumerable<UIElement> containers)
    {
        ItemContainerGenerator generator = ItemsOwner!.ItemContainerGenerator;
        foreach (UIElement container in containers)
        {
            if (panel is null || !ReferenceEquals(container.VisualParent, panel))
            {
                generator.ReleaseUnpresented(container);
            }
        }
    }

    // A sync that throws puts the previous children back in their previous order, so the retained
    // panel keeps presenting the previous items like the old panel of a failed panel swap.
    private static void SynchronizePanelChildren(
        Layout.Panels.Panel panel,
        IReadOnlyList<UIElement> desiredChildren,
        IReadOnlyList<UIElement> previousChildren)
    {
        try
        {
            MovePanelChildrenTo(panel, desiredChildren);
        }
        catch
        {
            MovePanelChildrenTo(panel, previousChildren);
            throw;
        }
    }

    private static void MovePanelChildrenTo(Layout.Panels.Panel panel, IReadOnlyList<UIElement> desiredChildren)
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

            panel.Children.Insert(desiredIndex, desiredChild);
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
        ItemsControl owner = ItemsOwner!;
        foreach (UIElement container in owner.ItemContainerGenerator.RealizeWithoutRecycling(window))
        {
            owner.OnItemContainerPrepared(container, ItemContainerGenerator.GetItemIndex(container));
            yield return container;
        }
    }

    // The element the template created for an item is kept while the same item stays at its index, so an element that
    // stays in the window keeps its state when the window moves or the items refresh, like an element item (#904). The
    // template runs only for an index that enters the window or whose item changed. The elements of the new window are
    // recorded in nextTemplatedElements, which the refresh keeps only when it succeeds.
    private IEnumerable<UIElement> CreatePresenterItemChildren(
        RealizationWindow? window,
        Dictionary<int, TemplatedItemElement> nextTemplatedElements)
    {
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

            UIElement? child = item as UIElement ?? GetOrCreateTemplatedElement(item, index, nextTemplatedElements);
            if (child is not null)
            {
                yield return child;
            }

            index++;
        }
    }

    private UIElement? GetOrCreateTemplatedElement(
        object? item,
        int index,
        Dictionary<int, TemplatedItemElement> nextTemplatedElements)
    {
        if (templatedElements.TryGetValue(index, out TemplatedItemElement kept) && IsSameItem(kept.Item, item))
        {
            nextTemplatedElements.Add(index, kept);
            return kept.Element;
        }

        UIElement? created = ItemTemplate?.Create(new ContentTemplateContext(item, index: index));
        if (created is not null)
        {
            nextTemplatedElements.Add(index, new TemplatedItemElement(item, created));
        }

        return created;
    }

    // An element shows the item instance its template received, so another instance gets a new element, like an
    // owner's container is prepared again for it, even when it is equal and shows other values. A boxed value is a new
    // instance on each enumeration and an equal string shows the same text, so an equal one is the same item.
    private static bool IsSameItem(object? keptItem, object? item)
    {
        return ReferenceEquals(keptItem, item) || (item is ValueType or string && Equals(keptItem, item));
    }

    // An item or panel change re-measures this presenter but not necessarily the scroll presenter
    // that pushes the viewport, so the panel must not keep realizing against a stale count, and a
    // panel that is about to become the root, which has not received the viewport, must not realize
    // every item.
    // Both viewports keep the current count, so the owner viewport that applies again when the host viewport is cleared
    // does not bring back an old count.
    private void RefreshAutomaticViewport()
    {
        if (AutomaticViewport is not ItemsVirtualizationViewport viewport)
        {
            return;
        }

        int itemCount = GetItemCount();
        if (ownerViewport is ItemsVirtualizationViewport owner && owner.ItemCount != itemCount)
        {
            ownerViewport = owner with { ItemCount = itemCount };
        }

        if (hostViewport is ItemsVirtualizationViewport host && host.ItemCount != itemCount)
        {
            hostViewport = host with { ItemCount = itemCount };
        }

        if (VirtualizationContext is not null ||
            ResolveItemsPanel() is not IItemsVirtualizingPanel virtualizingPanel ||
            (itemCount == viewport.ItemCount && ReferenceEquals(virtualizingPanel, panelRoot)))
        {
            return;
        }

        virtualizingPanel.UpdateViewport(AutomaticViewport.Value);
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

    // A refresh applies the context only after its panel and children are committed, so a refresh
    // that throws leaves the context and first realized index with the panel the presenter keeps.
    private void ApplyVirtualizationContext(Layout.Panels.Panel? panel, VirtualizationContext? context, RealizationWindow? window)
    {
        if (!ReferenceEquals(contextPanel, panel))
        {
            ReleasePanelVirtualizationContext();
        }

        if (panel is VirtualizingStackPanel virtualizingPanel && context is VirtualizationContext suppliedContext)
        {
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

    // Realized children belong to the owner whose generator created them, and the owner's next
    // presenter can resolve the same panel, so a presenter that changes owner gives both back.
    // The containers that a failed refresh kept displaced in the panel represent no item any more and leave the
    // panel here, so the previous owner's generator releases them now, before user code can give one another parent
    // that a later commit would detach it from.
    // The scroll presenters push their viewports only to the owner's active presenter, so the viewports kept here
    // describe the previous owner's scrolling and are dropped too.
    private void WithdrawRealizedItems(ItemsControl? previousOwner)
    {
        ReleasePanelVirtualizationContext();
        ownerViewport = null;
        hostViewport = null;
        if (panelRoot is not null)
        {
            ClearPanelChildren(panelRoot);
            VisualChildren.Remove(panelRoot);
            LogicalChildren.Remove(panelRoot);
            panelRoot = null;
        }

        itemsDirty = true;
        lastRealizationWindow = null;
        presentedRealizationWindow = null;
        templatedElements.Clear();
        previousOwner?.ItemContainerGenerator.ReleaseDisplaced();
    }

    private static void RemoveMeasureWorkForSubtree(UIElement? element)
    {
        if (element?.Root is not UIRoot root)
        {
            return;
        }

        foreach (UIElement current in ElementTreeWalker.PreOrder(element, ElementChildRole.Visual))
        {
            root.Scheduler.CompleteQueuedMeasure(current);
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
            root.Scheduler.CompleteQueuedArrange(current);
        }
    }

    // The panel inherits from this presenter, so the propagation starts here (see UIRoot.ProcessInheritedAndAspectForSubtree).
    private void ProcessInheritedAndAspectForSubtree(UIElement? element)
    {
        if (element?.Root is UIRoot root)
        {
            root.ProcessInheritedAndAspectForSubtree(this, element);
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

    // This measure covers the presenter's own queued measure and the measure work that only propagated to its
    // ancestors. An ancestor's own queued measure, for example from a property of that ancestor that changed in the
    // same frame, is not covered by this measure, so it stays queued for the scheduler.
    private void RemoveMeasureWorkForLayoutScope()
    {
        if (Root is not UIRoot root)
        {
            return;
        }

        root.Scheduler.CompleteQueuedMeasure(this);
        for (UIElement? current = VisualParent; current is not null; current = current.VisualParent)
        {
            root.Scheduler.CompletePropagatedQueuedMeasure(current);
        }
    }

    // The measure propagated from this presenter, which is the work its queued Inherited entry stands for. It did
    // not process this presenter's Aspect or the work queued on its ancestors, for example by a ContentPresenter
    // that created this subtree during the same measure, so that work stays queued for the next frame.
    private void RemoveInheritedWorkOfThisPresenter()
    {
        if (Root is not UIRoot root)
        {
            return;
        }

        root.Scheduler.BeginInheritedDescendantPropagation(this);
    }

    private readonly record struct TemplatedItemElement(object? Item, UIElement Element);
}
