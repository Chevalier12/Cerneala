using Cerneala.UI.Controls;

namespace Cerneala.UI.Accessibility;

public sealed class MenuItemAutomationPeer : AutomationPeer
{
    private readonly MenuItem menuItem;

    public MenuItemAutomationPeer(MenuItem menuItem)
        : base(menuItem)
    {
        this.menuItem = menuItem;
    }

    public override SemanticsRole Role => SemanticsRole.MenuItem;

    public override string? Name =>
        AccessibleName.GetName(menuItem) ?? AccessibleName.GetContentText(menuItem.Header);

    public override IReadOnlyDictionary<SemanticsProperty, object?> GetProperties()
    {
        Dictionary<SemanticsProperty, object?> properties = new(base.GetProperties())
        {
            [SemanticsProperty.ItemCount] = menuItem.ItemCount,
            [SemanticsProperty.IsExpanded] = menuItem.IsSubmenuOpen
        };
        return properties;
    }
}
