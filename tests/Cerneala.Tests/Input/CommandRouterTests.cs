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
        root.CommandBindings.Add(new CommandBinding(command, null, (_, _) => calls.Add("root")));
        parent.CommandBindings.Add(new CommandBinding(command, null, (_, args) =>
        {
            calls.Add("parent");
            args.CanExecute = true;
            args.Handled = true;
        }));
        child.CommandBindings.Add(new CommandBinding(command, null, (_, _) => calls.Add("child")));

        bool canExecute = new CommandRouter().CanExecute(new RoutedCommandContext(command, child, map));

        Assert.True(canExecute);
        Assert.Equal(["child", "parent"], calls);
    }

    // As WPF's CommandBinding.CanExecute, a binding answers only the bubbling CanExecuteEvent, so each callback runs
    // once per query, not once for the preview pass and again for the bubbling pass (#1034).
    [Fact]
    public void CanExecuteRunsEachBindingOnceWhenNothingHandlesIt()
    {
        UIRoot root = BuildTree(out UIElement parent, out UIElement child, out ElementInputRouteMap map);
        RoutedCommand command = new("Save", typeof(CommandRouterTests));
        List<string> calls = [];
        root.CommandBindings.Add(new CommandBinding(command, null, (_, _) => calls.Add("root")));
        parent.CommandBindings.Add(new CommandBinding(command, null, (_, _) => calls.Add("parent")));
        child.CommandBindings.Add(new CommandBinding(command, null, (_, args) =>
        {
            calls.Add("child");
            args.CanExecute = true;
        }));

        bool canExecute = new CommandRouter().CanExecute(new RoutedCommandContext(command, child, map));

        Assert.True(canExecute);
        Assert.Equal(["child", "parent", "root"], calls);
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
    public void HandledExecuteStopsTheBubble()
    {
        UIRoot root = BuildTree(out _, out UIElement child, out ElementInputRouteMap map);
        RoutedCommand command = new("Save", typeof(CommandRouterTests));
        List<string> calls = [];
        child.CommandBindings.Add(new CommandBinding(command, (_, args) =>
        {
            calls.Add("child");
            args.Handled = true;
        }, (_, args) =>
        {
            args.CanExecute = true;
            args.Handled = true;
        }));
        root.CommandBindings.Add(new CommandBinding(command, (_, _) => calls.Add("root")));

        bool didExecute = new CommandRouter().Execute(new RoutedCommandContext(command, child, map));

        Assert.True(didExecute);
        Assert.Equal(["child"], calls);
    }

    [Fact]
    public void CommandBindingHandlersReceiveTheElementOwningTheBindingAsSenderAndSource()
    {
        BuildTree(out UIElement parent, out UIElement child, out ElementInputRouteMap map);
        RoutedCommand command = new("Save", typeof(CommandRouterTests));
        List<(object Sender, object Source, object OriginalSource)> canExecuteCalls = [];
        List<(object Sender, object Source, object OriginalSource)> executedCalls = [];
        parent.CommandBindings.Add(new CommandBinding(
            command,
            (sender, args) => executedCalls.Add((sender, args.Source, args.OriginalSource)),
            (sender, args) =>
            {
                canExecuteCalls.Add((sender, args.Source, args.OriginalSource));
                args.CanExecute = true;
            }));

        bool didExecute = new CommandRouter().Execute(new RoutedCommandContext(command, child, map));

        Assert.True(didExecute);
        Assert.NotEmpty(canExecuteCalls);
        Assert.NotEmpty(executedCalls);
        Assert.All(canExecuteCalls.Concat(executedCalls), call =>
        {
            Assert.Same(parent, call.Sender);
            Assert.Same(parent, call.Source);
            Assert.Same(child, call.OriginalSource);
        });
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
