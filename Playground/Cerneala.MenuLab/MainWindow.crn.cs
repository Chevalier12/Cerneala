using Cerneala.UI.Controls;
using Cerneala.UI.Input;
using ServoApi = Cerneala.UI.Servo.Servo;

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
        ServoApi.SetId(FileMenuItem, "file-menu");
        ServoApi.SetId(RecentMenuItem, "recent-menu");
        ServoApi.SetId(ExitMenuItem, "exit-item");
        ServoApi.SetId(EdgeMenuItem, "edge-menu");
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
