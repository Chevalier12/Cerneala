using System.Runtime.ExceptionServices;
using Cerneala.UI.Controls.Items;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;

namespace Cerneala.UI.Controls;

[TemplatePart("PART_ItemsPresenter", typeof(ItemsPresenter))]
public class TabControl : Selector
{
    public static readonly UiProperty<ContentTemplate?> ContentTemplateProperty = UiProperty<ContentTemplate?>.Register(
        nameof(ContentTemplate),
        typeof(TabControl),
        new UiPropertyMetadata<ContentTemplate?>(null, UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender));

    // As in WPF, SelectedContent and SelectedContentTemplate are read-only properties that follow the selected tab, so a
    // ComponentTemplate can bind its ContentPresenter to them.
    private static readonly UiPropertyKey<object?> SelectedContentPropertyKey = UiProperty<object?>.RegisterReadOnly(
        nameof(SelectedContent),
        typeof(TabControl),
        new UiPropertyMetadata<object?>(null, UiPropertyOptions.None, ContentControl.ContentEqualityComparer));

    public static readonly UiProperty<object?> SelectedContentProperty = SelectedContentPropertyKey.Property;

    private static readonly UiPropertyKey<ContentTemplate?> SelectedContentTemplatePropertyKey =
        UiProperty<ContentTemplate?>.RegisterReadOnly(
            nameof(SelectedContentTemplate),
            typeof(TabControl),
            new UiPropertyMetadata<ContentTemplate?>(null));

    public static readonly UiProperty<ContentTemplate?> SelectedContentTemplateProperty = SelectedContentTemplatePropertyKey.Property;

    // Shows the content of the selected tab below the header row; a TabItem never hosts its own Content.
    private readonly ContentPresenter selectedContentPresenter;

    // The tab whose Content the content area was last prepared to show.
    private TabItem? presentedTab;

    // The height of the header row as last arranged without a template: the content area and its chrome start there.
    private float contentAreaTop;

    public TabControl()
    {
        selectedContentPresenter = new SelectedContentPresenter(this);
        SetFrameworkDefault(ItemsPanelProperty, new StackPanel { Orientation = Layout.Orientation.Horizontal });
        LogicalChildren.Add(selectedContentPresenter);
        VisualChildren.Add(selectedContentPresenter);
    }

    // The template of the content area when the selected tab has no ContentTemplate of its own.
    public ContentTemplate? ContentTemplate
    {
        get => GetValue(ContentTemplateProperty);
        set => SetValue(ContentTemplateProperty, value);
    }

    public object? SelectedContent => GetValue(SelectedContentProperty);

