using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Input;

public sealed class StylusInputBridgeTests
{
    [Fact]
    public void StylusDownRoutesPressureAndPosition()
    {
        (UIRoot root, UIElement target) = CreateRootWithTarget();
        StylusEventArgs? received = null;
        target.Handlers.AddHandler(InputEvents.StylusDownEvent, (_, args) => received = Assert.IsType<StylusEventArgs>(args));

        new StylusInputBridge().Dispatch(root, new StylusInputFrame(new StylusInputPoint(9, 11, 13, StylusInputAction.Down, 0.75f)));

        Assert.NotNull(received);
        Assert.Equal(9, received.StylusId);
        Assert.Equal(11, received.X);
        Assert.Equal(13, received.Y);
        Assert.Equal(0.75f, received.Pressure);
        Assert.True(received.IsInRange);
    }

    [Fact]
    public void StylusButtonRoutesTypedArgs()
    {
        (UIRoot root, UIElement target) = CreateRootWithTarget();
        StylusEventArgs? received = null;
        target.Handlers.AddHandler(InputEvents.StylusButtonDownEvent, (_, args) => received = Assert.IsType<StylusEventArgs>(args));

        new StylusInputBridge().Dispatch(root, new StylusInputFrame(new StylusInputPoint(2, 8, 9, StylusInputAction.ButtonDown, Button: "Barrel")));

        Assert.NotNull(received);
        Assert.Equal("Barrel", received.Button);
        Assert.Same(InputEvents.StylusButtonDownEvent, received.RoutedEvent);
    }

    private static (UIRoot Root, UIElement Target) CreateRootWithTarget()
    {
        UIRoot root = new(100, 100);
        UIElement target = new();
        target.Arrange(new ArrangeContext(new LayoutRect(0, 0, 50, 50)));
        root.VisualChildren.Add(target);
        return (root, target);
    }
}
