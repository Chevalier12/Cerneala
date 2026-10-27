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

    // SubmenuOpened and SubmenuClosed bubble, and parent items (File, Workspace, Edge menu) have the same handlers as
    // their nested items, so each handler records only the transition of its own item.
    private void OnSubmenuOpened(MenuItem sender, RoutedEventArgs args)
    {
        if (ReferenceEquals(args.OriginalSource, sender))
        {
            ViewModel.RecordSubmenuOpened(HeaderOf(sender));
        }
    }

    private void OnSubmenuClosed(MenuItem sender, RoutedEventArgs args)
    {
        if (ReferenceEquals(args.OriginalSource, sender))
        {
            ViewModel.RecordSubmenuClosed(HeaderOf(sender));
        }
    }

    private void OnCloseRequested(object? sender, EventArgs args)
    {
        Close();
    }

    private static string HeaderOf(MenuItem item)
    {
        return item.Header?.ToString() ?? "MenuItem";
    }
}
