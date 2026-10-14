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
            if (selectedIndex < 0 || selectedIndex >= ItemCount)
            {
                return null;
            }

            if (ItemContainerGenerator.RealizedContainers.TryGetValue(selectedIndex, out UIElement? container))
            {
                return container as TabItem;
            }

            return GetItemAt(selectedIndex) as TabItem;
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

    // As in WPF, removing the selected tab selects the tab that takes its place, or the
    // previous tab when the removed tab was the last one.
    internal override void OnItemsReplaced(int index, int removedCount, int addedCount)
    {
        int selectedIndex = SelectedIndex;
        base.OnItemsReplaced(index, removedCount, addedCount);
        if (addedCount == 0 && selectedIndex >= index && selectedIndex < index + removedCount && SelectedIndex < 0)
        {
            int nextIndex = FindSelectableTab(Math.Min(index, ItemCount - 1));
            if (nextIndex >= 0)
            {
                SelectedIndex = nextIndex;
            }
        }
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
