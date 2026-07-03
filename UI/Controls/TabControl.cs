using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Controls;

public class TabControl : Selector
{
    protected override Type DefaultContainerType => typeof(TabItem);

    public TabItem? SelectedTabItem
    {
        get
        {
            int selectedIndex = SelectedIndex;
            if (selectedIndex < 0 || selectedIndex >= Items.Count)
            {
                return null;
            }

            if (ItemContainerGenerator.RealizedContainers.TryGetValue(selectedIndex, out UIElement? container))
            {
                return container as TabItem;
            }

            return Items[selectedIndex] as TabItem;
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
}
