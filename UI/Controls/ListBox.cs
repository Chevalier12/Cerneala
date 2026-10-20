using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

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

    // As WPF's ListBoxItem selects itself on a press only when its Focus() succeeds, a click selects a row only when the
    // row can take the keyboard focus: a row whose Focusable is false leaves the selection as it was (#675).
    internal override bool SelectsContainerOnClick(UIElement container, MouseButtonEventArgs args)
    {
        ElementInputRouteMap? routeMap = args.RouteMap ??
            (container.Root is UIRoot root ? root.InputCache.EnsureCurrent(root) : null);
        return routeMap is not null && FocusPolicy.CanFocus(container, routeMap);
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
}
