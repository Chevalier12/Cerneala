using Cerneala.UI.Elements;
using Cerneala.UI.Input;

namespace Cerneala.Tests.Input;

public sealed class RoutedCommandExecutionTests
{
    [Fact]
    public void RouterExecutesRoutedCommandWhenCanExecuteIsTrue()
    {
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);
        ElementInputRouteMap map = new ElementInputRouteBuilder().Build(root);
        RoutedCommand command = new("Save", typeof(RoutedCommandExecutionTests));
        object? receivedParameter = null;
        child.CommandBindings.Add(new CommandBinding(command, (_, args) => receivedParameter = ((ExecutedRoutedEventArgs)args).Parameter, (_, args) =>
        {
            args.CanExecute = true;
            args.Handled = true;
        }));

        bool executed = new CommandRouter().Execute(new RoutedCommandContext(command, child, map, "file"));

        Assert.True(executed);
        Assert.Equal("file", receivedParameter);
    }

    [Fact]
    public void DirectExecuteFailsWithRouterRequiredMessage()
    {
        RoutedCommand command = new("Save", typeof(RoutedCommandExecutionTests));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => command.Execute(null));

        Assert.Contains("CommandRouter.Execute", exception.Message);
    }

    [Fact]
    public void DirectCanExecuteFailsWithRouterRequiredMessage()
    {
        RoutedCommand command = new("Save", typeof(RoutedCommandExecutionTests));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => command.CanExecute(null));

        Assert.Contains("CommandRouter.CanExecute", exception.Message);
    }
}
