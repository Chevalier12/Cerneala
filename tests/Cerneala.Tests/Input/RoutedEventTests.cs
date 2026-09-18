using Cerneala.UI.Input;

namespace Cerneala.Tests.Input;

public sealed class RoutedEventTests
{
    [Fact]
    public void RegisterCreatesRoutedEventMetadata()
    {
        RoutedEvent routedEvent = RoutedEventRegistry.Register(
            "MouseDown",
            typeof(RoutedEventTests),
            RoutingStrategy.Bubble,
            typeof(RoutedEventArgs));

        Assert.Equal("MouseDown", routedEvent.Name);
        Assert.Equal(typeof(RoutedEventTests), routedEvent.OwnerType);
        Assert.Equal(RoutingStrategy.Bubble, routedEvent.RoutingStrategy);
        Assert.Equal(typeof(RoutedEventArgs), routedEvent.ArgsType);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RegisterRejectsEmptyName(string name)
    {
        Assert.Throws<ArgumentException>(
            () => RoutedEventRegistry.Register(name, typeof(RoutedEventTests), RoutingStrategy.Bubble, typeof(RoutedEventArgs)));
    }

    [Fact]
    public void RegisterRejectsNullArgsType()
    {
        Assert.Throws<ArgumentNullException>(
            () => RoutedEventRegistry.Register("MouseMove", typeof(RoutedEventTests), RoutingStrategy.Bubble, null!));
    }

    [Fact]
    public void RegisterRejectsUnsupportedRoutingStrategy()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => RoutedEventRegistry.Register(
                "MouseMove",
                typeof(RoutedEventTests),
                (RoutingStrategy)42,
                typeof(RoutedEventArgs)));
    }

    [Theory]
    [InlineData(RoutingStrategy.Direct)]
    [InlineData(RoutingStrategy.Bubble)]
    [InlineData(RoutingStrategy.Tunnel)]
    public void RegisterPreservesEverySupportedStrategy(RoutingStrategy strategy)
    {
        RoutedEvent routedEvent = RoutedEventRegistry.Register(
            "Valid", typeof(RoutedEventTests), strategy, typeof(MouseEventArgs));
        Assert.Equal(strategy, routedEvent.RoutingStrategy);
        Assert.Equal(typeof(MouseEventArgs), routedEvent.ArgsType);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void RegisterPreservesInvalidStrategyFailure(int value)
    {
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            RoutedEventRegistry.Register("Invalid", typeof(RoutedEventTests), (RoutingStrategy)value, typeof(string)));
        Assert.Equal("routingStrategy", error.ParamName);
        Assert.Equal((RoutingStrategy)value, error.ActualValue);
    }

    [Fact]
    public void RegisterValidatesNullArgumentTypeBeforeStrategy()
    {
        ArgumentNullException error = Assert.Throws<ArgumentNullException>(() =>
            RoutedEventRegistry.Register("Invalid", typeof(RoutedEventTests), (RoutingStrategy)(-1), null!));
        Assert.Equal("argsType", error.ParamName);
    }

    [Fact]
    public void RoutedEventArgsDefaultsSourceToOriginalSource()
    {
        object source = new();
        RoutedEvent routedEvent = RoutedEventRegistry.Register(
            "MouseMove",
            typeof(RoutedEventTests),
            RoutingStrategy.Bubble,
            typeof(RoutedEventArgs));

        RoutedEventArgs args = new(routedEvent, source);

        Assert.Same(source, args.OriginalSource);
        Assert.Same(source, args.Source);
        Assert.False(args.Handled);
    }
}
