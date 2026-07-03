using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.UI.Layout;

public sealed class VisibilityTests
{
    [Fact]
    public void VisibleElementParticipatesNormally()
    {
        FixedElement element = new(new LayoutSize(20, 10));

        LayoutSize desired = element.Measure(new MeasureContext(new LayoutSize(100, 100)));

        Assert.Equal(new LayoutSize(20, 10), desired);
    }

    [Fact]
    public void HiddenElementReservesLayoutSpace()
    {
        FixedElement element = new(new LayoutSize(20, 10))
        {
            Visibility = Visibility.Hidden
        };

        LayoutSize desired = element.Measure(new MeasureContext(new LayoutSize(100, 100)));

        Assert.Equal(new LayoutSize(20, 10), desired);
    }

    [Fact]
    public void CollapsedElementContributesZeroSize()
    {
        FixedElement element = new(new LayoutSize(20, 10))
        {
            Visibility = Visibility.Collapsed
        };

        LayoutSize desired = element.Measure(new MeasureContext(new LayoutSize(100, 100)));
        LayoutRect arranged = element.Arrange(new ArrangeContext(new LayoutRect(0, 0, 50, 50)));

        Assert.Equal(LayoutSize.Zero, desired);
        Assert.Equal(new LayoutRect(0, 0, 0, 0), arranged);
    }

    [Theory]
    [InlineData(Visibility.Hidden)]
    [InlineData(Visibility.Collapsed)]
    public void NonVisibleLayoutVisibilityExcludesElementFromInputRoute(Visibility visibility)
    {
        UIRoot root = new();
        UIElement child = new()
        {
            Visibility = visibility
        };

        root.VisualChildren.Add(child);

        ElementInputRouteMap routeMap = new ElementInputRouteBuilder().Build(root);

        Assert.False(routeMap.TryGetId(child, out _));
    }

    [Theory]
    [InlineData(Visibility.Hidden)]
    [InlineData(Visibility.Collapsed)]
    public void NonVisibleLayoutVisibilityExcludesDescendantsFromInputRoute(Visibility visibility)
    {
        UIRoot root = new();
        UIElement parent = new()
        {
            Visibility = visibility
        };
        UIElement child = new();

        root.VisualChildren.Add(parent);
        parent.VisualChildren.Add(child);

        ElementInputRouteMap routeMap = new ElementInputRouteBuilder().Build(root);

        Assert.False(routeMap.TryGetId(child, out _));
    }

    private sealed class FixedElement(LayoutSize size) : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return size;
        }
    }
}
