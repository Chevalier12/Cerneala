using Cerneala.UI.Input;

namespace Cerneala.Tests.Input;

public sealed class CommandingTests
{
    [Fact]
    public void RoutedCommandStoresNameAndOwner()
    {
        RoutedCommand command = new("Save", typeof(CommandingTests));

        Assert.Equal("Save", command.Name);
        Assert.Equal(typeof(CommandingTests), command.OwnerType);
    }

    [Fact]
    public void CanExecuteArgsCanBeHandledByRoute()
    {
        RoutedCommand command = new("Save", typeof(CommandingTests));
        object source = new();
        CanExecuteRoutedEventArgs args = new(CommandEvents.CanExecuteEvent, source, command, "file");

        args.CanExecute = true;
        args.Handled = true;

        Assert.Same(command, args.Command);
        Assert.Equal("file", args.Parameter);
        Assert.True(args.CanExecute);
        Assert.True(args.Handled);
    }

    [Fact]
    public void CommandBindingInvokesHandlers()
    {
        RoutedCommand command = new("Save", typeof(CommandingTests));
        bool executed = false;
        CommandBinding binding = new(
            command,
            (_, _) => executed = true,
            (_, args) => args.CanExecute = true);

        ExecutedRoutedEventArgs executedArgs = new(CommandEvents.ExecutedEvent, new object(), command, null);
        binding.OnExecuted(new UiElementId("target"), executedArgs);

        Assert.True(executed);
    }
}
