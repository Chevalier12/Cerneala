using Cerneala.UI.Controls.Items;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Controls;

public class ListBox : Selector
{
    public ListBox()
    {
        ItemsPanel = new ItemsPanelTemplate(() => new StackPanel());
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
}
