using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using MenuLabViewModel = Cerneala.MenuLab.MenuLabViewModel;
using MenuLabWindow = Cerneala.MenuLab.MainWindow;
using ServoApi = Cerneala.UI.Servo.Servo;
using ServoTarget = Cerneala.UI.Servo.ServoTarget;

namespace Cerneala.Tests.Playground;

public sealed class MenuLabSubmenuActivityTests
{
    [Fact]
    public async Task OpeningAndClosingANestedSubmenuLogsOneActivityLinePerTransition()
    {
        // Issue #1013: File and Recent both handle the bubbling SubmenuOpened/SubmenuClosed events
        // (MenuItem.md: "Occurs once when the effective submenu overlay opens."), so the File handler
        // must not log Recent's bubbled event a second time.
        UIRoot root = new(1100, 720);
        MenuLabViewModel viewModel = new();
        MenuLabWindow window = new(viewModel);
        root.LogicalChildren.Add(window);
        root.VisualChildren.Add(window);
        UiHost host = new(new UiHostOptions { Root = root, Viewport = new(1100, 720) });
        host.Update(new InputFrame(PointerSnapshot.Empty, PointerSnapshot.Empty,
            KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []), host.Viewport, TimeSpan.Zero);
        MenuItem[] topLevel = Descendants(root).OfType<MenuItem>().ToArray();
        MenuItem edit = topLevel.Single(item => Equals(item.Header, "Edit"));
        MenuItem markChanged = edit.Items.OfType<MenuItem>().Single(item => Equals(item.Header, "Mark as changed"));
        MenuItem file = topLevel.Single(item => Equals(item.Header, "File"));
        MenuItem recent = file.Items.OfType<MenuItem>().Single(item => Equals(item.Header, "Recent"));
        ServoApi.SetId(edit, "edit-menu");
        ServoApi.SetId(markChanged, "mark-changed-item");
        ServoApi.SetId(file, "file-menu");
        ServoApi.SetId(recent, "recent-menu");
        List<string> activity = [];
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(MenuLabViewModel.ActivityText))
            {
                activity.Add(viewModel.ActivityText.Split(Environment.NewLine)[^1]);
            }
        };
        ServoApi servo = new(host);

        // Works around #851: while the document is clean, the command refresh disables Save when File opens, and that
        // closes the whole File menu. "Mark as changed" keeps Save enabled.
        await servo.ClickAsync(ServoTarget.ById("edit-menu"));
        await servo.ClickAsync(ServoTarget.ById("mark-changed-item"));
        await servo.ClickAsync(ServoTarget.ById("file-menu"));
        await servo.ClickAsync(ServoTarget.ById("recent-menu"));
        Assert.True(recent.IsSubmenuOpen);
        Assert.Equal("opened: Recent", viewModel.Lifecycle);
        await servo.ClickAsync(ServoTarget.ById("file-menu"));
        Assert.False(file.IsSubmenuOpen);
        Assert.False(recent.IsSubmenuOpen);

        Assert.Equal(
            ["Opened submenu: File", "Opened submenu: Recent", "Closed submenu: Recent", "Closed submenu: File"],
            activity.Select(line => line[4..]).Where(line => line.Contains(" submenu: File", StringComparison.Ordinal) ||
                line.Contains(" submenu: Recent", StringComparison.Ordinal)));
    }

    private static IEnumerable<UIElement> Descendants(UIElement element)
    {
        foreach (UIElement child in element.VisualChildren)
        {
            yield return child;
            foreach (UIElement descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }
}