    public ContentTemplate? SelectedContentTemplate => GetValue(SelectedContentTemplateProperty);

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
            UpdateSelectedContentProperties();
            InvalidateSelectedContent("Selected tab content changed");
        }
    }

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        if (TemplateChild is not null)
        {
            LayoutSize size = base.MeasureCore(context);
            // The tab of a data item exists only once the template realized it, so SelectedContent follows it here, and
            // every other tab owns a content that nothing shows again. The template is built and measured, so the
            // selected tab owns its UIElement content again when the template did not take it.
            UpdateSelectedContentProperties();
            SelectedTabItem?.HoldContent();
            return size;
        }

        // As the ContentPanel Border of the WPF TabControl template, the content area below the header row lays out the
        // selected content inside BorderThickness and Padding; the header row is not inset.
        LayoutSize headerSize = ItemsPresenter.Measure(context);
        UpdateSelectedContent();
        LayoutSize available = context.AvailableSize;
        float contentHeight = available.IsHeightUnconstrained
            ? float.PositiveInfinity
            : MathF.Max(0, available.Height - headerSize.Height);
        Thickness insets = Insets;
        LayoutSize contentSize = selectedContentPresenter.Measure(new MeasureContext(
            ContentControl.Deflate(new LayoutSize(available.Width, contentHeight), insets),
            context.Rounding));
        LayoutSize contentAreaSize = ContentControl.Inflate(contentSize, insets);
        return new LayoutSize(MathF.Max(headerSize.Width, contentAreaSize.Width), headerSize.Height + contentAreaSize.Height);
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
        LayoutRect contentArea = new(final.X, final.Y + headerHeight, final.Width, MathF.Max(0, final.Height - headerHeight));
        selectedContentPresenter.Arrange(new ArrangeContext(ContentControl.Deflate(contentArea, Insets), context.Rounding));
        // The chrome below follows the header row, which can change height while this control keeps its bounds.
        if (headerHeight != contentAreaTop)
        {
            contentAreaTop = headerHeight;
            IncrementRenderVersion();
            Invalidate(InvalidationFlags.Render, "TabControl content area changed");
        }

        return final;
    }

    // As the ContentPanel Border of the WPF TabControl template, a TabControl without a template fills the content area
    // below the header row with Background and draws BorderBrush around it, not around the header row as the
    // ItemsControl chrome would; a ComponentTemplate draws its own chrome.
    protected override void OnRender(RenderContext context)
    {
        if (TemplateChild is null)
        {
            LayoutRect bounds = context.Bounds;
            float top = MathF.Min(contentAreaTop, MathF.Max(0, bounds.Height));
            Border.RenderBox(this, context, new LayoutRect(bounds.X, bounds.Y + top, bounds.Width, bounds.Height - top));
        }
    }

    // The header row starts at the top of a TabControl without a template, as the layout above arranges it, and
    // Padding and BorderThickness inset only the content area below it, so a scroll offset reaches the headers without
    // an inset; a ComponentTemplate has none either.
    internal override float ItemsTopInset => 0;

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        bool templateChanged = ReferenceEquals(args.Property, ComponentTemplateProperty) ||
            ReferenceEquals(args.Property, ComponentTemplateKeyProperty);
        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, SelectedIndexProperty))
        {
            UpdateSelectedContentProperties();
            InvalidateSelectedContent("Selected tab changed");
        }
        else if (ReferenceEquals(args.Property, ContentTemplateProperty))
        {
            UpdateSelectedContentProperties();
        }

        if (templateChanged)
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

    // A Selected or SelectionChanged handler can give the template's ContentPresenter the new SelectedContent, which
    // lets go of the content of the tab that was just deselected. As in WPF, where TabControl.OnSelectionChanged runs
    // after the SelectionChanged handlers, that tab owns its content again at once, not only at the next layout pass.
    internal override void OnSelectionChangeCompleted()
    {
        if (IsComponentTemplated)
        {
            UpdateTemplatedContentOwnership();
        }
    }

    // As in WPF, the template's PART_ItemsPresenter hosts the tab headers and is optional: without it, or without a
    // template, the built-in header row hosts them. The template that goes away lets go of the element content of the
    // tabs it showed, so the next template or the content area can show it, and its tab can own it again. When an
    // aspect template replaces the old one, the selected tab lets go of its content in OnTemplateInstanceCreating,
    // before the next template is built.
    protected override void OnTemplateApplied(ComponentTemplateInstance? instance)
    {
        base.OnTemplateApplied(instance);
        ActivateItemsPresenter(null);
        if (instance is not null)
        {
            ActivateItemsPresenter(GetOptionalTemplatePart<ItemsPresenter>("PART_ItemsPresenter"));
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
    // and every other tab owns its content again once nothing shows it; the next measure gives the selected tab its
    // content back when the template did not take it. Without one, every tab owns a content that nothing shows, and
    // the content area takes the selected tab's content in measure.
    private void UpdateTemplatedContentOwnership()
    {
        TabItem? selected = IsComponentTemplated ? SelectedTabItem : null;
        HoldContentOfOtherTabs(selected);
        selected?.ReleaseContentForContentArea();
    }

    // Sets SelectedContent and SelectedContentTemplate to those of the selected tab. With a ComponentTemplate, the
    // selected tab lets go of a UIElement content before SelectedContent changes, so a ContentPresenter bound to
    // SelectedContentProperty can take it at once, and every other tab owns its content again once nothing shows it.
    private void UpdateSelectedContentProperties()
    {
        TabItem? tab = SelectedTabItem;
        bool templated = IsComponentTemplated;
        if (templated)
        {
            tab?.ReleaseContentForContentArea();
        }

        SetValue(SelectedContentPropertyKey, tab?.Content);
        SetValue(SelectedContentTemplatePropertyKey, tab is null ? null : tab.ContentTemplate ?? ContentTemplate);
        if (templated)
        {
            HoldContentOfOtherTabs(tab);
        }
    }

    private void HoldContentOfOtherTabs(TabItem? selected)
    {
        foreach (TabItem tab in GetTabs())
        {
            if (!ReferenceEquals(tab, selected))
            {
                tab.HoldContent();
            }
        }
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
    // content joins the content area while its tab is selected and leaves it when another tab is selected. The tab of
    // a data item exists only once it is realized, so SelectedContent is updated here too.
    private void UpdateSelectedContent()
    {
        TabItem? previousTab = presentedTab;
        TabItem? tab = SelectedTabItem;
        presentedTab = tab;
        UpdateSelectedContentProperties();
        selectedContentPresenter.PrepareItemPresentation(SelectedContent, SelectedContentTemplate, null, null, -1);
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
