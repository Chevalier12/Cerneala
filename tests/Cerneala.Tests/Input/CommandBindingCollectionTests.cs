using Cerneala.UI.Elements;
using Cerneala.UI.Input;

namespace Cerneala.Tests.Input;

public sealed class CommandBindingCollectionTests
{
    [Fact]
    public void StoresBindingsInInsertionOrder()
    {
        RoutedCommand command = new("Save", typeof(CommandBindingCollectionTests));
        CommandBindingCollection bindings = new();
        List<string> calls = [];
        bindings.Add(new CommandBinding(command, (_, _) => calls.Add("first")));
        bindings.Add(new CommandBinding(command, (_, _) => calls.Add("second")));

        bindings.InvokeExecuted(
            new UiElementId("target"),
            new ExecutedRoutedEventArgs(CommandEvents.ExecutedEvent, "target", command, null));

        Assert.Equal(["first", "second"], calls);
    }

    [Fact]
    public void RejectsNullBinding()
    {
        CommandBindingCollection bindings = new();

        Assert.Throws<ArgumentNullException>(() => bindings.Add(null!));
    }

    [Fact]
    public void DispatchIgnoresNonMatchingCommand()
    {
        RoutedCommand save = new("Save", typeof(CommandBindingCollectionTests));
        RoutedCommand open = new("Open", typeof(CommandBindingCollectionTests));
        CommandBindingCollection bindings = new();
        bool executed = false;
        bindings.Add(new CommandBinding(save, (_, _) => executed = true));

        bindings.InvokeExecuted(
            new UiElementId("target"),
            new ExecutedRoutedEventArgs(CommandEvents.ExecutedEvent, "target", open, null));

        Assert.False(executed);
    }

    [Fact]
    public void ExecuteDispatchIgnoresBindingAddedDuringCurrentInvocation()
    {
        UIElement element = new();
        RoutedCommand command = new("Save", typeof(CommandBindingCollectionTests));
        List<string> calls = [];
        bool added = false;
        element.CommandBindings.Add(new CommandBinding(command, (_, _) =>
        {
            calls.Add("first");
            if (added)
            {
                return;
            }

            added = true;
            element.CommandBindings.Add(new CommandBinding(command, (_, _) => calls.Add("added")));
        }));

        element.CommandBindings.InvokeExecuted(
            new UiElementId("target"),
            new ExecutedRoutedEventArgs(CommandEvents.ExecutedEvent, "target", command, null));

        Assert.Equal(["first"], calls);
    }

    [Fact]
    public void CanExecuteDispatchIgnoresBindingAddedDuringCurrentInvocation()
    {
        UIElement element = new();
        RoutedCommand command = new("Save", typeof(CommandBindingCollectionTests));
        List<string> calls = [];
        bool added = false;
        element.CommandBindings.Add(new CommandBinding(command, null, (_, _) =>
        {
            calls.Add("first");
            if (added)
            {
                return;
            }

            added = true;
            element.CommandBindings.Add(new CommandBinding(command, null, (_, _) => calls.Add("added")));
        }));

        element.CommandBindings.InvokeCanExecute(
            new UiElementId("target"),
            new CanExecuteRoutedEventArgs(CommandEvents.CanExecuteEvent, "target", command, null));

        Assert.Equal(["first"], calls);
    }
}
