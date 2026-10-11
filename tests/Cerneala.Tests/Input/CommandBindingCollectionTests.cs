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

    [Fact]
    public void ExecuteDispatchCallsEveryOriginalBindingWhenHandlerRemovesItself()
    {
        RoutedCommand command = new("Save", typeof(CommandBindingCollectionTests));
        CommandBindingCollection bindings = new();
        List<string> calls = [];
        CommandBinding first = null!;
        first = new CommandBinding(command, (_, _) =>
        {
            calls.Add("A");
            bindings.Remove(first);
        });
        bindings.Add(first);
        bindings.Add(new CommandBinding(command, (_, _) => calls.Add("B")));
        bindings.Add(new CommandBinding(command, (_, _) => calls.Add("C")));

        bindings.InvokeExecuted(
            new UiElementId("target"),
            new ExecutedRoutedEventArgs(CommandEvents.ExecutedEvent, "target", command, null));

        Assert.Equal(["A", "B", "C"], calls);
        Assert.Equal(2, bindings.Count);
    }

    [Fact]
    public void ExecuteDispatchDoesNotThrowWhenHandlerClearsBindings()
    {
        RoutedCommand command = new("Save", typeof(CommandBindingCollectionTests));
        CommandBindingCollection bindings = new();
        List<string> calls = [];
        bindings.Add(new CommandBinding(command, (_, _) =>
        {
            calls.Add("A");
            bindings.Clear();
        }));
        bindings.Add(new CommandBinding(command, (_, _) => calls.Add("B")));

        bindings.InvokeExecuted(
            new UiElementId("target"),
            new ExecutedRoutedEventArgs(CommandEvents.ExecutedEvent, "target", command, null));

        Assert.Equal(["A", "B"], calls);
        Assert.Empty(bindings);
    }

    [Fact]
    public void CanExecuteDispatchCallsEveryOriginalBindingWhenHandlerRemovesItself()
    {
        RoutedCommand command = new("Save", typeof(CommandBindingCollectionTests));
        CommandBindingCollection bindings = new();
        List<string> calls = [];
        CommandBinding first = null!;
        first = new CommandBinding(command, null, (_, _) =>
        {
            calls.Add("A");
            bindings.Remove(first);
        });
        bindings.Add(first);
        bindings.Add(new CommandBinding(command, null, (_, _) => calls.Add("B")));
        bindings.Add(new CommandBinding(command, null, (_, _) => calls.Add("C")));

        bindings.InvokeCanExecute(
            new UiElementId("target"),
            new CanExecuteRoutedEventArgs(CommandEvents.CanExecuteEvent, "target", command, null));

        Assert.Equal(["A", "B", "C"], calls);
        Assert.Equal(2, bindings.Count);
    }

    [Fact]
    public void CanExecuteDispatchDoesNotThrowWhenHandlerClearsBindings()
    {
        RoutedCommand command = new("Save", typeof(CommandBindingCollectionTests));
        CommandBindingCollection bindings = new();
        List<string> calls = [];
        bindings.Add(new CommandBinding(command, null, (_, _) =>
        {
            calls.Add("A");
            bindings.Clear();
        }));
        bindings.Add(new CommandBinding(command, null, (_, _) => calls.Add("B")));

        bindings.InvokeCanExecute(
            new UiElementId("target"),
            new CanExecuteRoutedEventArgs(CommandEvents.CanExecuteEvent, "target", command, null));

        Assert.Equal(["A", "B"], calls);
        Assert.Empty(bindings);
    }
}
