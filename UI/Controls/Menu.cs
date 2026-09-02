using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;

namespace Cerneala.UI.Controls;

[TemplatePart("PART_ItemsPresenter", typeof(ItemsPresenter))]
public class Menu : ItemsControl
{
    private readonly MenuSession session;

    public Menu()
    {
        session = new MenuSession(this);
        Focusable = true;
        IsTabStop = true;
        ItemsPanel = new global::Cerneala.UI.Layout.Panels.StackPanel { Orientation = Orientation.Vertical };
        SetFrameworkDefault(ComponentTemplateProperty, MenuTemplates.Menu);
    }

    internal MenuSession Session => session;

    protected override Type DefaultContainerType => typeof(MenuItem);

    protected internal override Type GetContainerTypeForItem(object? item)
    {
        return item is MenuItem ? item.GetType() : typeof(MenuItem);
    }

    protected internal override UIElement CreateItemContainer(int index, object? item)
    {
        return item is MenuItem menuItem ? menuItem : new MenuItem();
    }

    protected override void PrepareItemContent(UIElement container, int index, object? item)
    {
        if (container is MenuItem menuItem)
        {
            MenuItemContainerPolicy.Prepare(this, menuItem, index, item);
            return;
        }

        base.PrepareItemContent(container, index, item);
    }

    protected internal override void OnItemContainerPrepared(UIElement container, int index)
    {
        base.OnItemContainerPrepared(container, index);
        if (container is MenuItem menuItem)
        {
            menuItem.SetMenuOwner(session, this, parent: null);
            session.OnContainerPrepared(this, menuItem);
        }
    }

    protected internal override void ClearItemContainer(UIElement container)
    {
        object? item = global::Cerneala.UI.Controls.Items.ItemContainerGenerator.GetItem(container);
        if (container is MenuItem menuItem)
        {
            menuItem.SetMenuOwner(session: null, root: null, parent: null);
            MenuItemContainerPolicy.Clear(menuItem, item);
        }

        base.ClearItemContainer(container);
    }

    internal override void OnItemsViewSourceChanged()
    {
        base.OnItemsViewSourceChanged();
        session.OnRootItemsChanged();
        ItemContainerGenerator.Clear();
    }

    protected override void OnTemplateApplied(ComponentTemplateInstance? instance)
    {
        session.CloseAll(restoreFocus: false);
        ActivateItemsPresenter(null);
        if (instance is not null)
        {
            ActivateItemsPresenter(GetRequiredTemplatePart<ItemsPresenter>("PART_ItemsPresenter"));
        }
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, IsEnabledProperty) && !IsEnabled)
        {
            session.CloseAll(restoreFocus: false);
        }
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        session.OnRootAttached();
    }

    protected override void OnDetached()
    {
        session.OnRootDetached();
        base.OnDetached();
    }
}
