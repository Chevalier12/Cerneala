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
    private readonly ContentPresenter selectedContentPresenter = new();

    // The tab whose Content the content area was last prepared to show.
    private TabItem? presentedTab;

    public TabControl()
    {
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

    internal void OnTabContentChanged(TabItem tab)
    {
        if (ReferenceEquals(tab, SelectedTabItem))
        {
            InvalidateSelectedContent("Selected tab content changed");
        }
    }

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        if (TemplateChild is not null)
        {
            return base.MeasureCore(context);
        }

        LayoutSize headerSize = ItemsPresenter.Measure(context);
        TabItem? previousTab = UpdateSelectedContent();
        LayoutSize available = context.AvailableSize;
        float contentHeight = available.IsHeightUnconstrained
            ? float.PositiveInfinity
            : MathF.Max(0, available.Height - headerSize.Height);
        LayoutSize contentSize = selectedContentPresenter.Measure(
            new MeasureContext(new LayoutSize(available.Width, contentHeight), context.Rounding));
        // Measuring the content area removed the content of the tab shown before; that tab owns it again.
        if (!ReferenceEquals(previousTab, presentedTab))
        {
            previousTab?.HoldContent();
        }

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
        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, SelectedIndexProperty))
        {
            InvalidateSelectedContent("Selected tab changed");
        }
    }

    // The only place the content area changes: it runs in measure, after the tabs are realized, so a UIElement
    // content joins the content area while its tab is selected and leaves it when another tab is selected. The
    // selected tab lets go of its content first, so the content area can take it; returns the tab shown before.
    private TabItem? UpdateSelectedContent()
    {
        TabItem? previousTab = presentedTab;
        TabItem? tab = SelectedTabItem;
        presentedTab = tab;
        tab?.ReleaseContentForContentArea();
        selectedContentPresenter.PrepareItemPresentation(tab?.Content, SelectedContentTemplate, null, null, -1);
        return previousTab;
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

    // As in WPF, ItemTemplate is the template of the header of a generated tab, and the item is its Content.
    protected override void PrepareItemContent(UIElement container, int index, object? item)
    {
        if (container is not TabItem tab)
        {
            base.PrepareItemContent(container, index, item);
            return;
        }

        tab.HeaderTemplate = ItemTemplate;
        tab.HeaderTemplateKey = ItemTemplateKey;
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
            tab.HeaderTemplate = null;
            tab.HeaderTemplateKey = null;
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
