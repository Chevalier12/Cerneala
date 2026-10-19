using System.Runtime.ExceptionServices;
using Cerneala.UI.Controls.Items;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;

namespace Cerneala.UI.Controls;

public class TabControl : Selector
{
    public static readonly UiProperty<ContentTemplate?> ContentTemplateProperty = UiProperty<ContentTemplate?>.Register(
        nameof(ContentTemplate),
        typeof(TabControl),
        new UiPropertyMetadata<ContentTemplate?>(null, UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender));

    // Shows the content of the selected tab below the header row; a TabItem never hosts its own Content.
    private readonly ContentPresenter selectedContentPresenter;

    // The tab whose Content the content area was last prepared to show.
    private TabItem? presentedTab;

    public TabControl()
    {
        selectedContentPresenter = new SelectedContentPresenter(this);
        ItemsPanel = new StackPanel { Orientation = Layout.Orientation.Horizontal };
        LogicalChildren.Add(selectedContentPresenter);
        VisualChildren.Add(selectedContentPresenter);
    }

    // The template of the content area when the selected tab has no ContentTemplate of its own.
    public ContentTemplate? ContentTemplate
    {
        get => GetValue(ContentTemplateProperty);
        set => SetValue(ContentTemplateProperty, value);
    }

    public object? SelectedContent => SelectedTabItem?.Content;

    public ContentTemplate? SelectedContentTemplate =>
        SelectedTabItem is { } tab ? tab.ContentTemplate ?? ContentTemplate : null;

    protected override Type DefaultContainerType => typeof(TabItem);

    // Realized containers keep their old indexes until the next layout pass, so the container at the
    // selected index can belong to another item; the container prepared for the selected item is used instead.
    public TabItem? SelectedTabItem
    {
        get
        {
            int selectedIndex = SelectedIndex;
            if (selectedIndex < 0 || selectedIndex >= ItemCount)
            {
                return null;
            }

            object? item = GetItemAt(selectedIndex);
            if (item is TabItem tab)
            {
                return tab;
            }

            IReadOnlyDictionary<int, UIElement> realized = ItemContainerGenerator.RealizedContainers;
            if (realized.TryGetValue(selectedIndex, out UIElement? container) &&
                Equals(ItemContainerGenerator.GetItem(container), item))
            {
                return container as TabItem;
            }

            foreach (UIElement candidate in realized.Values)
            {
                if (Equals(ItemContainerGenerator.GetItem(candidate), item))
                {
                    return candidate as TabItem;
                }
            }

            return null;
        }
    }

    private bool IsComponentTemplated => ComponentTemplate is not null || ComponentTemplateKey is not null;

    internal void OnTabContentChanged(TabItem tab)
    {
        if (ReferenceEquals(tab, SelectedTabItem))
        {
            if (IsComponentTemplated)
            {
                tab.ReleaseContentForContentArea();
            }

            InvalidateSelectedContent("Selected tab content changed");
        }
    }

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        if (TemplateChild is not null)
        {
            LayoutSize size = base.MeasureCore(context);
            // The template may have let go of the content of a tab shown before; that tab owns it again.
            UpdateTemplatedContentOwnership();
            return size;
        }

        LayoutSize headerSize = ItemsPresenter.Measure(context);
        UpdateSelectedContent();
        LayoutSize available = context.AvailableSize;
        float contentHeight = available.IsHeightUnconstrained
            ? float.PositiveInfinity
            : MathF.Max(0, available.Height - headerSize.Height);
        LayoutSize contentSize = selectedContentPresenter.Measure(
            new MeasureContext(new LayoutSize(available.Width, contentHeight), context.Rounding));
        return new LayoutSize(MathF.Max(headerSize.Width, contentSize.Width), headerSize.Height + contentSize.Height);
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        if (TemplateChild is not null)
        {
            return base.ArrangeCore(context);
        }

