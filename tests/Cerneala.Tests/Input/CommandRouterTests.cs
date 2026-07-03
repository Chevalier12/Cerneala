using Cerneala.UI.Elements;
using Cerneala.UI.Input;

namespace Cerneala.Tests.Input;

public sealed class CommandRouterTests
{
    [Fact]
    public void CanExecuteUsesRetainedRouteOrder()
    {
        UIRoot root = BuildTree(out UIElement parent, out UIElement child, out ElementInputRouteMap map);
        RoutedCommand command = new("Save", typeof(CommandRouterTests));
        List<string> calls = [];
        root.CommandBindings.Add(new CommandBinding(command, null, (_, _) => calls.Add("preview-root")));
        parent.CommandBindings.Add(new CommandBinding(command, null, (_, _) => calls.Add("preview-parent")));
        child.CommandBindings.Add(new CommandBinding(command, null, (_, args) =>
        {
            calls.Add("preview-child");
            args.CanExecute = true;
            args.Handled = true;
        }));

        bool canExecute = new CommandRouter().CanExecute(new RoutedCommandContext(command, child, map));

        Assert.True(canExecute);
        Assert.Equal(["preview-root", "preview-parent", "preview-child"], calls);
    }

    [Fact]
    public void CanExecuteBubblesWhenPreviewIsNotHandled()
    {
        UIRoot root = BuildTree(out UIElement parent, out UIElement child, out ElementInputRouteMap map);
        RoutedCommand command = new("Save", typeof(CommandRouterTests));
        List<string> calls = [];
        root.CommandBindings.Add(new CommandBinding(command, null, (_, _) => calls.Add("preview-root")));
        parent.CommandBindings.Add(new CommandBinding(command, null, (_, _) => calls.Add("preview-parent")));
        child.CommandBindings.Add(new CommandBinding(command, null, (_, args) =>
        {
            calls.Add("preview-child");
            args.CanExecute = true;
        }));

        bool canExecute = new CommandRouter().CanExecute(new RoutedCommandContext(command, child, map));

        Assert.True(canExecute);
        Assert.Equal(
            ["preview-root", "preview-parent", "preview-child", "preview-child", "preview-parent", "preview-root"],
            calls);
    }

    [Fact]
    public void MissingTargetCannotExecute()
    {
        UIRoot root = BuildTree(out _, out _, out ElementInputRouteMap map);
        RoutedCommand command = new("Save", typeof(CommandRouterTests));

        bool canExecute = new CommandRouter().CanExecute(new RoutedCommandContext(command, new UIElement(), map));

        Assert.False(canExecute);
        _ = root;
    }

    [Fact]
    public void ExecuteSkipsWhenCanExecuteIsFalse()
    {
        BuildTree(out _, out UIElement child, out ElementInputRouteMap map);
        RoutedCommand command = new("Save", typeof(CommandRouterTests));
        bool executed = false;
        child.CommandBindings.Add(new CommandBinding(command, (_, _) => executed = true));

        bool didExecute = new CommandRouter().Execute(new RoutedCommandContext(command, child, map));

        Assert.False(didExecute);
        Assert.False(executed);
    }

    [Fact]
    public void HandledPreviewExecuteSuppressesBubble()
    {
        UIRoot root = BuildTree(out _, out UIElement child, out ElementInputRouteMap map);
        RoutedCommand command = new("Save", typeof(CommandRouterTests));
        List<string> calls = [];
        child.CommandBindings.Add(new CommandBinding(command, (_, args) =>
        {
            calls.Add("preview-child");
            args.Handled = true;
        }, (_, args) =>
        {
            args.CanExecute = true;
            args.Handled = true;
        }));
        root.CommandBindings.Add(new CommandBinding(command, (_, _) => calls.Add("preview-root")));

        bool didExecute = new CommandRouter().Execute(new RoutedCommandContext(command, child, map));

        Assert.True(didExecute);
        Assert.Equal(["preview-root", "preview-child"], calls);
    }

    private static UIRoot BuildTree(out UIElement parent, out UIElement child, out ElementInputRouteMap map)
    {
        UIRoot root = new();
        parent = new UIElement();
        child = new UIElement();
        root.VisualChildren.Add(parent);
        parent.VisualChildren.Add(child);
        map = new ElementInputRouteBuilder().Build(root);
        return root;
    }
}
