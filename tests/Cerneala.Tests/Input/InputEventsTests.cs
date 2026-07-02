using Cerneala.UI.Input;

namespace Cerneala.Tests.Input;

public sealed class InputEventsTests
{
    [Theory]
    [InlineData("PreviewMouseDown", RoutingStrategy.Tunnel)]
    [InlineData("MouseDown", RoutingStrategy.Bubble)]
    [InlineData("MouseEnter", RoutingStrategy.Direct)]
    [InlineData("PreviewMouseLeftButtonDown", RoutingStrategy.Direct)]
    [InlineData("MouseLeftButtonDown", RoutingStrategy.Direct)]
    [InlineData("PreviewMouseLeftButtonUp", RoutingStrategy.Direct)]
    [InlineData("MouseLeftButtonUp", RoutingStrategy.Direct)]
    [InlineData("PreviewMouseRightButtonDown", RoutingStrategy.Direct)]
    [InlineData("MouseRightButtonDown", RoutingStrategy.Direct)]
    [InlineData("PreviewMouseRightButtonUp", RoutingStrategy.Direct)]
    [InlineData("MouseRightButtonUp", RoutingStrategy.Direct)]
    [InlineData("PreviewMouseDoubleClick", RoutingStrategy.Direct)]
    [InlineData("MouseDoubleClick", RoutingStrategy.Direct)]
    [InlineData("PreviewKeyDown", RoutingStrategy.Tunnel)]
    [InlineData("KeyDown", RoutingStrategy.Bubble)]
    [InlineData("PreviewTextInput", RoutingStrategy.Tunnel)]
    [InlineData("TextInput", RoutingStrategy.Bubble)]
    [InlineData("PreviewGotKeyboardFocus", RoutingStrategy.Tunnel)]
    [InlineData("GotKeyboardFocus", RoutingStrategy.Bubble)]
    [InlineData("GotFocus", RoutingStrategy.Bubble)]
    public void InputEventsUseWpfNamesAndRoutingStrategies(string name, RoutingStrategy strategy)
    {
        RoutedEvent routedEvent = InputEvents.All.Single(e => e.Name == name);

        Assert.Equal(strategy, routedEvent.RoutingStrategy);
    }

    [Theory]
    [InlineData("PreviewStylusDown")]
    [InlineData("StylusButtonUp")]
    [InlineData("PreviewTouchDown")]
    [InlineData("TouchLeave")]
    [InlineData("ManipulationDelta")]
    [InlineData("ManipulationCompleted")]
    [InlineData("PreviewDragEnter")]
    [InlineData("Drop")]
    public void InputEventsIncludeRepresentativeNonMouseCategories(string name)
    {
        Assert.Contains(InputEvents.All, e => e.Name == name);
    }

    [Theory]
    [InlineData("PreviewCanExecute", RoutingStrategy.Tunnel)]
    [InlineData("CanExecute", RoutingStrategy.Bubble)]
    [InlineData("PreviewExecuted", RoutingStrategy.Tunnel)]
    [InlineData("Executed", RoutingStrategy.Bubble)]
    public void CommandEventsUseWpfNamesAndRoutingStrategies(string name, RoutingStrategy strategy)
    {
        RoutedEvent routedEvent = CommandEvents.All.Single(e => e.Name == name);

        Assert.Equal(strategy, routedEvent.RoutingStrategy);
    }

    [Fact]
    public void MouseButtonEventArgsExposeButtonAndPosition()
    {
        object source = new();
        MouseButtonEventArgs args = new(InputEvents.MouseDownEvent, source, InputMouseButton.Left, 10, 20, 1);

        Assert.Equal(InputMouseButton.Left, args.ChangedButton);
        Assert.Equal(10, args.X);
        Assert.Equal(20, args.Y);
        Assert.Equal(1, args.ClickCount);
    }
}
