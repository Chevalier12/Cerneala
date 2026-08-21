using Cerneala.UI.Controls;

namespace Cerneala.UI.Accessibility;

public sealed class MenuAutomationPeer : AutomationPeer
{
    private readonly Menu menu;

    public MenuAutomationPeer(Menu menu)
        : base(menu)
    {
        this.menu = menu;
    }

    public override SemanticsRole Role => menu is MenuBar
        ? SemanticsRole.MenuBar
        : SemanticsRole.Menu;

    public override IReadOnlyDictionary<SemanticsProperty, object?> GetProperties()
    {
        Dictionary<SemanticsProperty, object?> properties = new(base.GetProperties())
        {
            [SemanticsProperty.ItemCount] = menu.ItemCount
        };
        return properties;
    }
}