        LayoutRect final = context.FinalRect;
        float headerHeight = MathF.Min(ItemsPresenter.DesiredSize.Height, final.Height);
        ItemsPresenter.Arrange(new ArrangeContext(new LayoutRect(final.X, final.Y, final.Width, headerHeight), context.Rounding));
        selectedContentPresenter.Arrange(new ArrangeContext(
            new LayoutRect(final.X, final.Y + headerHeight, final.Width, MathF.Max(0, final.Height - headerHeight)),
            context.Rounding));
        return final;
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        bool templateChanged = ReferenceEquals(args.Property, ComponentTemplateProperty) ||
            ReferenceEquals(args.Property, ComponentTemplateKeyProperty);
        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, SelectedIndexProperty))
        {
            InvalidateSelectedContent("Selected tab changed");
        }

        if (templateChanged || (ReferenceEquals(args.Property, SelectedIndexProperty) && IsComponentTemplated))
        {
            UpdateTemplatedContentOwnership();
        }
    }

    // From now on the template shows SelectedContent: the content area and the selected tab let go of a UIElement
    // content before the template is built, so a ContentPresenter in the template can take it. This also holds when a
    // ComponentTemplateKey starts to resolve after the content area showed the content.
    internal override void OnTemplateInstanceCreating()
    {
        presentedTab = null;
        selectedContentPresenter.PrepareItemPresentation(null, null, null, null, -1);
        SelectedTabItem?.ReleaseContentForContentArea();
    }

    // The template that goes away lets go of the element content of the tabs it showed, so the next template or the
    // content area can show it, and its tab can own it again.
    protected override void OnTemplateApplied(ComponentTemplateInstance? instance)
    {
        base.OnTemplateApplied(instance);
        if (instance is not null)
        {
            return;
        }

        foreach (TabItem tab in GetTabs())
        {
            if (tab.Content is UIElement element && element.LogicalParent is not null && !ReferenceEquals(element.LogicalParent, tab))
            {
                ContentControl.DetachChildFromOwnedSubtree(this, element);
            }
        }
    }

    // With a ComponentTemplate, the template shows SelectedContent, so the selected tab lets go of a UIElement content
    // and every other tab owns its content again once nothing shows it. Without one, every tab owns a content that
    // nothing shows, and the content area takes the selected tab's content in measure.
    private void UpdateTemplatedContentOwnership()
    {
        TabItem? selected = IsComponentTemplated ? SelectedTabItem : null;
        foreach (TabItem tab in GetTabs())
        {
            if (!ReferenceEquals(tab, selected))
            {
                tab.HoldContent();
            }
        }

        selected?.ReleaseContentForContentArea();
    }

    // TabItem items, which a templated control may never realize, and the realized generated tabs.
    private IEnumerable<TabItem> GetTabs()
    {
        for (int index = 0; index < ItemCount; index++)
        {
            if (GetItemAt(index) is TabItem tab)
            {
                yield return tab;
            }
        }

        foreach (UIElement container in ItemContainerGenerator.RealizedContainers.Values)
        {
            if (container is TabItem tab && !ReferenceEquals(ItemContainerGenerator.GetItem(container), container))
            {
                yield return tab;
            }
        }
    }

    // The only place the content area changes: it runs in measure, after the tabs are realized, so a UIElement
    // content joins the content area while its tab is selected and leaves it when another tab is selected.
    private void UpdateSelectedContent()
    {
        TabItem? previousTab = presentedTab;
        TabItem? tab = SelectedTabItem;
        presentedTab = tab;
        selectedContentPresenter.PrepareItemPresentation(tab?.Content, SelectedContentTemplate, null, null, -1);
        // Changing the Content of the content area let go of the content of the tab shown before at once; that tab
        // owns it again before the content area is measured, so a template that throws while it is measured cannot
        // leave that content without an owner.
        if (!ReferenceEquals(previousTab, tab))
        {
            previousTab?.HoldContent();
        }
    }

    // The content area. Whenever it builds its presentation again, in measure or when its aspect content templates
    // change, the selected tab lets go of its UIElement content first, so the content area or its template can take
    // it, and owns it again when the new presentation did not take it, for example when a ContentTemplate builds an
    // element that does not contain the content.
    private sealed class SelectedContentPresenter(TabControl owner) : ContentPresenter
    {
        internal override void OnPresentationRefreshing(object? content)
        {
            if (owner.presentedTab is { } tab && content is UIElement && ReferenceEquals(tab.Content, content))
            {
                tab.ReleaseContentForContentArea();
            }
        }

        internal override void OnPresentationRefreshed(object? content)
        {
            if (owner.presentedTab is { } tab && content is UIElement && ReferenceEquals(tab.Content, content))
            {
                tab.HoldContent();
            }
        }
    }

    private void InvalidateSelectedContent(string reason)
    {
        Invalidate(InvalidationFlags.Measure | InvalidationFlags.Arrange | InvalidationFlags.Render, reason);
    }

    protected internal override void PrepareItemContainer(UIElement container, int index, object? item)
    {
        base.PrepareItemContainer(container, index, item);
        if (container is TabItem tab)
        {
            tab.Owner = this;
        }
    }

    // As in WPF, ItemTemplate is the template of the header of a generated tab, and the item is its Content. Like
    // WPF's PrepareHeaderedContentControl, a null ItemTemplate or ItemTemplateKey is not written, so a HeaderTemplate or
    // HeaderTemplateKey from ItemContainerAspect applies. Changing either clears every container first, and clearing
    // removes the value written here, so a tab is never prepared over a value from an older ItemTemplate.
    protected override void PrepareItemContent(UIElement container, int index, object? item)
    {
        if (container is not TabItem tab)
        {
            base.PrepareItemContent(container, index, item);
            return;
        }

        if (ItemTemplate is { } itemTemplate)
        {
            tab.HeaderTemplate = itemTemplate;
        }

        if (ItemTemplateKey is { } itemTemplateKey)
        {
            tab.HeaderTemplateKey = itemTemplateKey;
        }

        tab.SetHeaderPresentation(ContentTemplateRegistry, index);
        tab.Header = item is UIElement ? null : ItemTemplate is null ? GetItemDisplayValue(item) : item;
        tab.Content = item;
    }

    protected internal override void ClearItemContainer(UIElement container)
    {
        bool generated = !ReferenceEquals(ItemContainerGenerator.GetItem(container), container);
        base.ClearItemContainer(container);
        if (container is not TabItem tab)
        {
            return;
        }

        if (ReferenceEquals(tab.Owner, this))
        {
            tab.Owner = null;
        }

        if (generated)
        {
            tab.Header = null;
            tab.ClearValue(TabItem.HeaderTemplateProperty);
            tab.ClearValue(TabItem.HeaderTemplateKeyProperty);
            tab.SetHeaderPresentation(null, -1);
        }
    }

    protected internal override Type GetContainerTypeForItem(object? item)
    {
        return item is TabItem ? item.GetType() : typeof(TabItem);
    }

    protected internal override UIElement CreateItemContainer(int index, object? item)
    {
        return item is TabItem element ? element : new TabItem();
    }

    internal override bool IsItemItsOwnContainer(object? item)
    {
        return item is TabItem;
    }

    // As in WPF, removing the selected tab selects the tab that takes its place, or the
    // previous tab when the removed tab was the last one. Clearing the selection can throw after the items
    // changed, for example when a container's re-preparation fails; the next tab is still selected, and the
    // first failure propagates after it.
    internal override void OnItemsReplaced(int index, int removedCount, int addedCount)
    {
        int selectedIndex = SelectedIndex;
        ExceptionDispatchInfo? failure = null;
        try
        {
            base.OnItemsReplaced(index, removedCount, addedCount);
        }
        catch (Exception exception)
        {
            failure = ExceptionDispatchInfo.Capture(exception);
        }

        if (addedCount == 0 && selectedIndex >= index && selectedIndex < index + removedCount && SelectedIndex < 0)
        {
            int nextIndex = FindSelectableTab(Math.Min(index, ItemCount - 1));
            if (nextIndex >= 0)
            {
                try
                {
                    SelectedIndex = nextIndex;
                }
                catch (Exception exception)
                {
                    failure ??= ExceptionDispatchInfo.Capture(exception);
                }
            }
        }

        failure?.Throw();
    }

    // Searches backwards from startIndex and wraps to the last tab; a disabled or hidden tab is skipped.
    private int FindSelectableTab(int startIndex)
    {
        for (int offset = 0; offset < ItemCount; offset++)
        {
            int index = (startIndex - offset + ItemCount) % ItemCount;
            if (GetItemAt(index) is not TabItem tab || (tab.IsEnabled && tab.Visibility == global::Cerneala.UI.Layout.Visibility.Visible))
            {
                return index;
            }
        }

        return -1;
    }
}
