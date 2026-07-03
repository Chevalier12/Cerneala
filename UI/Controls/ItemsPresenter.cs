using System.Collections;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

namespace Cerneala.UI.Controls;

public class ItemsPresenter : Control
{
    private static readonly ItemsPanelTemplate DefaultItemsPanelTemplate = new(() => new Panel());
    private Panel? panelRoot;
    private bool itemsDirty = true;

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

    public Panel? PanelRoot => panelRoot;

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        RefreshItems();
        return panelRoot?.Measure(context) ?? LayoutSize.Zero;
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        RefreshItems();
        panelRoot?.Arrange(context);
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
        if (!itemsDirty)
        {
            return;
        }

        itemsDirty = false;
        Panel nextPanel = (ItemsPanel ?? DefaultItemsPanelTemplate).CreatePanel();
        List<UIElement> nextChildren = [.. CreateItemChildren()];
        Panel? oldPanel = panelRoot;
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

    private static void AddPanelChild(Panel panel, UIElement child)
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

    private void AddPanelRoot(Panel panel)
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

    private void RemovePanelRoot(Panel panel)
    {
        VisualChildren.Remove(panel);
        LogicalChildren.Remove(panel);
    }

    private static void ClearPanelChildren(Panel panel)
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

    private IEnumerable<UIElement> CreateItemChildren()
    {
        if (Items is null)
        {
            yield break;
        }

        foreach (object? item in Items)
        {
            UIElement? child = item as UIElement ?? ItemTemplate?.CreateElement(item);
            if (child is not null)
            {
                yield return child;
            }
        }
    }
}
