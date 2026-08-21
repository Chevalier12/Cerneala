using Cerneala.UI.Automation;
using Cerneala.UI.Controls;
using Cerneala.UI.Input;

namespace Cerneala.MenuLab;

public partial class MainWindow : Window<MenuLabViewModel>
{
    private bool initialized;

    private void OnContentRendered(object? sender, EventArgs args)
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
        ViewModel.CloseRequested += OnCloseRequested;
        AutomationProperties.SetAutomationId(FileMenuItem, "file-menu");
        AutomationProperties.SetAutomationId(RecentMenuItem, "recent-menu");
        AutomationProperties.SetAutomationId(ExitMenuItem, "exit-item");
        AutomationProperties.SetAutomationId(EdgeMenuItem, "edge-menu");
    }

    private void OnFrameRendered(object? sender, EventArgs args)
    {
        if (!initialized)
        {
            return;
        }

        bool sessionOpen = FileMenuItem.IsSubmenuOpen ||
            EditMenuItem.IsSubmenuOpen ||
            ViewMenuItem.IsSubmenuOpen ||
            HelpMenuItem.IsSubmenuOpen ||
            EdgeMenuItem.IsSubmenuOpen;
        ViewModel.UpdateMenuSession(sessionOpen);
    }

    private void OnSubmenuOpened(UiElementId sender, RoutedEventArgs args)
    {
        ViewModel.RecordSubmenuOpened(HeaderOf(args));
    }

    private void OnSubmenuClosed(UiElementId sender, RoutedEventArgs args)
    {
        ViewModel.RecordSubmenuClosed(HeaderOf(args));
    }

    private void OnCloseRequested(object? sender, EventArgs args)
    {
        Close();
    }

    private static string HeaderOf(RoutedEventArgs args)
    {
        return args.OriginalSource is MenuItem item
            ? item.Header?.ToString() ?? "MenuItem"
            : "MenuItem";
    }
}
