using System.Runtime.ExceptionServices;
using Cerneala.UI.Controls.Items;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Controls;

public class TabControl : Selector
{
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
