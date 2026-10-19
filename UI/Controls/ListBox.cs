using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;
using Cerneala.UI.Rendering;

namespace Cerneala.UI.Controls;

public class ListBox : Selector
{
    public ListBox()
    {
        ItemsPanel = new StackPanel();
    }

    protected override Type DefaultContainerType => typeof(ListBoxItem);

    protected internal override Type GetContainerTypeForItem(object? item)
    {
        return item is ListBoxItem ? item.GetType() : typeof(ListBoxItem);
    }

    protected internal override UIElement CreateItemContainer(int index, object? item)
    {
        return item is ListBoxItem element ? element : new ListBoxItem();
    }

    internal override bool IsItemItsOwnContainer(object? item)
    {
        return item is ListBoxItem;
    }

    // The template goes to the row before its content, so the row presents the new content with it once.
    protected override void PrepareItemContent(UIElement container, int index, object? item)
    {
        if (container is ListBoxItem listBoxItem)
        {
            listBoxItem.SetItemPresentation(ItemTemplate, ItemTemplateKey, ContentTemplateRegistry, index);
        }

        base.PrepareItemContent(container, index, item);
    }

    protected internal override void ClearItemContainer(UIElement container)
    {
        base.ClearItemContainer(container);
        if (container is ListBoxItem listBoxItem)
        {
            listBoxItem.SetItemPresentation(null, null, null, -1);
        }
    }

    // As the outer border of the WPF ListBox template, a list without a template fills its bounds with Background
    // behind its items; a ComponentTemplate draws its own chrome.
    protected override void OnRender(RenderContext context)
    {
        if (TemplateChild is null)
        {
            Border.RenderBackground(this, context);
        }
    }
}
