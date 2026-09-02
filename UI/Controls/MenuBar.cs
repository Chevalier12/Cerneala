using Cerneala.UI.Core;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;

namespace Cerneala.UI.Controls;

public class MenuBar : Menu
{
    public MenuBar()
    {
        ItemsPanel = new global::Cerneala.UI.Layout.Panels.StackPanel { Orientation = Orientation.Horizontal };
        SetFrameworkDefault(ComponentTemplateProperty, MenuTemplates.MenuBar);
    }
}
