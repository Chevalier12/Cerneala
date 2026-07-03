using System.Collections;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Virtualization;

namespace Cerneala.UI.Controls;

public class ItemsControl : Control
{
    private readonly ItemsPresenter itemsPresenter;

    public ItemsControl()
    {
        Items = new ItemCollection();
        Items.Changed += OnItemsChanged;
        ItemContainerGenerator = new ItemContainerGenerator(this);
        itemsPresenter = new ItemsPresenter
        {
            ItemsOwner = this
        };
        LogicalChildren.Add(itemsPresenter);
        VisualChildren.Add(itemsPresenter);
    }

    public static readonly UiProperty<DataTemplate?> ItemTemplateProperty = UiProperty<DataTemplate?>.Register(
        nameof(ItemTemplate),
        typeof(ItemsControl),
        new UiPropertyMetadata<DataTemplate?>(null, UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<ItemsPanelTemplate?> ItemsPanelProperty = UiProperty<ItemsPanelTemplate?>.Register(
        nameof(ItemsPanel),
        typeof(ItemsControl),
        new UiPropertyMetadata<ItemsPanelTemplate?>(null, UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender));

    public ItemCollection Items { get; }

    public ItemContainerGenerator ItemContainerGenerator { get; }

    public ItemsPresenter ItemsPresenter => itemsPresenter;

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

    public void SetItems(IEnumerable? items)
    {
        Items.ReplaceWith(items);
    }

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        if (TemplateChild is not null)
        {
            return base.MeasureCore(context);
        }

        return itemsPresenter.Measure(context);
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        if (TemplateChild is not null)
        {
            return base.ArrangeCore(context);
        }

        itemsPresenter.Arrange(context);
        return context.FinalRect;
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, ItemTemplateProperty) ||
            ReferenceEquals(args.Property, ItemsPanelProperty))
        {
            ItemContainerGenerator.Clear();
            itemsPresenter.MarkItemsDirty();
            InvalidateItems("ItemsControl item policy changed");
        }
    }

    protected virtual Type DefaultContainerType => typeof(ContentPresenter);

    protected internal virtual Type GetContainerTypeForItem(object? item)
    {
        return item is UIElement element ? element.GetType() : DefaultContainerType;
    }

    protected internal virtual UIElement CreateItemContainer(int index, object? item)
    {
        return item is UIElement element ? element : new ContentPresenter();
    }

    protected internal virtual void PrepareItemContainer(UIElement container, int index, object? item)
    {
        bool selected = IsItemSelected(index);
        ItemContainerGenerator.SetInfo(container, index, item, selected);
        if (container is ISelectableItemContainer selectable)
        {
            selectable.ItemIndex = index;
            selectable.Item = item;
            selectable.IsSelected = selected;
        }

        if (ReferenceEquals(container, item))
        {
            return;
        }

        switch (container)
        {
            case ContentPresenter presenter:
                presenter.Content = item;
                presenter.ContentTemplate = ItemTemplate;
                break;
            case ContentControl contentControl:
                contentControl.Content = item;
                break;
        }
    }

    protected internal virtual void ClearItemContainer(UIElement container)
    {
        ItemContainerGenerator.ClearInfo(container);
        if (container is ISelectableItemContainer selectable)
        {
            selectable.ItemIndex = -1;
            selectable.Item = null;
            selectable.IsSelected = false;
        }

        switch (container)
        {
            case ContentPresenter presenter:
                presenter.Content = null;
                presenter.ContentTemplate = null;
                break;
            case ContentControl contentControl:
                contentControl.Content = null;
                break;
        }
    }

    protected internal virtual bool IsItemSelected(int index)
    {
        return false;
    }

    protected internal virtual void OnItemContainerPrepared(UIElement container, int index)
    {
    }

    public void SetVirtualizationContext(VirtualizationContext? context)
    {
        itemsPresenter.VirtualizationContext = context;
        itemsPresenter.MarkItemsDirty();
        InvalidateItems("Items virtualization context changed");
    }

    public void UpdateVirtualizationFromScrollInfo(IScrollInfo scrollInfo, float itemExtent, int cacheItems = 0)
    {
        itemsPresenter.UpdateVirtualizationFromScrollInfo(scrollInfo, itemExtent, cacheItems);
        InvalidateItems("Items scroll virtualization changed");
    }

    internal void InvalidateItems(string reason)
    {
        IncrementLayoutVersion();
        IncrementRenderVersion();
        Invalidate(InvalidationFlags.Measure | InvalidationFlags.Arrange | InvalidationFlags.Render | InvalidationFlags.HitTest, reason);
    }

    private void OnItemsChanged(object? sender, EventArgs args)
    {
        itemsPresenter.MarkItemsDirty();
        InvalidateItems("Items changed");
    }
}

public interface ISelectableItemContainer
{
    int ItemIndex { get; set; }

    object? Item { get; set; }

    bool IsSelected { get; set; }
}
