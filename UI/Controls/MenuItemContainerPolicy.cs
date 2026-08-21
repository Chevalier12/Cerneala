using Cerneala.UI.Elements;

namespace Cerneala.UI.Controls;

internal static class MenuItemContainerPolicy
{
    public static void Prepare(ItemsControl owner, MenuItem container, int index, object? item)
    {
        if (owner.ItemTemplate is null)
        {
            container.Header = owner.GetItemDisplayValue(item);
            return;
        }

        ContentPresenter presenter = container.Header as ContentPresenter ?? new ContentPresenter();
        presenter.PrepareItemPresentation(
            item,
            owner.ItemTemplate,
            owner.ItemTemplateKey,
            owner.ContentTemplateRegistry,
            index);
        container.Header = presenter;
    }

    public static void Clear(MenuItem container, object? item)
    {
        if (ReferenceEquals(container, item))
        {
            return;
        }

        if (container.Header is ContentPresenter presenter)
        {
            presenter.Content = null;
            presenter.ContentTemplate = null;
            presenter.ContentTemplateKey = null;
            presenter.LocalTemplateRegistry = null;
            presenter.ContentIndex = -1;
        }

        container.Header = null;
    }
}
